package pcover.runtime.logger;

import lombok.Setter;
import org.apache.logging.log4j.Level;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.LoggerContext;
import org.apache.logging.log4j.core.appender.ConsoleAppender;
import org.apache.logging.log4j.core.config.Configurator;
import org.apache.logging.log4j.core.layout.PatternLayout;
import pcover.utils.exceptions.NotImplementedException;

import java.io.PrintWriter;
import java.io.StringWriter;

/**
 * Represents the main PCover logger
 */
public class PCoverLogger {
    static Logger log = null;
    static LoggerContext context = null;
    @Setter
    static int verbosity;

    /**
     * Initializes the logger with the given verbosity level.
     *
     * @param verb Verbosity level
     */
    public static void Initialize(int verb) {
        verbosity = verb;
        log = Log4JConfig.getContext().getLogger(PCoverLogger.class.getName());
        org.apache.logging.log4j.core.Logger coreLogger =
                (org.apache.logging.log4j.core.Logger) LogManager.getLogger(PCoverLogger.class.getName());
        context = coreLogger.getContext();

        PatternLayout layout = Log4JConfig.getPatternLayout();
        ConsoleAppender consoleAppender = ConsoleAppender.createDefaultAppenderForLayout(layout);
        consoleAppender.start();

        context.getConfiguration().addLoggerAppender(coreLogger, consoleAppender);
    }

    /**
     * Disable the logger
     */
    public static void disable() {
        Configurator.setLevel(PCoverLogger.class.getName(), Level.OFF);
    }

    /**
     * Enable the logger
     */
    public static void enable() {
        Configurator.setLevel(PCoverLogger.class.getName(), Level.ALL);
    }

    /**
     * TODO
     *
     * @param totalIter
     * @param newIter
     * @param timeSpent
     * @param result
     */
    public static void finished(int totalIter, int newIter, long timeSpent, String result) {
        throw new NotImplementedException();
    }

    /**
     * Logs the given message based on the current verbosity level.
     *
     * @param message Message to print
     */
    public static void log(String message) {
        if (verbosity > 0) {
            log.info(message);
        }
    }

    /**
     * Logs the given info message.
     *
     * @param message Message to print
     */

    public static void info(String message) {
        log.info(message);
    }

    /**
     * Logs the given warning message.
     *
     * @param message Message to print
     */
    public static void warn(String message) {
        log.warn(message);
    }

    /**
     * Logs the given error message.
     *
     * @param message Message to print
     */
    public static void error(String message) {
        log.error(message);
    }

    /**
     * TODO
     *
     * @param verbosity
     * @param projectName
     * @param outputFolder
     */
    public static void ResetAllConfigurations(
            int verbosity, String projectName, String outputFolder) {
        throw new NotImplementedException();
    }

    /**
     * Print error trace
     *
     * @param e          Exception object
     * @param stderrOnly Print to stderr only
     */
    public static void printStackTrace(Exception e, boolean stderrOnly) {
        if (stderrOnly) {
            System.err.println("... Stack trace:");
            e.printStackTrace(System.err);
        } else {
            StringWriter sw = new StringWriter();
            PrintWriter pw = new PrintWriter(sw);
            e.printStackTrace(pw);

            info("... Stack trace:");
            info(sw.toString());
        }
    }
}
