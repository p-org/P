package psym.commandline;

import static java.lang.System.exit;

import java.io.*;
import java.nio.file.Files;
import java.util.Iterator;
import org.apache.commons.cli.*;
import org.json.JSONArray;
import org.json.JSONObject;
import org.json.JSONTokener;
import psym.runtime.PSymGlobal;
import psym.runtime.scheduler.search.choiceorchestration.ChoiceLearningRewardMode;
import psym.runtime.scheduler.search.choiceorchestration.ChoiceLearningStateMode;
import psym.runtime.scheduler.search.choiceorchestration.ChoiceOrchestrationMode;
import psym.runtime.scheduler.search.choiceorchestration.ChoiceOrchestratorEpsilonGreedy;
import psym.runtime.scheduler.search.explicit.StateCachingMode;
import psym.runtime.scheduler.search.symmetry.SymmetryMode;
import psym.runtime.scheduler.search.taskorchestration.TaskOrchestrationMode;
import psym.runtime.scheduler.search.taskorchestration.TaskOrchestratorCoverageEpsilonGreedy;

/** Represents the commandline options for the tool */
public class PSymOptions {

  private static final Options allOptions;
  private static final Options visibleOptions;
  private static final PrintWriter writer = new PrintWriter(System.out);
  private static final HelpFormatter formatter = new HelpFormatter();

