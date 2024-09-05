package pex;

import org.reflections.Reflections;
import pex.commandline.PExOptions;
import pex.runtime.PExGlobal;
import pex.runtime.PModel;
import pex.runtime.logger.Log4JConfig;
import pex.runtime.logger.PExLogger;
import pex.runtime.logger.StatWriter;
import pex.runtime.machine.PTestDriver;
import pex.utils.exceptions.BugFoundException;
import pex.utils.exceptions.TooManyChoicesException;
import pex.utils.monitor.MemoryMonitor;
import pex.utils.monitor.TimeMonitor;
import pex.utils.random.RandomNumberGenerator;
import pex.values.ComputeHash;

import java.lang.reflect.InvocationTargetException;
import java.util.Optional;
import java.util.Set;

/**
 * PEx runtime top-level class
 */
public class PEx {

    /**
     * Main entry point for PEx runtime.
     */
    public static void main(String[] args) {
        // configure Log4J
        Log4JConfig.configureLog4J();

        // parse the commandline arguments to create the configuration
        PExGlobal.setConfig(PExOptions.ParseCommandlineArgs(args));
        PExGlobal.setChoiceSelector();
        PExLogger.Initialize(PExGlobal.getConfig().getVerbosity());
        ComputeHash.Initialize();

        // get reflections corresponding to the model
        Reflections reflections = new Reflections("pex.model");

        try {
            // get all classes extending PModel
            Set<Class<? extends PModel>> subTypesPModel = reflections.getSubTypesOf(PModel.class);
            if (subTypesPModel.isEmpty()) {
                throw new Exception("No PModel found.");
            }
            Optional<Class<? extends PModel>> pModel = subTypesPModel.stream().findFirst();

            // set model instance
            PExGlobal.setModel(pModel.get().getDeclaredConstructor().newInstance());

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
        } catch (TooManyChoicesException e) {
            exit_code = 3;
        } catch (BugFoundException e) {
            exit_code = 2;
        } catch (InvocationTargetException ex) {
            ex.printStackTrace();
            exit_code = 6;
        } catch (Exception ex) {
            if (ex.getMessage().equals("TIMEOUT")) {
                exit_code = 4;
            } else if (ex.getMessage().equals("MEMOUT")) {
                exit_code = 5;
            } else {
                ex.printStackTrace();
                exit_code = 6;
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
     * Initializes loggers, random number generator, time/memory monitors.
     */
    private static void setup() {
        RandomNumberGenerator.setup(PExGlobal.getConfig().getRandomSeed());
        MemoryMonitor.setup(PExGlobal.getConfig().getMemLimit());
        TimeMonitor.setup(PExGlobal.getConfig().getTimeLimit());
        // initialize stats writer
        StatWriter.Initialize();
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
        if (PExGlobal.getConfig().getProjectName().equals("default")) {
            PExGlobal.getConfig().setProjectName(sanitizeModelName(PExGlobal.getModel().getClass().getSimpleName()));
        }
    }

    /**
     * Set the test driver for the model
     *
     * @param reflections reflections corresponding to the pex.model
     * @throws Exception Throws exception if test driver is not found
     */
    private static void setTestDriver(Reflections reflections)
            throws Exception {
        final String name = PExGlobal.getConfig().getTestDriver();
        final String defaultTestDriver = PExGlobal.getConfig().getTestDriverDefault();

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
                PExLogger.logInfo("No test driver found named \"" + name + "\"");
            }
            PExLogger.logInfo(
                    String.format(
                            "Error: We found '%d' test cases. Please provide a more precise name of the test case you wish to check using (--testcase | -tc).",
                            subTypesDriver.size()));
            PExLogger.logInfo("Possible options are:");
            for (Class<? extends PTestDriver> td : subTypesDriver) {
                PExLogger.logInfo(String.format("%s", td.getSimpleName()));
            }
            if (!name.equals(defaultTestDriver)) {
                throw new Exception("No test driver found named \"" + PExGlobal.getConfig().getTestDriver() + "\"");
            } else {
                System.exit(6);
            }
        }
        PExGlobal.getConfig().setTestDriver(driver.getClass().getSimpleName());
        PExGlobal.getModel().setTestDriver(driver);
    }

}
