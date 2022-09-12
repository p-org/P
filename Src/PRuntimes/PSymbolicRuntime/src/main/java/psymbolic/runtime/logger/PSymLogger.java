package psymbolic.runtime.logger;

import org.apache.log4j.*;
import psymbolic.utils.MemoryMonitor;

/**
 * Represents the P Symbolic logger configuration
 */
public class PSymLogger {

    /* Get actual class name to be printed on */
    static Logger log = Logger.getLogger(PSymLogger.class.getName());

    public static void Initialize()
    {
        // remove all the appenders
        log.removeAllAppenders();
        // setting up the logger
        //This is the root logger provided by log4j
        log.setLevel(Level.ALL);

        //Define log pattern layout
        PatternLayout layout = new PatternLayout("%m%n");

        //Add console appender to root logger
        log.addAppender(new ConsoleAppender(layout));
    }

    public static void finished(int totalIter, int newIter, long timeSpent, String result, String mode) {
        log.info(String.format("--------------------"));
        log.info(String.format("Explored %d %s executions%s", totalIter, mode, ((totalIter==newIter)?"":String.format(" (%d new)", newIter))));
        log.info(String.format("Took %d seconds and %.1f GB", timeSpent, MemoryMonitor.getMaxMemSpent()/1000.0));
        log.info(String.format("Result: " + result));
        log.info(String.format("--------------------"));
    }

    public static void info(String message) {
        log.info(message);
    }

    public static void warn(String message) {
        log.warn(message);
    }

    public static void error(String message) {
        log.error(message);
    }

    public static void ResetAllConfigurations(int verbosity, String projectName, String outputFolder)
    {
        BasicConfigurator.resetConfiguration();
        Initialize();
        SearchLogger.Initialize(verbosity, outputFolder);
        TraceLogger.Initialize(verbosity, outputFolder);
        StatLogger.Initialize(projectName, outputFolder);
        CoverageLogger.Initialize(projectName, outputFolder);
    }

    public static void ErrorReproMode()
    {
        SearchLogger.disable();
        TraceLogger.enable();
        CoverageLogger.disable();
    }

    public static void SearchMode()
    {
        SearchLogger.enable();
        TraceLogger.enable();
        CoverageLogger.enable();
    }
}
