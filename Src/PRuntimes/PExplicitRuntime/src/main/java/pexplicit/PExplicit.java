package pexplicit;

import org.reflections.Reflections;
import pexplicit.commandline.PExplicitOptions;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.PModel;
import pexplicit.runtime.logger.Log4JConfig;
import pexplicit.runtime.logger.PExplicitLogger;
import pexplicit.runtime.logger.StatWriter;
import pexplicit.runtime.machine.PTestDriver;
import pexplicit.utils.exceptions.BugFoundException;
import pexplicit.utils.monitor.MemoryMonitor;
import pexplicit.utils.monitor.TimeMonitor;
import pexplicit.utils.random.RandomNumberGenerator;
import pexplicit.values.ComputeHash;

import java.lang.reflect.InvocationTargetException;
import java.util.Optional;
import java.util.Set;

/**
 * PExplicit runtime top-level class
 */
public class PExplicit {

    /**
     * Main entry point for PExplicit runtime.
     */
    public static void main(String[] args) {
        // configure Log4J
        Log4JConfig.configureLog4J();

        // parse the commandline arguments to create the configuration
        PExplicitGlobal.setConfig(PExplicitOptions.ParseCommandlineArgs(args));
        PExplicitGlobal.setChoiceSelector();
        PExplicitLogger.Initialize(PExplicitGlobal.getConfig().getVerbosity());
        ComputeHash.Initialize();

        // get reflections corresponding to the model
        Reflections reflections = new Reflections("pexplicit.model");

        try {
            // get all classes extending PModel
            Set<Class<? extends PModel>> subTypesPModel = reflections.getSubTypesOf(PModel.class);
            if (subTypesPModel.isEmpty()) {
                throw new Exception("No PModel found.");
            }
            Optional<Class<? extends PModel>> pModel = subTypesPModel.stream().findFirst();

            // set model instance
            PExplicitGlobal.setModel(pModel.get().getDeclaredConstructor().newInstance());

            // set project name
            setProjectName();
        } catch (Exception ex) {
            ex.printStackTrace();
            System.exit(5);
        }

        // setup loggers, random number gen, time and memory monitors
        setup();

        int exit_code = 0;
        try {
            // set test driver
            setTestDriver(reflections);

            // run the analysis
            RuntimeExecutor.run();
        } catch (BugFoundException e) {
            exit_code = 2;
        } catch (InvocationTargetException ex) {
            ex.printStackTrace();
            exit_code = 5;
        } catch (Exception ex) {
            if (ex.getMessage().equals("TIMEOUT")) {
                exit_code = 3;
            } else if (ex.getMessage().equals("MEMOUT")) {
                exit_code = 4;
            } else {
                ex.printStackTrace();
                exit_code = 5;
            }
        } finally {
            // log end-of-run metrics
            StatWriter.log("exit-code", String.format("%d", exit_code));

            // exit
            System.exit(exit_code);
        }
    }

    /**
     * Sets up the runtime before the run.
     * Initializes loggers, random number generator, time.memory monitors.
     */
    private static void setup() {
        RandomNumberGenerator.setup(PExplicitGlobal.getConfig().getRandomSeed());
        MemoryMonitor.setup(PExplicitGlobal.getConfig().getMemLimit());
        TimeMonitor.setup(PExplicitGlobal.getConfig().getTimeLimit());
    }

    /**
     * Sanitize the model name by removing trailing "PModel" keyword
     *
     * @param name Name of the PModel class
     * @return Class name without trailing "PModel"
     */
    private static String sanitizeModelName(String name) {
        int index = name.lastIndexOf("PModel");
        if (index > 0) {
            name = name.substring(0, index);
        }
        return name;
    }

    /**
     * Set the project name
     */
    private static void setProjectName() {
        if (PExplicitGlobal.getConfig().getProjectName().equals("default")) {
            PExplicitGlobal.getConfig().setProjectName(sanitizeModelName(PExplicitGlobal.getModel().getClass().getSimpleName()));
        }
    }

    /**
     * Set the test driver for the model
     *
     * @param reflections reflections corresponding to the pexplicit.model
     * @throws Exception Throws exception if test driver is not found
     */
    private static void setTestDriver(Reflections reflections)
            throws Exception {
        final String name = PExplicitGlobal.getConfig().getTestDriver();
        final String defaultTestDriver = PExplicitGlobal.getConfig().getTestDriverDefault();

        Set<Class<? extends PTestDriver>> subTypesDriver = reflections.getSubTypesOf(PTestDriver.class);
        PTestDriver driver = null;
        for (Class<? extends PTestDriver> td : subTypesDriver) {
            if (td.getSimpleName().equals(name)) {
                driver = td.getDeclaredConstructor().newInstance();
                break;
            }
        }
        if (driver == null && name.equals(defaultTestDriver) && subTypesDriver.size() == 1) {
            for (Class<? extends PTestDriver> td : subTypesDriver) {
                driver = td.getDeclaredConstructor().newInstance();
                break;
            }
        }
        if (driver == null) {
            if (!name.equals(defaultTestDriver)) {
                PExplicitLogger.logInfo("No test driver found named \"" + name + "\"");
            }
            PExplicitLogger.logInfo(
                    String.format(
                            "Error: We found '%d' test cases. Please provide a more precise name of the test case you wish to check using (--testcase | -tc).",
                            subTypesDriver.size()));
            PExplicitLogger.logInfo("Possible options are:");
            for (Class<? extends PTestDriver> td : subTypesDriver) {
                PExplicitLogger.logInfo(String.format("%s", td.getSimpleName()));
            }
            if (!name.equals(defaultTestDriver)) {
                throw new Exception("No test driver found named \"" + PExplicitGlobal.getConfig().getTestDriver() + "\"");
            } else {
                System.exit(6);
            }
        }
        PExplicitGlobal.getConfig().setTestDriver(driver.getClass().getSimpleName());
        PExplicitGlobal.getModel().setTestDriver(driver);
    }

}
