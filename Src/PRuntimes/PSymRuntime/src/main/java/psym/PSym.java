package psym;

import java.util.Optional;
import java.util.Set;
import org.reflections.Reflections;
import psym.commandline.PSymConfiguration;
import psym.commandline.PSymOptions;
import psym.runtime.GlobalData;
import psym.runtime.PTestDriver;
import psym.runtime.Program;
import psym.runtime.logger.*;
import psym.runtime.scheduler.replay.ReplayScheduler;
import psym.runtime.statistics.SolverStats;
import psym.utils.exception.BugFoundException;
import psym.utils.monitor.MemoryMonitor;
import psym.utils.monitor.TimeMonitor;
import psym.utils.random.RandomNumberGenerator;
import psym.valuesummary.solvers.SolverEngine;

public class PSym {

  public static void main(String[] args) {
    Log4JConfig.configureLog4J();
    Reflections reflections = new Reflections("psym.model");
    Program p = null;

    // parse the commandline arguments to create the configuration
    PSymConfiguration config = PSymOptions.ParseCommandlineArgs(args);
    PSymLogger.Initialize(config.getVerbosity());

    try {
      if (config.getReadFromFile().equals("") && config.getReadReplayerFromFile().equals("")) {
        Set<Class<? extends Program>> subTypesProgram = reflections.getSubTypesOf(Program.class);
        if (subTypesProgram.size() == 0) {
          throw new Exception("No program found.");
        }

        Optional<Class<? extends Program>> program = subTypesProgram.stream().findFirst();
        p = program.get().getDeclaredConstructor().newInstance();
        setProjectName(p, config);
      }
    } catch (Exception ex) {
      ex.printStackTrace();
      System.exit(5);
    }

    setup(config);

    int exit_code = 0;
    try {
      if (!config.getReadReplayerFromFile().equals("")) {
        ReplayScheduler replayScheduler =
            ReplayScheduler.readFromFile(config.getReadReplayerFromFile());
        EntryPoint.replayBug(replayScheduler, config);
        throw new Exception("ERROR");
      }

      if (config.isWriteToFile()) {
        BacktrackWriter.Initialize(config.getProjectName(), config.getOutputFolder());
      }

      if (config.getReadFromFile().equals("")) {
        assert (p != null);
        setTestDriver(p, config, reflections);
        EntryPoint.run(config, p);
      } else {
        EntryPoint.resume(config);
      }

      if (config.isWriteToFile()) {
        EntryPoint.writeToFile();
      }
    } catch (BugFoundException e) {
      exit_code = 2;
    } catch (Exception ex) {
      if (ex.getMessage().equals("TIMEOUT")) {
        exit_code = 3;
      } else if (ex.getMessage().equals("MEMOUT")) {
        exit_code = 4;
      } else {
        ex.printStackTrace();
        exit_code = 5;
      }
    } finally {
      double postSearchTime = TimeMonitor.getInstance().stopInterval();
      StatWriter.log("time-post-seconds", String.format("%.1f", postSearchTime));
      System.exit(exit_code);
    }
  }

  private static void setup(PSymConfiguration config) {
    PSymLogger.ResetAllConfigurations(
        config.getVerbosity(), config.getProjectName(), config.getOutputFolder());
    SolverEngine.resetEngine(config.getSolverType(), config.getExprLibType());
    GlobalData.initializeSymmetryTracker(config.isSymbolic());
    SolverStats.setTimeLimit(config.getTimeLimit());
    SolverStats.setMemLimit(config.getMemLimit());
    MemoryMonitor.setup();
    RandomNumberGenerator.setup(config.getRandomSeed());
    TimeMonitor.setup(config.getTimeLimit());
  }

  /**
   * Set the project name
   *
   * @param p Input program instance
   * @param config Input PSymConfiguration
   */
  private static void setProjectName(Program p, PSymConfiguration config) {
    if (config.getProjectName().equals("default")) {
      config.setProjectName(p.getClass().getSimpleName());
    }
  }

  /**
   * Set the test driver for the program
   *
   * @param p Input program instance
   * @param config Input PSymConfiguration
   * @throws Exception Throws exception if test driver is not found
   */
  private static void setTestDriver(Program p, PSymConfiguration config, Reflections reflections)
      throws Exception {
    final String name = sanitizeTestName(config.getTestDriver());
    final String defaultTestDriver = sanitizeTestName(config.getTestDriverDefault());

    Set<Class<? extends PTestDriver>> subTypesDriver = reflections.getSubTypesOf(PTestDriver.class);
    PTestDriver driver = null;
    for (Class<? extends PTestDriver> td : subTypesDriver) {
      if (sanitizeTestName(td.getSimpleName()).equals(name)) {
        driver = td.getDeclaredConstructor().newInstance();
        break;
      }
    }
    if (driver == null && name.equals(defaultTestDriver) && subTypesDriver.size() == 1) {
      for (Class<? extends PTestDriver> td : subTypesDriver) {
        driver = td.getDeclaredConstructor().newInstance();
        break;
      }
    }
    if (driver == null) {
      if (!name.equals(defaultTestDriver)) {
        PSymLogger.info("No test driver found named \"" + name + "\"");
      }
      PSymLogger.info(
          String.format(
              "Error: We found '%d' test cases. Please provide a more precise name of the test case you wish to check using (--testcase | -tc).",
              subTypesDriver.size()));
      PSymLogger.info("Possible options are:");
      for (Class<? extends PTestDriver> td : subTypesDriver) {
        PSymLogger.info(String.format("%s", td.getSimpleName()));
      }
      if (!name.equals(defaultTestDriver)) {
        throw new Exception("No test driver found named \"" + config.getTestDriver() + "\"");
      } else {
        System.exit(6);
      }
    }
    config.setTestDriver(driver.getClass().getSimpleName());
    p.setTestDriver(driver);
  }

  private static String sanitizeTestName(String name) {
    String result = name.toLowerCase();
    result = result.replaceFirst("^pimplementation.", "");
    int index = result.lastIndexOf(".execute");
    if (index > 0) {
      result = result.substring(0, index);
    }
    return result;
  }

  public static void initializeDefault(String outputFolder) {
    Log4JConfig.configureLog4J();
    SearchLogger.disable();
    TraceLogger.disable();
    // parse the commandline arguments to create the configuration
    PSymConfiguration config = PSymOptions.ParseCommandlineArgs(new String[0]);
    config.setOutputFolder(outputFolder);
    setup(config);
  }
}
