package psym.commandline;

import org.apache.commons.cli.*;
import psym.runtime.scheduler.choiceorchestration.ChoiceOrchestrationMode;
import psym.runtime.scheduler.taskorchestration.TaskOrchestrationMode;
import psym.valuesummary.solvers.SolverType;
import psym.valuesummary.solvers.sat.expr.ExprLibType;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;

/**
 * Represents the commandline options for the tool
 */
public class PSymOptions {

    private static final Options options;
    private static HelpFormatter formatter = new HelpFormatter();
    private static final PrintWriter writer = new PrintWriter(System.out);

    static {
        options = new Options();

        // mode of exploration
        Option mode = Option.builder("mode")
                .longOpt("mode")
                .desc("Mode of exploration: default, bmc, random, fuzz, dfs, learn")
                .numberOfArgs(1)
                .hasArg()
                .argName("Mode (string)")
                .build();
        options.addOption(mode);

        // time limit
        Option timeLimit = Option.builder("tl")
                .longOpt("time-limit")
                .desc("Time limit in seconds (default: 60). Use 0 for no limit.")
                .numberOfArgs(1)
                .hasArg()
                .argName("Time Limit (seconds)")
                .build();
        options.addOption(timeLimit);

        // memory limit
        Option memLimit = Option.builder("ml")
                .longOpt("memory-limit")
                .desc("Memory limit in megabytes (MB). Use 0 for no limit.")
                .numberOfArgs(1)
                .hasArg()
                .argName("Memory Limit (MB)")
                .build();
        options.addOption(memLimit);

        // random seed for the search
        Option randomSeed = Option.builder("seed")
                .longOpt("seed")
                .desc("Random seed for the search (default: auto)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Random Seed (integer)")
                .build();
        options.addOption(randomSeed);

        // test driver name
        Option testName = Option.builder("m")
                .longOpt("method")
                .desc("Name of the test method from where the symbolic engine should start exploration")
                .numberOfArgs(1)
                .hasArg()
                .argName("Test Method (string)")
                .build();
        options.addOption(testName);

        // project name
        Option projectName = Option.builder("p")
                .longOpt("project")
                .desc("Name of the project (default: auto)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Project Name (string)")
                .build();
        options.addOption(projectName);

        // output folder
        Option outputDir = Option.builder("o")
                .longOpt("outdir")
                .desc("Name of the output directory (default: output)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Output Dir (string)")
                .build();
        options.addOption(outputDir);

        // read replayer state from file
        Option readReplayerFromFile = Option.builder("replay")
                .longOpt("replay")
                .desc("Name of the .schedule file with the counterexample")
                .numberOfArgs(1)
                .hasArg()
                .argName("File Name (string)")
                .build();
        options.addOption(readReplayerFromFile);

        // max steps/depth bound for the search
        Option maxSteps = Option.builder("ms")
                .longOpt("max-steps")
                .desc("Max scheduling steps to be explored (default: 1000)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Steps (integer)")
                .build();
        options.addOption(maxSteps);

        // max number of executions for the search
        Option maxExecutions = Option.builder("i")
                .longOpt("iterations")
                .desc("Number of schedules/executions to explore (default: no-limit)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Executions (integer)")
                .build();
        options.addOption(maxExecutions);

        // max scheduling choice bound for the search
        Option maxSchedBound = Option.builder("sb")
                .longOpt("sched-bound")
                .desc("Max scheduling choice bound at each step during the search (default: 1)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Schedule Bound (integer)")
                .build();
        options.addOption(maxSchedBound);

        // max data choice bound for the search
        Option dataChoiceBound = Option.builder("db")
                .longOpt("data-bound")
                .desc("Max data choice bound at each step during the search (default: 1)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Data Bound (integer)")
                .build();
        options.addOption(dataChoiceBound);

        // whether or not to disable state caching
        Option noStateCaching = Option.builder("nsc")
                .longOpt("no-state-caching")
                .desc("Disable state caching")
                .numberOfArgs(0)
                .build();
        options.addOption(noStateCaching);

        // whether or not to disable stateful backtracking
        Option backtrack = Option.builder("nb")
                .longOpt("no-backtrack")
                .desc("Disable stateful backtracking")
                .numberOfArgs(0)
                .build();
        options.addOption(backtrack);

        // mode of choice orchestration
        Option choiceOrch = Option.builder("corch")
                .longOpt("choice-orch")
                .desc("Choice orchestration options: random, learn (default: random)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Choice Orch. (string)")
                .build();
        options.addOption(choiceOrch);

        // mode of task orchestration
        Option taskOrch = Option.builder("torch")
                .longOpt("task-orch")
                .desc("Task orchestration options: astar, random, dfs, learn (default: astar)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Task Orch. (string)")
                .build();
        options.addOption(taskOrch);

        // max number of backtrack tasks per execution
        Option maxBacktrackTasksPerExecution = Option.builder("bpe")
                .longOpt("backtracks-per-exe")
                .desc("Max number of backtracks to generate per execution (default: 2)")
                .numberOfArgs(1)
                .hasArg()
                .argName("(integer)")
                .build();
        options.addOption(maxBacktrackTasksPerExecution);

        // solver type
        Option solverType = Option.builder("st")
                .longOpt("solver")
                .desc("Solver type to use: bdd, yices2, z3, cvc5 (default: bdd)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Solver Type (string)")
                .build();
        options.addOption(solverType);

        // expression type
        Option exprLibType = Option.builder("et")
                .longOpt("expr")
                .desc("Expression type to use: bdd, fraig, aig, native (default: bdd)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Expression Type (string)")
                .build();
        options.addOption(exprLibType);

        // read program state from file
        Option readFromFile = Option.builder("r")
                .longOpt("read")
                .desc("Name of the file with the program state")
                .numberOfArgs(1)
                .hasArg()
                .argName("File Name (string)")
                .build();
        options.addOption(readFromFile);

        // Enable writing the program state to file
        Option writeToFile = Option.builder("w")
                .longOpt("write")
                .desc("Enable writing program state")
                .numberOfArgs(0)
                .build();
        options.addOption(writeToFile);

        // whether or not to disable filter-based reductions
        Option filters = Option.builder("nf")
                .longOpt("no-filters")
                .desc("Disable filter-based reductions")
                .numberOfArgs(0)
                .build();
        options.addOption(filters);

//        // whether or not to disable receiver queue semantics
//        Option receiverQueue = Option.builder("rq")
//                .longOpt("receiver-queue")
//                .desc("Disable sender queue reduction to get receiver queue semantics")
//                .numberOfArgs(0)
//                .build();
//        options.addOption(receiverQueue);
//
//        // whether or not to use symbolic exploration sleep sets
//        Option sleep = Option.builder("sl")
//                .longOpt("sleep-sets")
//                .desc("Enable frontier sleep sets")
//                .numberOfArgs(0)
//                .build();
//        options.addOption(sleep);
//
//        // whether or not to use DPOR
//        Option dpor = Option.builder("dpor")
//                .longOpt("use-dpor")
//                .desc("Enable use of DPOR (not implemented)")
//                .numberOfArgs(0)
//                .build();
//        options.addOption(dpor);

        // whether or not to collect search stats
        Option collectStats = Option.builder("s")
                .longOpt("stats")
                .desc("Level of stats collection/reporting during the search (default: 1)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Collection Level (integer)")
                .build();
        options.addOption(collectStats);

        // set the level of verbosity
        Option verbosity = Option.builder("v")
                .longOpt("verbose")
                .desc("Level of verbosity in the log output (default: 0)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Log Verbosity (integer)")
                .build();
        options.addOption(verbosity);

        Option help = Option.builder("h")
                .longOpt("help")
                .desc("Print the help message")
                .build();
        options.addOption(help);
    }

