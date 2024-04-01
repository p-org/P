package pexplicit.runtime.logger;

import lombok.Setter;
import org.apache.logging.log4j.Level;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.LoggerContext;
import org.apache.logging.log4j.core.appender.ConsoleAppender;
import org.apache.logging.log4j.core.config.Configurator;
import org.apache.logging.log4j.core.layout.PatternLayout;
import pexplicit.runtime.PExplicitGlobal;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.events.PMessage;
import pexplicit.utils.monitor.MemoryMonitor;

import java.io.PrintWriter;
import java.io.StringWriter;

/**
 * Represents the main PExplicit logger
 */
public class PExplicitLogger {
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
        log = Log4JConfig.getContext().getLogger(PExplicitLogger.class.getName());
        org.apache.logging.log4j.core.Logger coreLogger =
                (org.apache.logging.log4j.core.Logger) LogManager.getLogger(PExplicitLogger.class.getName());
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
        Configurator.setLevel(PExplicitLogger.class.getName(), Level.OFF);
    }

    /**
     * Enable the logger
     */
    public static void enable() {
        Configurator.setLevel(PExplicitLogger.class.getName(), Level.ALL);
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
     * Initialize all loggers and writers
     */
    public static void InitializeLoggers() {
        StatWriter.Initialize();
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

    /**
     * Log at the start of an iteration
     * @param iter Iteration number
     * @param step Starting step number
     */
    public static void logStartIteration(int iter, int step) {
        if (verbosity > 0) {
            log.info("--------------------");
            log.info("Starting Schedule: " + iter + " from step: " + step);
        }
    }

    public static void logStartStep(int step, PMachine sender, PMessage msg) {
        if (verbosity > 0) {
            log.info(String.format(
                    "  Step %d: %s sent %s to %s",
                    step, sender, msg.getEvent(), msg.getTarget()));
            if (verbosity > 5) {
                log.info(String.format("    payload: %s", msg.getPayload()));
            }
        }
    }

    /**
     * Log at the end of an iteration
     * @param step Step number
     */
    public static void logFinishedIteration(int step) {
        if (verbosity > 0) {
            log.info(String.format("  Schedule finished at step %d", step));
        }
    }

    /**
     * Logs message at the end of a run.
     *
     * @param totalIter Total number of completed iterations
     * @param newIter Number of newly complexted iterations
     * @param timeSpent Time spent in seconds
     * @param result Result of the run
     */
    public static void logEndOfRun(int totalIter, int newIter, long timeSpent, String result) {
        log.info("--------------------");
        log.info(
                String.format(
                        "Explored %d schedules%s",
                        totalIter, ((totalIter == newIter) ? "" : String.format(" (%d new)", newIter))));
        log.info(
                String.format(
                        "Took %d seconds and %.1f GB", timeSpent, MemoryMonitor.getMaxMemSpent() / 1000.0));
        log.info(String.format("Result: " + result));
        log.info("--------------------");
    }
}