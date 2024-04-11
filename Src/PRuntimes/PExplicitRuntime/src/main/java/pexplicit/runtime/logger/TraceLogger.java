package pexplicit.runtime.logger;

import lombok.Getter;
import lombok.Setter;
import org.apache.logging.log4j.Level;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.Appender;
import org.apache.logging.log4j.core.LoggerContext;
import org.apache.logging.log4j.core.appender.ConsoleAppender;
import org.apache.logging.log4j.core.appender.OutputStreamAppender;
import org.apache.logging.log4j.core.config.Configurator;
import org.apache.logging.log4j.core.layout.PatternLayout;
import pexplicit.runtime.machine.PMachine;
import pexplicit.runtime.machine.events.PMessage;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.util.Date;

/**
 * Represents the trace logger that logs on every schedule step.
 */
public class TraceLogger {
    static Logger log = null;
    static LoggerContext context = null;

    @Getter
    @Setter
    static int verbosity;

    /**
     * Initialize the logger
     *
     * @param verb         Verbosity level
     * @param outputFolder Output folder where trace log file resides
     */
    public static void Initialize(int verb, String outputFolder) {
        verbosity = verb;
        log = Log4JConfig.getContext().getLogger(TraceLogger.class.getName());
        org.apache.logging.log4j.core.Logger coreLogger =
                (org.apache.logging.log4j.core.Logger) LogManager.getLogger(TraceLogger.class.getName());
        context = coreLogger.getContext();

        try {
            // get new file name
            Date date = new Date();
            String fileName = outputFolder + "/trace-" + date + ".log";
            File file = new File(fileName);
            file.getParentFile().mkdirs();
            file.createNewFile();

            // Define new file printer
            FileOutputStream fout = new FileOutputStream(fileName, false);

            PatternLayout layout = Log4JConfig.getPatternLayout();
            Appender fileAppender =
                    OutputStreamAppender.createAppender(layout, null, fout, fileName, false, true);
            ConsoleAppender consoleAppender = ConsoleAppender.createDefaultAppenderForLayout(layout);
            fileAppender.start();
            consoleAppender.start();

            context.getConfiguration().addLoggerAppender(coreLogger, fileAppender);
            context.getConfiguration().addLoggerAppender(coreLogger, consoleAppender);
        } catch (IOException e) {
            System.out.println("Failed to set printer to the TraceLogger!!");
        }
    }

    /**
     * Disable the logger
     */
    public static void disable() {
        Configurator.setLevel(TraceLogger.class.getName(), Level.OFF);
    }

    /**
     * Enable the logger
     */
    public static void enable() {
        Configurator.setLevel(TraceLogger.class.getName(), Level.INFO);
    }

    /**
     * Log when a machine sends an event to another machine.
     *
     * @param message Message that is sent
     */

    public static void send(PMessage message) {
        if (verbosity > 3) {
            String msg = String.format("Send %s to %s", message.getEvent(), message.getTarget());
            log.info(msg);
        }
    }

    /**
     * Log when a machine unblocks on receiving an event
     *
     * @param message
     */
    public static void unblock(PMessage message) {
        if (verbosity > 3) {
            String msg = String.format("Unblock %s on receiving %s", message.getTarget(), message.getEvent());
            log.info(msg);
        }
    }

    /**
     * Log when a machine schedules an event.
     *
     * @param depth   Schedule depth
     * @param message Message that is scheduled
     * @param sender  Sender machine
     */

    public static void schedule(int depth, PMessage message, PMachine sender) {
        if (verbosity > 0) {
            String msg =
                    String.format("  Depth %d: %s sent %s to %s", depth, sender, message.getEvent(), message.getTarget());
            log.info(msg);
        }
    }

    /**
     * Log at the start of replaying a counterexample
     */
    public static void logStartReplayCex() {
        log.info("------------------------");
        log.info("Replaying Counterexample");
        log.info("-----------------------");
    }
}
