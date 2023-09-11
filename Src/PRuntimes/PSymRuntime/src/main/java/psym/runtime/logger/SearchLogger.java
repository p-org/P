package psym.runtime.logger;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.util.Date;
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
import psym.runtime.statistics.SearchStats;

public class SearchLogger {
  static Logger log = null;
  static LoggerContext context = null;

  @Getter @Setter static int verbosity;

  public static void Initialize(int verb, String outputFolder) {
    verbosity = verb;
    log = Log4JConfig.getContext().getLogger(SearchLogger.class.getName());
    org.apache.logging.log4j.core.Logger coreLogger =
        (org.apache.logging.log4j.core.Logger) LogManager.getLogger(SearchLogger.class.getName());
    context = coreLogger.getContext();

    try {
      // get new file name
      Date date = new Date();
      String fileName = outputFolder + "/searchStats-" + date + ".log";
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
      System.out.println("Failed to set printer to the SearchLogger!!");
    }
  }

  public static void disable() {
    Configurator.setLevel(SearchLogger.class.getName(), Level.OFF);
  }

  public static void enable() {
    Configurator.setLevel(SearchLogger.class.getName(), Level.INFO);
  }

  public static void logMessage(String str) {
    if (verbosity > 3) {
      log.info(str);
    }
  }

  public static void log(String message) {
    log.info(message);
  }

  public static void log(String key, String value) {
    log(String.format("%-40s%s", key + ":", value));
  }

  public static void finishedExecution(int depth) {
    if (verbosity > 0) {
      log.info(String.format("  Execution finished at depth %d", depth));
    }
  }

  public static void logResumeExecution(int iter, int depth) {
    if (verbosity > 0) {
      log.info("--------------------");
      log.info("Resuming Schedule: " + iter + " from Depth: " + depth);
    }
  }

  public static void logStartExecution(int iter, int depth) {
    if (verbosity > 0) {
      log.info("--------------------");
      log.info("Starting Schedule: " + iter + " from Depth: " + depth);
    }
  }

  public static void logDepthStats(SearchStats.DepthStats depthStats) {
    log.info(
        String.format(
            "Depth: %d: TotalTransitions = %d, ReducedTransitionsExplored = %d",
            depthStats.getDepth(),
            depthStats.getNumOfTransitions(),
            depthStats.getNumOfTransitionsExplored()));
  }

  public static void logIterationStats(SearchStats.IterationStats iterStats) {
    log.info(
        String.format(
            "Finished Schedule: %d: Max Depth: %d, TotalStates = %d, TotalTransitions = %d, ReducedTransitionsExplored = %d",
            iterStats.getSchedule(),
            iterStats.getIterationTotal().getDepth(),
            iterStats.getIterationTotal().getNumOfStates(),
            iterStats.getIterationTotal().getNumOfTransitions(),
            iterStats.getIterationTotal().getNumOfTransitionsExplored()));
  }
}
