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
import psym.runtime.machine.Machine;
import psym.runtime.machine.State;
import psym.runtime.machine.events.Message;
import psym.valuesummary.Guard;
import psym.valuesummary.PrimitiveVS;

/** Represents the trace logger for P Symbolic */
public class TraceLogger {
  static Logger log = null;
  static LoggerContext context = null;

  @Getter @Setter static int verbosity;

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

  public static void disable() {
    Configurator.setLevel(TraceLogger.class.getName(), Level.OFF);
  }

  public static void enable() {
    Configurator.setLevel(TraceLogger.class.getName(), Level.INFO);
  }

  public static void onProcessEvent(Guard pc, Machine machine, Message message) {
    if (verbosity > 3) {
      String msg =
          String.format(
              "Machine %s is processing event %s in state %s",
              machine, message.getEvent(), machine.getCurrentState().restrict(pc));
      log.info(msg);
    }
  }

  public static void onProcessStateTransition(
      Guard pc, Machine machine, PrimitiveVS<State> newState) {
    if (verbosity > 3) {
      String msg =
          String.format("Machine %s transitioning to state %s", machine.toString(), newState);
      log.info(msg);
    }
  }

  public static void onCreateMachine(Guard pc, Machine machine) {
    if (verbosity > 3) {
      String msg = "Machine " + machine + " was created";
      log.info(msg);
    }
  }

  public static void onMachineStart(Guard pc, Machine machine) {
    if (verbosity > 3) {
      String msg = String.format("Machine %s starting", machine.toString());
      log.info(msg);
    }
  }

  public static void machineState(Guard pc, Machine machine) {
    if (verbosity > 3) {
      String msg =
          String.format("Machine %s in state %s", machine, machine.getCurrentState().restrict(pc));
      log.info(msg);
    }
  }

  public static void handle(Machine m, State st, Message event) {
    if (verbosity > 3) {
      log.info("Machine " + m + " handling event " + event.getEvent() + " in state " + st);
    }
  }

  public static void send(Message effect) {
    if (verbosity > 3) {
      String msg = "Send effect [" + effect.getEvent() + "] to [" + effect.getTarget() + "]";
      log.info(msg);
    }
  }

  public static void unblock(Message effect) {
    if (verbosity > 3) {
      String msg = "Unblock [" + effect.getTarget() + "] on receiving effect [" + effect.getEvent() + "]";
      log.info(msg);
    }
  }

  public static void schedule(int depth, Message effect, Machine sender) {
    if (verbosity > 0) {
      String msg =
              String.format(
                      "  Depth %d: scheduled event[%s] sent to [%s] from [%s]",
                      depth, effect.getEvent().toString(), effect.getTarget(), sender);
      log.info(msg);
    }
  }

  public static void schedule(int depth, Message effect) {
    if (verbosity > 0) {
      String msg =
          String.format(
              "  Depth %d: scheduled event[%s] sent to [%s]",
              depth, effect.getEvent().toString(), effect.getTarget());
      log.info(msg);
    }
  }

  public static void logMessage(String str) {
    if (verbosity > 3) {
      log.info(str);
    }
  }

  public static void log(String str) {
    log.info(str);
  }

  public static void logStartReplayCex() {
    log.info("--------------------");
    log.info("Replaying Counterexample");
    log.info("--------------------");
  }
}
