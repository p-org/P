package psymbolic.commandline;

import org.reflections.Reflections;
import psymbolic.runtime.logger.*;
import psymbolic.runtime.scheduler.DPORScheduler;
import psymbolic.runtime.scheduler.IterativeBoundedScheduler;
import psymbolic.runtime.scheduler.ReplayScheduler;
import psymbolic.runtime.statistics.SolverStats;
import psymbolic.utils.MemoryMonitor;
import psymbolic.utils.RandomNumberGenerator;
import psymbolic.utils.TimeMonitor;
import psymbolic.valuesummary.solvers.SolverEngine;

import java.util.Optional;
import java.util.Set;

public class PSymbolic {

    public static void main(String[] args) {
        Log4JConfig.configureLog4J();
        Reflections reflections = new Reflections("psymbolic");
        Program p = null;

        // parse the commandline arguments to create the configuration
        PSymConfiguration config = PSymOptions.ParseCommandlineArgs(args);
        PSymLogger.Initialize(config.getVerbosity());

        try {
            if (config.getReadFromFile() == "" && config.getReadReplayerFromFile() == "") {
                Set<Class<? extends Program>> subTypesProgram = reflections.getSubTypesOf(Program.class);
                if (subTypesProgram.stream().count() == 0) {
                    throw new Exception("No program found.");
                }

                Optional<Class<? extends Program>> program = subTypesProgram.stream().findFirst();
                Object instance = program.get().getDeclaredConstructor().newInstance();
                p = (Program) instance;
                setProjectName(p, config);
            }
        } catch (Exception ex) {
            ex.printStackTrace();
            System.exit(5);
        }

        setup(config);

        int exit_code = 0;
        try {
            if (config.getReadReplayerFromFile() != "") {
                ReplayScheduler replayScheduler = ReplayScheduler.readFromFile(config.getReadReplayerFromFile());
                EntryPoint.replayBug(replayScheduler, config);
                throw new Exception("ERROR");
            }

            IterativeBoundedScheduler scheduler;

            if (config.isWriteToFile()) {
                BacktrackWriter.Initialize(config.getProjectName(), config.getOutputFolder());
            }

            if (config.getReadFromFile() == "") {
                assert(p != null);
                setTestDriver(p, config, reflections);
                scheduler = new IterativeBoundedScheduler(config, p);
                if (config.isDpor()) scheduler = new DPORScheduler(config, p);
                EntryPoint.run(scheduler, config);
            } else {
                scheduler = IterativeBoundedScheduler.readFromFile(config.getReadFromFile());
                EntryPoint.resume(scheduler, config);
            }

            if (config.isWriteToFile()) {
                EntryPoint.writeToFile();
            }
        } catch (BugFoundException e) {
            exit_code = 2;
        } catch (Exception ex) {
            ex.printStackTrace();
            if (ex.getMessage() == "TIMEOUT") {
                exit_code = 3;
            } else if (ex.getMessage() == "MEMOUT") {
                exit_code = 4;
            } else {
                exit_code = 5;
            }
        } finally {
            double postSearchTime = TimeMonitor.getInstance().stopInterval();
            StatWriter.log("time-post-seconds", String.format("%.1f", postSearchTime));
            System.exit(exit_code);
        }
    }

    private static void setup(PSymConfiguration config) {
        PSymLogger.ResetAllConfigurations(config.getVerbosity(), config.getProjectName(), config.getOutputFolder());
        SolverEngine.resetEngine(config.getSolverType(), config.getExprLibType());
        SolverStats.setTimeLimit(config.getTimeLimit());
        SolverStats.setMemLimit(config.getMemLimit());
        MemoryMonitor.setup();
        RandomNumberGenerator.setup(config.getRandomSeed());
        TimeMonitor.setup(config.getTimeLimit());
    }

    /**
     * Set the project name
     * @param p Input program instance
     * @param config Input PSymConfiguration
     */
    private static void setProjectName(Program p, PSymConfiguration config) {
        if(config.getProjectName().equals(config.getProjectNameDefault())) {
            config.setProjectName(p.getClass().getSimpleName());
        }
    }

    /**
     * Set the test driver for the program
     * @param p Input program instance
     * @param config Input PSymConfiguration
     * @throws Exception Throws exception if test driver is not found
     */
    private static void setTestDriver(Program p, PSymConfiguration config, Reflections reflections) throws Exception {
        final String name = sanitizeTestName(config.getTestDriver());
        final String defaultTestDriver = sanitizeTestName(config.getTestDriverDefault());

        Set<Class<? extends PTestDriver>> subTypesDriver = reflections.getSubTypesOf(PTestDriver.class);
        PTestDriver driver = null;
        for (Class<? extends PTestDriver> td: subTypesDriver) {
            if (sanitizeTestName(td.getSimpleName()).equals(name)) {
                driver = td.getDeclaredConstructor().newInstance();
                break;
            }
        }
        if(driver == null
           && name.equals(defaultTestDriver)
           && subTypesDriver.size() == 1) {
            for (Class<? extends PTestDriver> td: subTypesDriver) {
                driver = td.getDeclaredConstructor().newInstance();
                break;
            }
        }
        if(driver == null) {
            if (!name.equals(defaultTestDriver)) {
                PSymLogger.info("No test driver found named \"" + name + "\"");
            }
            PSymLogger.info("Provide /method or -m flag to qualify the test method name you wish to use.");
            PSymLogger.info("Possible options are::");
            for (Class<? extends PTestDriver> td: subTypesDriver) {
                PSymLogger.info(String.format("  %s", td.getSimpleName()));
            }
            if (!name.equals(defaultTestDriver)) {
                throw new Exception("No test driver found named \"" + config.getTestDriver() + "\"");
            } else {
                System.exit(5);
            }
        }
        assert(driver != null);
        config.setTestDriver(driver.getClass().getSimpleName());
        p.setTestDriver(driver);
    }

    private static String sanitizeTestName(String name) {
        String result = name.toLowerCase();
        result = result.replaceFirst("^pimplementation.", "");
        int index = result.lastIndexOf(".execute");
        if (index > 0) {
            result = result.substring(0, index);
        }
        return result;
    }

    public static void initializeDefault(String outputFolder) {
        Log4JConfig.configureLog4J();
        SearchLogger.disable();
        TraceLogger.disable();
        // parse the commandline arguments to create the configuration
        PSymConfiguration config = PSymOptions.ParseCommandlineArgs(new String[0]);
        config.setOutputFolder(outputFolder);
        setup(config);
    }

}