  static {
    allOptions = new Options();
    visibleOptions = new Options();

    // Basic options

    // test driver name
    Option testName =
        Option.builder("tc")
            .longOpt("testcase")
            .desc("Test case to explore")
            .numberOfArgs(1)
            .hasArg()
            .argName("Test Case (string)")
            .build();
    addOption(testName);

    // project name
    Option projName =
        Option.builder("pn")
            .longOpt("projname")
            .desc("Project name")
            .numberOfArgs(1)
            .hasArg()
            .argName("Project Name (string)")
            .build();
    addOption(projName);

    // output folder
    Option outputDir =
        Option.builder("o")
            .longOpt("outdir")
            .desc("Dump output to directory (absolute or relative path)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Output Dir (string)")
            .build();
    addOption(outputDir);

    // time limit
    Option timeLimit =
        Option.builder("t")
            .longOpt("timeout")
            .desc("Timeout in seconds (disabled by default)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Time Limit (seconds)")
            .build();
    addOption(timeLimit);

    // memory limit
    Option memLimit =
        Option.builder("m")
            .longOpt("memout")
            .desc("Memory limit in Giga bytes (auto-detect by default)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Memory Limit (GB)")
            .build();
    addOption(memLimit);

    // set the level of verbosity
    Option verbosity =
        Option.builder("v")
            .longOpt("verbose")
            .desc("Level of verbose log output during exploration (default: 0)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Log Verbosity (integer)")
            .build();
    addOption(verbosity);

    // Explore options

    // strategy of exploration
    Option strategy =
        Option.builder("st")
            .longOpt("strategy")
            .desc("Exploration strategy: symbolic, random, dfs, learn (default: symbolic)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Strategy (string)")
            .build();
    addOption(strategy);

    // Systematic exploration options

    // max number of schedules for the search
    Option maxSchedules =
        Option.builder("s")
            .longOpt("schedules")
            .desc("Number of schedules to explore (default: 1)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Schedules (integer)")
            .build();
    addOption(maxSchedules);

    // max steps/depth bound for the search
    Option maxSteps =
        Option.builder("ms")
            .longOpt("max-steps")
            .desc("Max scheduling steps to be explored (default: 10,000)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Max Steps (integer)")
            .build();
    addOption(maxSteps);

    // whether or not to fail on reaching max step bound
    Option failOnMaxSteps =
        Option.builder("fms")
            .longOpt("fail-on-maxsteps")
            .desc("Consider it a bug if the test hits the specified max-steps")
            .numberOfArgs(0)
            .build();
    addOption(failOnMaxSteps);

    // Replay and debug options

    // read replayer state from file
    Option readReplayerFromFile =
        Option.builder("r")
            .longOpt("replay")
            .desc("Schedule file to replay")
            .numberOfArgs(1)
            .hasArg()
            .argName("File Name (string)")
            .build();
    addOption(readReplayerFromFile);

    // Advanced options

    // random seed for the search
    Option randomSeed =
        Option.builder()
            .longOpt("seed")
            .desc("Specify the random value generator seed")
            .numberOfArgs(1)
            .hasArg()
            .argName("Random Seed (integer)")
            .build();
    addOption(randomSeed);

    // max scheduling choice bound for the search
    Option maxSchedBound = Option.builder("sb")
            .longOpt("sch-bound")
            .desc("Max scheduling choice bound at each step during the search (default: unbounded)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Schedule Bound (integer)")
            .build();
    addOption(maxSchedBound);

    // max data choice bound for the search
    Option dataChoiceBound = Option.builder("db")
            .longOpt("data-bound")
            .desc("Max data choice bound at each step during the search (default: unbounded)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Data Bound (integer)")
            .build();
    addOption(dataChoiceBound);


    // psym configuration file
    Option configFile =
        Option.builder()
            .longOpt("config")
            .desc("Name of the JSON configuration file")
            .numberOfArgs(1)
            .hasArg()
            .argName("File Name (string)")
            .build();
    addOption(configFile);

    // Invisible/expert options

    // whether or not to disable sync events
    Option sync =
        Option.builder().longOpt("no-sync").desc("Disable sync events").numberOfArgs(0).build();
    addHiddenOption(sync);

    // whether or not to disable state caching
    Option stateCaching =
        Option.builder()
            .longOpt("state-caching")
            .desc("State caching mode: none, symbolic, exact, fast (default: auto)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Caching Mode (string)")
            .build();
    addHiddenOption(stateCaching);

    // whether or not to enable symmetry
    Option symmetry =
        Option.builder()
            .longOpt("symmetry")
            .desc("Symmetry-aware exploration mode: none, full (default: none)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Symmetry Mode (string)")
            .build();
    addHiddenOption(symmetry);

    // whether or not to disable stateful backtracking
    Option backtrack =
        Option.builder()
            .longOpt("no-backtrack")
            .desc("Disable stateful backtracking")
            .numberOfArgs(0)
            .build();
    addHiddenOption(backtrack);

    // max number of backtrack tasks per execution
    Option maxBacktrackTasksPerExecution =
        Option.builder()
            .longOpt("backtracks-per-schedule")
            .desc("Max number of backtracks to generate per schedule (default: 2)")
            .numberOfArgs(1)
            .hasArg()
            .argName("(integer)")
            .build();
    addHiddenOption(maxBacktrackTasksPerExecution);

    // max number of backtrack tasks per execution
    Option maxPendingBacktrackTasks =
            Option.builder()
                    .longOpt("backtracks-pending")
                    .desc("Max number of pending backtracks (default: 100)")
                    .numberOfArgs(1)
                    .hasArg()
                    .argName("(integer)")
                    .build();
    addHiddenOption(maxPendingBacktrackTasks);

    // mode of choice orchestration
    Option choiceOrch =
        Option.builder("corch")
            .longOpt("choice-orch")
            .desc("Choice orchestration options: none, random, learn (default: none)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Choice Orch. (string)")
            .build();
    addHiddenOption(choiceOrch);

    // mode of task orchestration
    Option taskOrch =
        Option.builder("torch")
            .longOpt("task-orch")
            .desc("Task orchestration options: astar, random, dfs, learn (default: dfs)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Task Orch. (string)")
            .build();
    addHiddenOption(taskOrch);

    // mode of choice learning state mode
    Option choiceLearnState =
        Option.builder()
            .longOpt("learn-state")
            .desc(
                "Learning state options: none, last, states, events, full, timeline (default: timeline)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Learn State (string)")
            .build();
    addHiddenOption(choiceLearnState);

    // mode of choice learning reward mode
    Option choiceLearnReward =
        Option.builder()
            .longOpt("learn-reward")
            .desc("Learning reward options: coverage, fixed (default: coverage)")
            .numberOfArgs(1)
            .hasArg()
            .argName("Learn Reward (string)")
            .build();
    addHiddenOption(choiceLearnReward);

    // epsilon-greedy decay rate
    Option epsilonDecay =
        Option.builder()
            .longOpt("learn-decay")
            .desc("Decay rate for epsilon-greedy")
            .numberOfArgs(1)
            .hasArg()
            .argName("Decay Rate (double)")
            .build();
    addHiddenOption(epsilonDecay);

    // read program state from file
    Option readFromFile =
        Option.builder()
            .longOpt("read")
            .desc("Name of the file with the program state")
            .numberOfArgs(1)
            .hasArg()
            .argName("File Name (string)")
            .build();
    addHiddenOption(readFromFile);

    // Enable writing the program state to file
    Option writeToFile =
        Option.builder()
            .longOpt("write")
            .desc("Enable writing program state")
            .numberOfArgs(0)
            .build();
    addHiddenOption(writeToFile);

    // Help menu
    Option help = Option.builder("h").longOpt("help").desc("Show help menu").build();
    addOption(help);

    Option helpAll = Option.builder().longOpt("help-all").desc("Show complete help menu").build();
    addHiddenOption(helpAll);
  }

