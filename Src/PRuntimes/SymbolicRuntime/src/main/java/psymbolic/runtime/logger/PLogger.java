package psymbolic.runtime.logger;

import org.apache.log4j.*;

import java.util.Arrays;

public class PLogger {
    /* Get actual class name to be printed on */
    static Logger log = Logger.getLogger(PLogger.class.getName());
    public static void Init()
    {
        BasicConfigurator.configure();
        // setting up the logger
        //This is the root logger provided by log4j
        Logger rootLogger = Logger.getRootLogger();
        rootLogger.setLevel(Level.DEBUG);

        //Define log pattern layout
        PatternLayout layout = new PatternLayout("%m%n");

        //Add console appender to root logger
        //rootLogger.addAppender(new ConsoleAppender(layout));
        /*try
        {
            //Define file appender with layout and output log file name
            RollingFileAppender fileAppender = new RollingFileAppender(layout, "demoApplication.log");

            //Add the appender to root logger
            rootLogger.addAppender(fileAppender);
        }
        catch (IOException e)
        {
            System.out.println("Failed to add appender !!");
        }*/
    }

    public static void log(String s) {
        log.info(s);
    }

    public void logMessage(Object ... message) {
        log.info("<PrintLog> " + String.join(", ", Arrays.toString(message)));
    }

    public static void disable() {
        log.setLevel(Level.OFF);
    }

    public static void enable() {
        log.setLevel(Level.ALL);
    }
}
