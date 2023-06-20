package psym.runtime.logger;

import lombok.Setter;
import org.apache.logging.log4j.Level;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.LoggerContext;
import org.apache.logging.log4j.core.appender.ConsoleAppender;
import org.apache.logging.log4j.core.config.Configuration;
import org.apache.logging.log4j.core.config.Configurator;
import org.apache.logging.log4j.core.layout.PatternLayout;
import psym.utils.monitor.MemoryMonitor;

/**
 * Represents the P Symbolic logger configuration
 */
public class PSymLogger {
    static Logger log = null;
    static LoggerContext context = null;
    @Setter
    static int verbosity;

    public static void Initialize(int verb) {
        verbosity = verb;
        log = Log4JConfig.getContext().getLogger(PSymLogger.class.getName());
        org.apache.logging.log4j.core.Logger coreLogger =
                (org.apache.logging.log4j.core.Logger) LogManager.getLogger(PSymLogger.class.getName());
        context = coreLogger.getContext();

        Configuration config = Log4JConfig.getContext().getConfiguration();
        PatternLayout layout = PatternLayout.createDefaultLayout(config);
        ConsoleAppender consoleAppender = ConsoleAppender.createDefaultAppenderForLayout(layout);
        consoleAppender.start();

        context.getConfiguration().addLoggerAppender(coreLogger, consoleAppender);
    }

    public static void disable() {
        Configurator.setLevel(PSymLogger.class.getName(), Level.OFF);
    }

    public static void enable() {
        Configurator.setLevel(PSymLogger.class.getName(), Level.ALL);
    }

    public static void finished(int totalIter, int newIter, long timeSpent, String result, String mode) {
        log.info("--------------------");
        log.info(String.format("Explored %d %s executions%s", totalIter, mode, ((totalIter == newIter) ? "" : String.format(" (%d new)", newIter))));
        log.info(String.format("Took %d seconds and %.1f GB", timeSpent, MemoryMonitor.getMaxMemSpent() / 1000.0));
        log.info(String.format("Result: " + result));
        log.info("--------------------");
    }

    public static void log(String message) {
        if (verbosity > 0) {
            log.info(message);
        }
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

    public static void ResetAllConfigurations(int verbosity, String projectName, String outputFolder) {
        SearchLogger.Initialize(verbosity, outputFolder);
        TraceLogger.Initialize(verbosity, outputFolder);
        StatWriter.Initialize(projectName, outputFolder);
        CoverageWriter.Initialize(projectName, outputFolder);
        ScratchLogger.Initialize(verbosity, outputFolder);
    }
}