  private static void addHiddenOption(Option opt) {
    allOptions.addOption(opt);
  }

  private static void addOption(Option opt) {
    allOptions.addOption(opt);
    visibleOptions.addOption(opt);
  }

  private static void optionError(Option opt, String msg) {
    writer.println(msg);
    if (opt != null) {
      Options opts = new Options();
      opts.addOption(opt);
      formatter.printHelp(writer, 100, opt.getLongOpt(), "", opts, 2, 2, "Try --help for details.");
    }
    writer.flush();
    exit(10);
  }

  public static PSymConfiguration ParseCommandlineArgs(String[] args) {
    // Parse the commandline arguments
    CommandLineParser parser = new DefaultParser();
    formatter.setOptionComparator(null);
    CommandLine cmd = null;
    try {
      cmd = parser.parse(allOptions, args);
    } catch (ParseException e) {
      System.out.println(e.getMessage());
      formatter.printUsage(writer, 100, "java -jar <.jar-file>", allOptions);
      writer.flush();
      System.out.println("Try --help for details.");
      exit(10);
    }

    PSymConfiguration config = new PSymConfiguration();

    if (cmd.getOptionValue("config") == null) {
      // Populate the configuration based on psym-config file (if exists)
      File tempFile = new File("psym-config.json");
      if (tempFile.exists()) {
        readConfigFile(config, tempFile.getAbsolutePath(), null);
      }
    }

    // Populate the configuration based on the commandline arguments
    for (Option option : cmd.getOptions()) {
      switch (option.getLongOpt()) {
          // basic options
        case "tc":
        case "testcase":
          config.setTestDriver(option.getValue());
          break;
        case "projname":
          config.setProjectName(option.getValue());
          break;
        case "o":
        case "outdir":
          config.setOutputFolder(option.getValue());
          break;
        case "t":
        case "timeout":
          try {
            config.setTimeLimit(Double.parseDouble(option.getValue()));
            if (config.getMaxExecutions() == 1) {
              config.setMaxExecutions(0);
            }
          } catch (NumberFormatException ex) {
            optionError(
                option, String.format("Expected a double value, got %s", option.getValue()));
          }
          break;
        case "m":
        case "memout":
          try {
            config.setMemLimit(Double.parseDouble(option.getValue()) * 1024);
          } catch (NumberFormatException ex) {
            optionError(
                option, String.format("Expected a double value, got %s", option.getValue()));
          }
          break;
        case "v":
        case "verbose":
          try {
            config.setVerbosity(Integer.parseInt(option.getValue()));
          } catch (NumberFormatException ex) {
            optionError(
                option, String.format("Expected an integer value, got %s", option.getValue()));
          }
          break;
        case "st":
        case "strategy":
          switch (option.getValue()) {
            case "bmc":
            case "sym":
            case "symex":
            case "symbolic":
              config.setToSymbolic();
              break;
            case "symbolic-fixpoint":
              config.setToSymbolicFixpoint();
              break;
            case "symbolic-bounded":
              config.setToSymbolicBounded();
              break;
            case "random":
              config.setToRandom();
              break;
            case "dfs":
              config.setToDfs();
              break;
            case "learn":
              config.setToLearn();
              break;
            case "fuzz":
            case "stateless":
              config.setToStateless();
              break;
            default:
              optionError(
                  option,
                  String.format("Unrecognized strategy of exploration, got %s", option.getValue()));
          }
          break;
          // exploration options
        case "i":
        case "iterations":
        case "s":
        case "schedules":
          try {
            config.setMaxExecutions(Integer.parseInt(option.getValue()));
          } catch (NumberFormatException ex) {
            optionError(
                option, String.format("Expected an integer value, got %s", option.getValue()));
          }
          break;
        case "ms":
        case "max-steps":
          try {
            config.setMaxStepBound(Integer.parseInt(option.getValue()) + 1);
          } catch (NumberFormatException ex) {
            optionError(
                option, String.format("Expected an integer value, got %s", option.getValue()));
          }
          break;
        case "fms":
        case "fail-on-maxsteps":
          config.setFailOnMaxStepBound(true);
          break;
          // replay options
        case "r":
        case "replay":
          config.setReadScheduleFromFile(option.getValue());
          File file = new File(config.getReadScheduleFromFile());
          try {
            file.getCanonicalPath();
          } catch (IOException e) {
            optionError(
                option, String.format("File %s does not exist", config.getReadScheduleFromFile()));
          }
          break;
          // advanced options
        case "seed":
          try {
            config.setRandomSeed(Long.parseLong(option.getValue()));
          } catch (NumberFormatException ex) {
            optionError(
                option, String.format("Expected an integer value, got %s", option.getValue()));
          }
          break;
        case "sb":
        case "sch-bound":
          try {
            config.setSchChoiceBound(Integer.parseInt(option.getValue()));
          } catch (NumberFormatException ex) {
            optionError(option, String.format("Expected an integer value, got %s", option.getValue()));
          }
          break;
        case "db":
        case "data-bound":
          try {
            config.setDataChoiceBound(Integer.parseInt(option.getValue()));
          } catch (NumberFormatException ex) {
            optionError(option, String.format("Expected an integer value, got %s", option.getValue()));
          }
          break;
        case "config":
          readConfigFile(config, option.getValue(), option);
          break;
        case "no-sync":
          config.setAllowSyncEvents(false);
          break;
        case "state-caching":
          switch (option.getValue()) {
            case "none":
              config.setStateCachingMode(StateCachingMode.None);
              break;
            case "sym":
            case "symbolic":
              config.setStateCachingMode(StateCachingMode.Symbolic);
              break;
            case "exact":
              config.setStateCachingMode(StateCachingMode.ExplicitExact);
              break;
            case "fast":
              config.setStateCachingMode(StateCachingMode.ExplicitFast);
              break;
            default:
              optionError(
                  option,
                  String.format("Unrecognized state hashing mode, got %s", option.getValue()));
          }
          break;
        case "symmetry":
          switch (option.getValue()) {
            case "none":
              config.setSymmetryMode(SymmetryMode.None);
              break;
            case "full":
              config.setSymmetryMode(SymmetryMode.Full);
              break;
            default:
              optionError(
                  option, String.format("Unrecognized symmetry mode, got %s", option.getValue()));
          }
          break;
        case "no-backtrack":
          config.setUseBacktrack(false);
          break;
        case "backtracks-per-schedule":
          try {
            config.setMaxBacktrackTasksPerExecution(Integer.parseInt(option.getValue()));
          } catch (NumberFormatException ex) {
            optionError(
                option, String.format("Expected an integer value, got %s", option.getValue()));
          }
          break;
        case "backtracks-pending":
          try {
            config.setMaxPendingBacktrackTasks(Integer.parseInt(option.getValue()));
          } catch (NumberFormatException ex) {
            optionError(
                    option, String.format("Expected an integer value, got %s", option.getValue()));
          }
          break;
        case "corch":
        case "choice-orch":
          switch (option.getValue()) {
            case "none":
              config.setChoiceOrchestration(ChoiceOrchestrationMode.None);
              break;
            case "random":
              config.setChoiceOrchestration(ChoiceOrchestrationMode.Random);
              break;
            case "learn-ql":
              config.setChoiceOrchestration(ChoiceOrchestrationMode.QLearning);
              break;
            case "learn":
            case "learn-eg":
              config.setChoiceOrchestration(ChoiceOrchestrationMode.EpsilonGreedy);
              break;
            default:
              optionError(
                  option,
                  String.format(
                      "Unrecognized choice orchestration mode, got %s", option.getValue()));
          }
          break;
        case "learn-mode":
        case "learn-state":
          switch (option.getValue()) {
            case "none":
              config.setChoiceLearningStateMode(ChoiceLearningStateMode.None);
              break;
            case "depth":
              config.setChoiceLearningStateMode(ChoiceLearningStateMode.SchedulerDepth);
              break;
            case "last":
              config.setChoiceLearningStateMode(ChoiceLearningStateMode.LastStep);
              break;
            case "states":
              config.setChoiceLearningStateMode(ChoiceLearningStateMode.MachineState);
              break;
            case "states+last":
              config.setChoiceLearningStateMode(ChoiceLearningStateMode.MachineStateAndLastStep);
              break;
            case "events":
              config.setChoiceLearningStateMode(ChoiceLearningStateMode.MachineStateAndEvents);
              break;
            case "full":
              config.setChoiceLearningStateMode(ChoiceLearningStateMode.FullState);
              break;
            case "timeline":
              config.setChoiceLearningStateMode(ChoiceLearningStateMode.TimelineAbstraction);
              break;
            default:
              optionError(
                  option,
                  String.format(
                      "Unrecognized choice learning state mode, got %s", option.getValue()));
          }
          break;
        case "learn-reward":
          switch (option.getValue()) {
            case "none":
              config.setChoiceLearningRewardMode(ChoiceLearningRewardMode.None);
              break;
            case "fixed":
              config.setChoiceLearningRewardMode(ChoiceLearningRewardMode.Fixed);
              break;
            case "coverage":
              config.setChoiceLearningRewardMode(ChoiceLearningRewardMode.Coverage);
              break;
            default:
              optionError(
                  option,
                  String.format(
                      "Unrecognized choice learning reward mode, got %s", option.getValue()));
          }
          break;
        case "learn-decay":
          try {
            ChoiceOrchestratorEpsilonGreedy.setEPSILON_DECAY_FACTOR(
                Double.parseDouble(option.getValue()));
            TaskOrchestratorCoverageEpsilonGreedy.setEPSILON_DECAY_FACTOR(
                Double.parseDouble(option.getValue()));
          } catch (NumberFormatException ex) {
            optionError(
                option, String.format("Expected a double value, got %s", option.getValue()));
          }
          break;
        case "torch":
        case "task-orch":
          switch (option.getValue()) {
            case "dfs":
              config.setTaskOrchestration(TaskOrchestrationMode.DepthFirst);
              break;
            case "random":
              config.setTaskOrchestration(TaskOrchestrationMode.Random);
              break;
            case "astar":
              config.setTaskOrchestration(TaskOrchestrationMode.CoverageAStar);
              break;
            case "learn":
            case "learn-eg":
              config.setTaskOrchestration(TaskOrchestrationMode.CoverageEpsilonGreedy);
              break;
            default:
              optionError(
                  option,
                  String.format("Unrecognized task orchestration mode, got %s", option.getValue()));
          }
          break;
        case "read":
          config.setReadFromFile(option.getValue());
          File replayFile = new File(config.getReadFromFile());
          try {
            replayFile.getCanonicalPath();
          } catch (IOException e) {
            optionError(option, String.format("File %s does not exist", config.getReadFromFile()));
          }
          break;
        case "write":
          config.setWriteToFile(true);
          break;
        case "h":
        case "help":
          formatter.printHelp(
              100,
              "java -jar <.jar-file> [options]",
              "-----------------------------------\nCommandline options for PSym/PCover\n-----------------------------------",
              visibleOptions,
              "See https://p-org.github.io/P/ for details.");
          exit(0);
          break;
        case "help-all":
          formatter.printHelp(
                  100,
                  "java -jar <.jar-file> [options]",
                  "-----------------------------------\nCommandline options for PSym/PCover\n-----------------------------------",
                  allOptions,
                  "See https://p-org.github.io/P/ for details.");
          exit(0);
          break;
        default:
          optionError(option, String.format("Unrecognized option %s", option));
      }
    }

    // post process
    if (!config.isChoiceOrchestrationLearning()) {
      config.setChoiceLearningRewardMode(ChoiceLearningRewardMode.None);
    }
    return config;
  }