    private static void optionError(Option opt, String msg) {
        Options opts = new Options();
        opts.addOption(opt);
        writer.println(msg);
        formatter.printHelp(writer, 100, opt.getOpt(), "", opts, 2, 2, "Try --help for details.");
        writer.flush();
        System.exit(10);
    }

    public static PSymConfiguration ParseCommandlineArgs(String[] args) {
        // Parse the commandline arguments
        CommandLineParser parser = new DefaultParser();
        formatter.setOptionComparator(null);
        CommandLine cmd = null;
        try {
            cmd = parser.parse(options, args);
        } catch (ParseException e) {
            System.out.println(e.getMessage());
            formatter.printUsage(writer, 100, "PSym", options);
            writer.flush();
            System.exit(10);
        }

        // Populate the configuration based on the commandline arguments
        PSymConfiguration config = new PSymConfiguration();
        for (Option option : cmd.getOptions()) {
            switch (option.getOpt()) {
                case "mode":
                    switch (option.getValue()) {
                        case "default":
                            config.setToDefault();
                            break;
                        case "bmc":
                            config.setToBmc();
                            break;
                        case "random":
                            config.setToRandom();
                            break;
                        case "fuzz":
                            config.setToFuzz();
                            break;
                        case "dfs":
                            config.setToDfs();
                            break;
                        case "learn":
                            config.setToLearn();
                            break;
                        case "debug":
                            config.setToDebug();
                            break;
                        default:
                            optionError(option, String.format("Unrecognized mode of exploration, got %s", option.getValue()));
                    }
                    break;
                case "tl":
                case "time-limit":
                    try {
                        config.setTimeLimit(Double.parseDouble(option.getValue()));
                    } catch (NumberFormatException ex) {
                        optionError(option, String.format("Expected a double value, got %s", option.getValue()));
                    }
                    break;
                case "ml":
                case "memory-limit":
                    try {
                        config.setMemLimit(Double.parseDouble(option.getValue()));
                    } catch (NumberFormatException ex) {
                        optionError(option, String.format("Expected a double value, got %s", option.getValue()));
                    }
                    break;
                case "seed":
                    try {
                        config.setRandomSeed(Long.parseLong(option.getValue()));
                    } catch (NumberFormatException ex) {
                        optionError(option, String.format("Expected an integer value, got %s", option.getValue()));
                    }
                    break;
                case "m":
                case "method":
                    config.setTestDriver(option.getValue());
                    break;
                case "p":
                case "project":
                    config.setProjectName(option.getValue());
                    break;
                case "o":
                case "outdir":
                    config.setOutputFolder(option.getValue());
                    break;
                case "replay":
                    config.setReadReplayerFromFile(option.getValue());
                    File file = new File(config.getReadReplayerFromFile());
                    try {
                        file.getCanonicalPath();
                    } catch (IOException e) {
                        optionError(option, String.format("File %s does not exist", config.getReadFromFile()));
                    }
                    break;
                case "ms":
                case "max-steps":
                    try {
                        config.setMaxStepBound(Integer.parseInt(option.getValue()) + 1);
                    } catch (NumberFormatException ex) {
                        optionError(option, String.format("Expected an integer value, got %s", option.getValue()));
                    }
                    break;
                case "i":
                case "iterations":
                    try {
                        config.setMaxExecutions(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        optionError(option, String.format("Expected an integer value, got %s", option.getValue()));
                    }
                    break;
                case "sb":
                case "sched-bound":
                    try {
                        config.setSchedChoiceBound(Integer.parseInt(option.getValue()));
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
                case "nsc":
                case "no-state-caching":
                    config.setUseStateCaching(false);
                    break;
                case "nb":
                case "no-backtrack":
                    config.setUseBacktrack(false);
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
                            optionError(option, String.format("Unrecognized choice orchestration mode, got %s", option.getValue()));
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
                            optionError(option, String.format("Unrecognized task orchestration mode, got %s", option.getValue()));
                    }
                    break;
                case "bpe":
                case "backtracks-per-exe":
                    try {
                        config.setMaxBacktrackTasksPerExecution(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        optionError(option, String.format("Expected an integer value, got %s", option.getValue()));
                    }
                    break;
                case "st":
                case "solver":
                    switch (option.getValue()) {
                        case "abc":
                            config.setSolverType(SolverType.ABC);
                            break;
                        case "bdd":
                            config.setSolverType(SolverType.BDD);
                            break;
                        case "cbdd":
                            config.setSolverType(SolverType.CBDD);
                            break;
                        case "cvc5":
                            config.setSolverType(SolverType.CVC5);
                            break;
                        case "yices2":
                            config.setSolverType(SolverType.YICES2);
                            break;
                        case "z3":
                            config.setSolverType(SolverType.Z3);
                            break;
                        case "monosat":
                            config.setSolverType(SolverType.MONOSAT);
                            break;
                        case "boolector":
                            config.setSolverType(SolverType.JAVASMT_BOOLECTOR);
                            break;
                        case "mathsat5":
                            config.setSolverType(SolverType.JAVASMT_MATHSAT5);
                            break;
                        case "princess":
                            config.setSolverType(SolverType.JAVASMT_PRINCESS);
                            break;
                        case "smtinterpol":
                            config.setSolverType(SolverType.JAVASMT_SMTINTERPOL);
                            break;
                        default:
                            optionError(option, String.format("Expected a solver type, got %s", option.getValue()));
                    }
                    break;
                case "et":
                case "expr":
                    switch (option.getValue()) {
                        case "aig":
                            config.setExprLibType(ExprLibType.Aig);
                            break;
                        case "auto":
                            config.setExprLibType(ExprLibType.Auto);
                            break;
                        case "bdd":
                            config.setExprLibType(ExprLibType.Bdd);
                            break;
                        case "fraig":
                            config.setExprLibType(ExprLibType.Fraig);
                            break;
                        case "iaig":
                            config.setExprLibType(ExprLibType.Iaig);
                            break;
                        case "native":
                            config.setExprLibType(ExprLibType.NativeExpr);
                            break;
                        default:
                            optionError(option, String.format("Expected an expression type, got %s", option.getValue()));
                    }
                    break;
                case "r":
                case "read":
                    config.setReadFromFile(option.getValue());
                    File replayFile = new File(config.getReadFromFile());
                    try {
                        replayFile.getCanonicalPath();
                    } catch (IOException e) {
                        optionError(option, String.format("File %s does not exist", config.getReadFromFile()));
                    }
                    break;
                case "w":
                case "write":
                    config.setWriteToFile(true);
                    break;
                case "nf":
                case "no-filters":
                    config.setUseFilters(false);
                    break;
                //                case "rq":
                //                case "receiver-queue":
                //                    config.setUseReceiverQueueSemantics(true);
                //                    break;
                //                case "sl":
                //                case "sleep-sets":
                //                    config.setUseSleepSets(true);
                //                    break;
                //                case "dpor":
                //                case "use-dpor":
                //                    config.setDpor(true);
                //                    break;
                case "s":
                case "stats":
                    try {
                        config.setCollectStats(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        optionError(option, String.format("Expected an integer value, got %s", option.getValue()));
                    }
                    break;
                case "v":
                case "verbose":
                    try {
                        config.setVerbosity(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        optionError(option, String.format("Expected an integer value, got %s", option.getValue()));
                    }
                    break;
                case "h":
                case "help":
                default:
                    formatter.printHelp(100, "-h or --help", "Commandline options for PSym", options, "");
                    System.exit(0);
            }
        }
        return config;
    }
}
