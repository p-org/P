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
    /**
     * Exit codes:
     * 0 - Success
     * 2 - Bug found or too many choices
     * 3 - Timeout
     * 4 - Out of memory
     * 5 - Runtime error
     * 6 - Test driver error
     */
    public static void main(String[] args) {
        // configure Log4J
        Log4JConfig.configureLog4J();

        // Parse command line arguments and initialize global settings
        initializeEnvironment(args);
        
        int exitCode = 0;
        try {
            // Get model and test driver
            Reflections reflections = initializeModel();
            
            // Setup loggers, random number gen, time and memory monitors
            setup();
            
            // Set test driver and run analysis
            setTestDriver(reflections);
            RuntimeExecutor.run();
        } catch (BugFoundException e) {
            PExLogger.logInfo("Bug found or too many choices: " + e.getMessage());
            PExLogger.logTrace(e);
            exitCode = 2;
        } catch (InvocationTargetException ex) {
            PExLogger.logInfo("Invocation target exception: " + ex.getMessage());
            PExLogger.logTrace(ex);
            exitCode = 5;
        } catch (Exception ex) {
            if ("TIMEOUT".equals(ex.getMessage())) {
                PExLogger.logInfo("Execution timed out");
                exitCode = 3;
            } else if ("MEMOUT".equals(ex.getMessage())) {
                PExLogger.logInfo("Out of memory: " + MemoryMonitor.getMemSpent() + " MB used");
                exitCode = 4;
            } else {
                PExLogger.logInfo("Runtime exception: " + ex.getMessage());
                PExLogger.logTrace(ex);
                exitCode = 5;
            }
        } finally {
            // Log end-of-run metrics
            StatWriter.log("exit-code", String.format("%d", exitCode));
            
            // Exit
            System.exit(exitCode);
        }
    }
    
    /**
     * Initialize global environment settings
     * 
     * @param args Command line arguments
     */
    private static void initializeEnvironment(String[] args) {
        PExGlobal.setConfig(PExOptions.ParseCommandlineArgs(args));
        PExGlobal.setChoiceSelector();
        PExLogger.Initialize(PExGlobal.getConfig().getVerbosity());
        ComputeHash.Initialize();
    }
    
    /**
     * Initialize the model
     * 
     * @return Reflections object for model package
     * @throws Exception if model initialization fails
     */
    private static Reflections initializeModel() throws Exception {
        // Get reflections corresponding to the model
        Reflections reflections = new Reflections("pex.model");
        
        // Get all classes extending PModel
        Set<Class<? extends PModel>> subTypesPModel = reflections.getSubTypesOf(PModel.class);
        if (subTypesPModel.isEmpty()) {
            throw new Exception("No PModel implementation found in pex.model package.");
        }
        
        // Get first model and instantiate it
        Class<? extends PModel> modelClass = subTypesPModel.stream()
            .findFirst()
            .orElseThrow(() -> new Exception("Failed to get PModel instance."));
            
        PExGlobal.setModel(modelClass.getDeclaredConstructor().newInstance());
        
        // Set project name
        setProjectName();
        
        return reflections;
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
    private static void setTestDriver(Reflections reflections) throws Exception {
        final String requestedName = PExGlobal.getConfig().getTestDriver();
        final String defaultTestDriver = PExGlobal.getConfig().getTestDriverDefault();
        final Set<Class<? extends PTestDriver>> availableDrivers = reflections.getSubTypesOf(PTestDriver.class);
        
        // Try to find driver by name
        Optional<Class<? extends PTestDriver>> driverClass = availableDrivers.stream()
            .filter(d -> PTestDriver.getTestName(d).equals(requestedName))
            .findFirst();
            
        // If not found but using default driver name and only one driver exists, use that one
        if (driverClass.isEmpty() && requestedName.equals(defaultTestDriver) && availableDrivers.size() == 1) {
            driverClass = availableDrivers.stream().findFirst();
        }
        
        // If we found a driver, instantiate and configure it
        if (driverClass.isPresent()) {
            PTestDriver driver = driverClass.get().getDeclaredConstructor().newInstance();
            PExGlobal.getConfig().setTestDriver(PTestDriver.getTestName(driver.getClass()));
            PExGlobal.getModel().setTestDriver(driver);
            return;
        }
        
        // No driver found, report error with available options
        if (!requestedName.equals(defaultTestDriver)) {
            PExLogger.logInfo("No test driver found named \"" + requestedName + "\"");
        }
        
        PExLogger.logInfo(String.format(
                "Error: We found %d test cases. Please provide a more precise name using (--testcase | -tc).",
                availableDrivers.size()));
        
        if (!availableDrivers.isEmpty()) {
            PExLogger.logInfo("Available test driver options:");
            availableDrivers.forEach(td -> 
                PExLogger.logInfo(String.format("  - %s", PTestDriver.getTestName(td))));
        }
        
        // Exit with appropriate error
        if (!requestedName.equals(defaultTestDriver)) {
            throw new Exception("No test driver found named \"" + requestedName + "\"");
        } else {
            System.exit(6);
        }
    }

}
