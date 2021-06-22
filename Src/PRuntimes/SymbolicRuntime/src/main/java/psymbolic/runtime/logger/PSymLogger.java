package psymbolic.runtime.logger;

import org.apache.log4j.*;

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
        log.setLevel(Level.DEBUG);

        //Define log pattern layout
        PatternLayout layout = new PatternLayout("%m%n");

        //Add console appender to root logger
        log.addAppender(new ConsoleAppender(layout));
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

    public static void ResetAllConfigurations()
    {
        BasicConfigurator.resetConfiguration();
        Initialize();
        SearchLogger.Initialize();
        TraceSymLogger.Initialize();
    }

    public static void ErrorReproMode()
    {
        SearchLogger.disable();
        TraceSymLogger.enable();
    }

    public static void SearchMode()
    {
        SearchLogger.enable();
        TraceSymLogger.disable();
    }
}
