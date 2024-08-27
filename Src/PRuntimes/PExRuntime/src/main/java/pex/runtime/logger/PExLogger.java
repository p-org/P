package pex.runtime.logger;

import lombok.Setter;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.LoggerContext;
import org.apache.logging.log4j.core.appender.ConsoleAppender;
import org.apache.logging.log4j.core.layout.PatternLayout;
import pex.runtime.PExGlobal;
import pex.runtime.STATUS;
import pex.runtime.scheduler.explicit.ExplicitSearchScheduler;
import pex.runtime.scheduler.explicit.StateCachingMode;
import pex.utils.monitor.MemoryMonitor;

import java.io.PrintWriter;
import java.io.StringWriter;

/**
 * Represents the main PEx logger
 */
public class PExLogger {
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
        log = Log4JConfig.getContext().getLogger(PExLogger.class.getName());
        org.apache.logging.log4j.core.Logger coreLogger =
                (org.apache.logging.log4j.core.Logger) LogManager.getLogger(PExLogger.class.getName());
        context = coreLogger.getContext();

        PatternLayout layout = Log4JConfig.getPatternLayout();
        ConsoleAppender consoleAppender = ConsoleAppender.createDefaultAppenderForLayout(layout);
        consoleAppender.start();

        context.getConfiguration().addLoggerAppender(coreLogger, consoleAppender);
    }

    public static void logInfo(String message) {
        log.info(message);
    }

    /**
     * Logs the given message based on the current verbosity level.
     *
     * @param message Message to print
     */
    public static void logVerbose(String message) {
        if (verbosity > 3) {
            log.info(message);
        }
    }

    /**
     * Logs message at the end of a run.
     *
     * @param scheduler Explicit state search scheduler
     * @param timeSpent Time spent in seconds
     */
    public static void logEndOfRun(ExplicitSearchScheduler scheduler, long timeSpent) {
        if (verbosity == 0) {
            log.info("");
        }
        log.info("--------------------");
        log.info("... Checking statistics:");
        if (PExGlobal.getStatus() == STATUS.BUG_FOUND) {
            log.info("..... Found 1 bug.");
        } else {
            log.info("..... Found 0 bugs.");
        }
        if (scheduler != null) {
            log.info("... Scheduling statistics:");
            if (PExGlobal.getConfig().getStateCachingMode() != StateCachingMode.None) {
                log.info(String.format("..... Explored %d distinct states over %d timelines",
                        PExGlobal.getStateCache().size(), PExGlobal.getTimelines().size()));
            }
            log.info(String.format("..... Explored %d distinct schedules", PExGlobal.getTotalSchedules()));
            log.info(String.format("..... Finished %d search tasks (%d pending)",
                    PExGlobal.getFinishedTasks().size(), PExGlobal.getPendingTasks().size()));
            log.info(String.format("..... Number of steps explored: %d (min), %d (avg), %d (max).",
                    PExGlobal.getMinSteps(), (PExGlobal.getTotalSteps() / PExGlobal.getTotalSchedules()), PExGlobal.getMaxSteps()));
        }
        log.info(String.format("... Elapsed %d seconds and used %.1f GB", timeSpent, MemoryMonitor.getMaxMemSpent() / 1000.0));
        log.info(String.format(".. Result: " + PExGlobal.getResult()));
        log.info(". Done");
    }

    /**
     * Print error trace
     *
     * @param e Exception object
     */
    public static void logStackTrace(Exception e) {
        StringWriter sw = new StringWriter();
        PrintWriter pw = new PrintWriter(sw);
        e.printStackTrace(pw);
        log.info("--------------------");
        log.info(sw.toString());
    }
}
