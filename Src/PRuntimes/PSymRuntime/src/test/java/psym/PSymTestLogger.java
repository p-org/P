package psym;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.util.Date;
import org.apache.logging.log4j.Level;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.logging.log4j.core.Appender;
import org.apache.logging.log4j.core.LoggerContext;
import org.apache.logging.log4j.core.appender.ConsoleAppender;
import org.apache.logging.log4j.core.appender.OutputStreamAppender;
import org.apache.logging.log4j.core.config.Configurator;
import org.apache.logging.log4j.core.layout.PatternLayout;
import psym.runtime.logger.Log4JConfig;

/** Represents the P Symbolic test logger configuration */
public class PSymTestLogger {
  static Logger log = null;
  static LoggerContext context = null;

  public static void Initialize(String outputFolder) {
    log = Log4JConfig.getContext().getLogger(PSymTestLogger.class.getName());
    org.apache.logging.log4j.core.Logger coreLogger =
        (org.apache.logging.log4j.core.Logger) LogManager.getLogger(PSymTestLogger.class.getName());
    context = coreLogger.getContext();

    try {
      // get new file name
      Date date = new Date();
      String fileName = outputFolder + "/test-" + date + ".log";
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
      System.out.println("Failed to set printer to the PSymTestLogger!!");
    }
  }

  public static void disable() {
    Configurator.setLevel(PSymTestLogger.class.getName(), Level.OFF);
  }

  public static void enable() {
    Configurator.setLevel(PSymTestLogger.class.getName(), Level.ALL);
  }

  public static void log(String message) {
    log.info(message);
  }

  public static void warn(String message) {
    log.warn(message);
  }

  public static void error(String message) {
    log.error(message);
  }
}
