package psym;

import java.util.Optional;
import java.util.Set;
import org.reflections.Reflections;
import psym.commandline.PSymOptions;
import psym.runtime.PSymGlobal;
import psym.runtime.PTestDriver;
import psym.runtime.Program;
import psym.runtime.logger.*;
import psym.runtime.scheduler.replay.ReplayScheduler;
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
    PSymGlobal.setConfiguration(PSymOptions.ParseCommandlineArgs(args));
    PSymLogger.Initialize(PSymGlobal.getConfiguration().getVerbosity());

    try {
      if (PSymGlobal.getConfiguration().getReadFromFile().equals("")) {
        Set<Class<? extends Program>> subTypesProgram = reflections.getSubTypesOf(Program.class);
        if (subTypesProgram.size() == 0) {
          throw new Exception("No program found.");
        }

        Optional<Class<? extends Program>> program = subTypesProgram.stream().findFirst();
        p = program.get().getDeclaredConstructor().newInstance();
        setProjectName(p);
      }
    } catch (Exception ex) {
      ex.printStackTrace();
      System.exit(5);
    }

    setup();

    int exit_code = 0;
    try {
      if (PSymGlobal.getConfiguration().isWriteToFile()) {
        BacktrackWriter.Initialize(PSymGlobal.getConfiguration().getProjectName(), PSymGlobal.getConfiguration().getOutputFolder());
      }

      if (!PSymGlobal.getConfiguration().getReadScheduleFromFile().equals("")) {
        // replay mode
        assert (p != null);
        setTestDriver(p, reflections);
        ReplayScheduler replayScheduler =
            ReplayScheduler.readFromFile(PSymGlobal.getConfiguration().getReadScheduleFromFile());
        EntryPoint.replayBug(replayScheduler);
        throw new Exception("ERROR");
      } else if(!PSymGlobal.getConfiguration().getReadFromFile().equals("")){
        // resume mode
        EntryPoint.resume();
      } else {
        // default mode
        assert (p != null);
        setTestDriver(p, reflections);
        EntryPoint.run(p);
      }

      if (PSymGlobal.getConfiguration().isWriteToFile()) {
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
      StatWriter.log("result", PSymGlobal.getResult());
      StatWriter.log("status", String.format("%s", PSymGlobal.getStatus()));
      StatWriter.log("exit-code", String.format("%d", exit_code));
      System.exit(exit_code);
    }
  }

  private static void setup() {
    PSymLogger.ResetAllConfigurations(
            PSymGlobal.getConfiguration().getVerbosity(), PSymGlobal.getConfiguration().getProjectName(), PSymGlobal.getConfiguration().getOutputFolder());
    SolverEngine.resetEngine(PSymGlobal.getConfiguration().getSolverType(), PSymGlobal.getConfiguration().getExprLibType());
    PSymGlobal.initializeSymmetryTracker(PSymGlobal.getConfiguration().isSymbolic());
    RandomNumberGenerator.setup(PSymGlobal.getConfiguration().getRandomSeed());
    MemoryMonitor.setup(PSymGlobal.getConfiguration().getMemLimit());
    TimeMonitor.setup(PSymGlobal.getConfiguration().getTimeLimit());
  }

  /**
   * Set the project name
   *
   * @param p Input program instance
   */
  private static void setProjectName(Program p) {
    if (PSymGlobal.getConfiguration().getProjectName().equals("default")) {
      PSymGlobal.getConfiguration().setProjectName(sanitizeProgramName(p.getClass().getSimpleName()));
    }
  }

  private static String sanitizeProgramName(String name) {
    int index = name.lastIndexOf("Program");
    if (index > 0) {
      name = name.substring(0, index);
    }
    return name;
  }

  /**
   * Set the test driver for the program
   *
   * @param p Input program instance
   * @throws Exception Throws exception if test driver is not found
   */
  private static void setTestDriver(Program p, Reflections reflections)
      throws Exception {
    final String name = sanitizeTestName(PSymGlobal.getConfiguration().getTestDriver());
    final String defaultTestDriver = sanitizeTestName(PSymGlobal.getConfiguration().getTestDriverDefault());

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
        throw new Exception("No test driver found named \"" + PSymGlobal.getConfiguration().getTestDriver() + "\"");
      } else {
        System.exit(6);
      }
    }
    PSymGlobal.getConfiguration().setTestDriver(driver.getClass().getSimpleName());
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
    PSymGlobal.setConfiguration(PSymOptions.ParseCommandlineArgs(new String[0]));
    PSymGlobal.getConfiguration().setOutputFolder(outputFolder);
    setup();
  }
}