  public static void readConfigFile(
      PSymConfiguration config, String configFileName, Option option) {
    config.setConfigFile(configFileName);
    File configFile = new File(config.getConfigFile());
    try {
      configFile.getCanonicalPath();
      ParseConfigFile(config, configFile);
    } catch (IOException e) {
      optionError(option, String.format("File %s does not exist", config.getConfigFile()));
    }
  }

  private static void ParseConfigFile(PSymConfiguration config, File configFile)
      throws IOException {
    InputStream configStream = Files.newInputStream(configFile.toPath());
    JSONTokener jsonTokener = new JSONTokener(configStream);
    JSONObject jsonObject = new JSONObject(jsonTokener);

    Iterator<String> keys = jsonObject.keys();

    while (keys.hasNext()) {
      String key = keys.next();
      if (jsonObject.get(key) instanceof JSONObject) {
        JSONObject value = (JSONObject) jsonObject.get(key);
        switch (key) {
          case "sync-events":
            JSONArray allSyncEvents = value.getJSONArray("default");
            for (int i = 0; i < allSyncEvents.length(); i++) {
              JSONObject element = allSyncEvents.getJSONObject(i);
              String machineName = element.getString("machine");
              JSONArray syncEvents = element.getJSONArray("events");
              for (int j = 0; j < syncEvents.length(); j++) {
                String syncEventName = syncEvents.getString(j);
                PSymGlobal.addSyncEvent(machineName, syncEventName);
              }
            }
            break;
          case "symmetric-machines":
            JSONArray symMachineTypes = value.getJSONArray("default");
            for (int i = 0; i < symMachineTypes.length(); i++) {
              String symTypeName = symMachineTypes.getString(i);
              PSymGlobal.getSymmetryTracker().addSymmetryType(symTypeName);
            }
            break;
          default:
            optionError(null, String.format("Unrecognized key %s in config file", key));
        }
      }
    }
  }
}
