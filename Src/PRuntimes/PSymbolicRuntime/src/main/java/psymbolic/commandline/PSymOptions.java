package psymbolic.commandline;

import org.apache.commons.cli.*;

import psymbolic.utils.OrchestrationMode;
import psymbolic.valuesummary.solvers.SolverType;
import psymbolic.valuesummary.solvers.sat.expr.ExprLibType;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;

/**
 * Represents the commandline options for the tool
 */
public class PSymOptions {

    private static final Options options;

    static {
        options = new Options();

        // mode of orchestration
        Option orch = Option.builder("orch")
                .longOpt("orchestration")
                .desc("Orchestration options: random, coverage-astar, coverage-estimate, dfs, none")
                .numberOfArgs(1)
                .hasArg()
                .argName("Orchestration Mode (string)")
                .build();
        options.addOption(orch);

        // test driver name
        Option debugMode = Option.builder("d")
                .longOpt("debug")
                .desc("Debug mode (internal)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Debug Mode (string)")
                .build();
        options.addOption(debugMode);

        // test driver name
        Option testName = Option.builder("m")
                .longOpt("method")
                .desc("Name of the test method from where the symbolic engine should start exploration")
                .numberOfArgs(1)
                .hasArg()
                .argName("Name of Test Method (string)")
                .build();
        options.addOption(testName);

        // project name
        Option projectName = Option.builder("p")
                .longOpt("project")
                .desc("Name of the project")
                .numberOfArgs(1)
                .hasArg()
                .argName("Project Name (string)")
                .build();
        options.addOption(projectName);

        // output folder
        Option outputDir = Option.builder("o")
                .longOpt("output")
                .desc("Name of the output folder")
                .numberOfArgs(1)
                .hasArg()
                .argName("Output Folder (string)")
                .build();
        options.addOption(outputDir);

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

        // time limit
        Option timeLimit = Option.builder("tl")
                .longOpt("time-limit")
                .desc("Time limit in seconds. Use 0 for no limit.")
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

        // solver type
        Option solverType = Option.builder("st")
                .longOpt("solver")
                .desc("Solver type to use: bdd, yices2, z3, cvc5")
                .numberOfArgs(1)
                .hasArg()
                .argName("Solver Type (string)")
                .build();
        options.addOption(solverType);

        // expression type
        Option exprLibType = Option.builder("et")
                .longOpt("expr")
                .desc("Expression type to use: bdd, fraig, aig, native")
                .numberOfArgs(1)
                .hasArg()
                .argName("Expression Type (string)")
                .build();
        options.addOption(exprLibType);

        // max depth bound for the search
        Option depthBound = Option.builder("ms")
                .longOpt("max-steps")
                .desc("Max scheduling steps for the search")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Steps (integer)")
                .build();
        options.addOption(depthBound);

        // max number of executions for the search
        Option maxExecutions = Option.builder("me")
                .longOpt("max-executions")
                .desc("Max number of executions to run")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Executions (integer)")
                .build();
        options.addOption(maxExecutions);

        // max choice bound for the search
        Option inputChoiceBound = Option.builder("cb")
                .longOpt("choice-bound")
                .desc("Max choice bound at each depth during the search (integer)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Choice Bound (integer)")
                .build();
        options.addOption(inputChoiceBound);

        // max scheduling choice bound for the search
        Option maxSchedBound = Option.builder("sb")
                .longOpt("sched-choice-bound")
                .desc("Max scheduling choice bound at each depth during the search")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Schedule Choice Bound (integer)")
                .build();
        options.addOption(maxSchedBound);

        // whether or not to enable state caching
        Option stateCaching = Option.builder("sc")
                .longOpt("state-caching")
                .desc("Enable state caching via enumeration of exact states")
                .numberOfArgs(0)
                .build();
        options.addOption(stateCaching);

        // whether or not to disable receiver queue semantics
        Option receiverQueue = Option.builder("rq")
                .longOpt("receiver-queue")
                .desc("Disable sender queue reduction to get receiver queue semantics")
                .numberOfArgs(0)
                .build();
        options.addOption(receiverQueue);

        // whether or not to disable receiver queue semantics
        Option filters = Option.builder("nf")
                .longOpt("no-filters")
                .desc("Disable filter-based reductions")
                .numberOfArgs(0)
                .build();
        options.addOption(filters);

        // whether or not to collect search stats
        Option collectStats = Option.builder("s")
                .longOpt("stats")
                .desc("Level of stats collection during the search")
                .numberOfArgs(1)
                .hasArg()
                .argName("Collection Level")
                .build();
        options.addOption(collectStats);

        // whether or not to use symbolic exploration sleep sets
        Option sleep = Option.builder("sl")
                .longOpt("sleep-sets")
                .desc("Enable frontier sleep sets")
                .numberOfArgs(0)
                .build();
        options.addOption(sleep);

        // whether or not to use DPOR
        Option dpor = Option.builder("dpor")
                .longOpt("use-dpor")
                .desc("Enable use of DPOR (not implemented)")
                .numberOfArgs(0)
                .build();
        options.addOption(dpor);

        // whether or not to disable stateful backtracking
        Option backtrack = Option.builder("nb")
                .longOpt("no-backtrack")
                .desc("Disable stateful backtracking")
                .numberOfArgs(0)
                .build();
        options.addOption(backtrack);

        // whether or not to disable randomization
        Option random = Option.builder("nr")
                .longOpt("no-random")
                .desc("Disable randomization")
                .numberOfArgs(0)
                .build();
        options.addOption(random);

        // random seed for the search
        Option randomSeed = Option.builder("seed")
                .longOpt("seed")
                .desc("Random seed for the search")
                .numberOfArgs(1)
                .hasArg()
                .argName("Random Seed (integer)")
                .build();
        options.addOption(randomSeed);

        // set the level of verbosity
        Option verbosity = Option.builder("v")
                .longOpt("verbose")
                .desc("Level of verbosity for the logging")
                .numberOfArgs(1)
                .hasArg()
                .argName("Log Verbosity")
                .build();
        options.addOption(verbosity);

        Option help = Option.builder("h")
                .longOpt("help")
                .desc("Print the help message")
                .build();
        options.addOption(help);
    }

    public static PSymConfiguration ParseCommandlineArgs(String[] args) {
        // Parse the commandline arguments
        CommandLineParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();
        final PrintWriter writer = new PrintWriter(System.out);
        CommandLine cmd = null;
        try {
            cmd = parser.parse(options, args);
        } catch (ParseException e) {
            System.out.println(e.getMessage());
            formatter.printUsage(writer, 80, "PSymbolic", options);
            writer.flush();
            System.exit(10);
        }

        // Populate the configuration based on the commandline arguments
        PSymConfiguration config = new PSymConfiguration();
        for (Option option : cmd.getOptions()) {
            switch (option.getOpt()) {
                case "orch":
                case "orchestration":
                    switch (option.getValue()) {
                        case "none":
                            config.setOrchestration(OrchestrationMode.None);
                            break;
                        case "random":
                            config.setOrchestration(OrchestrationMode.Random);
                            break;
                        case "coverage-astar":
                            config.setOrchestration(OrchestrationMode.CoverageAStar);
                            break;
                        case "coverage-estimate":
                            config.setOrchestration(OrchestrationMode.CoverageEstimate);
                            break;
                        case "coverage-parent":
                            config.setOrchestration(OrchestrationMode.CoverageParent);
                            break;
                        case "dfs":
                            config.setOrchestration(OrchestrationMode.DepthFirst);
                            break;
                        default:
                            formatter.printHelp("orch", String.format("Unrecognized orchestration mode, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                            formatter.printUsage(writer, 80, "orch", options);
                    }
                    break;
                case "d":
                case "debug":
                    config.setDebugMode(option.getValue());
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
                case "output":
                    config.setOutputFolder(option.getValue());
                    break;
                case "r":
                case "read":
                    config.setReadFromFile(option.getValue());
                    File file = new File(config.getReadFromFile());
                    try {
                        file.getCanonicalPath();
                    } catch (IOException e) {
                        formatter.printHelp("r", String.format("File %s does not exist", config.getReadFromFile()), options, "Try \"--help\" option for details.");
                        formatter.printUsage(writer, 80, "r", options);
                    }
                    break;
                case "w":
                case "write":
                    config.setWriteToFile(true);
                    break;
                case "tl":
                case "time-limit":
                    try {
                        config.setTimeLimit(Double.parseDouble(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("tl", String.format("Expected a double value, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                        formatter.printUsage(writer, 80, "tl", options);
                    }
                    break;
                case "ml":
                case "memory-limit":
                    try {
                        config.setMemLimit(Double.parseDouble(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("ml", String.format("Expected a double value, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                        formatter.printUsage(writer, 80, "ml", options);
                    }
                    break;
                case "st":
                case "solver":
                	switch (option.getValue()) {
                    case "abc":			config.setSolverType(SolverType.ABC);
                        break;
                	case "bdd":			config.setSolverType(SolverType.BDD);
                		break;
                	case "cbdd":		config.setSolverType(SolverType.CBDD);
            			break;
                    case "cvc5":		config.setSolverType(SolverType.CVC5);
                        break;
                    case "yices2":		config.setSolverType(SolverType.YICES2);
                        break;
                    case "z3":		    config.setSolverType(SolverType.Z3);
                        break;
                    case "monosat":		config.setSolverType(SolverType.MONOSAT);
                        break;
                	case "boolector":	config.setSolverType(SolverType.JAVASMT_BOOLECTOR);
            			break;
                	case "mathsat5":	config.setSolverType(SolverType.JAVASMT_MATHSAT5);
            			break;
                	case "princess":	config.setSolverType(SolverType.JAVASMT_PRINCESS);
            			break;
                	case "smtinterpol":	config.setSolverType(SolverType.JAVASMT_SMTINTERPOL);
            			break;
        			default:
                        formatter.printHelp("st", String.format("Expected a solver type, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                        formatter.printUsage(writer, 80, "st", options);
                	}
                    break;
                case "et":
                case "expr":
                    switch (option.getValue()) {
                        case "aig":		        config.setExprLibType(ExprLibType.Aig);
                            break;
                        case "auto":    		config.setExprLibType(ExprLibType.Auto);
                            break;
                        case "bdd":    		    config.setExprLibType(ExprLibType.Bdd);
                            break;
                        case "fraig":		    config.setExprLibType(ExprLibType.Fraig);
                            break;
                        case "iaig":		    config.setExprLibType(ExprLibType.Iaig);
                            break;
                        case "native":			config.setExprLibType(ExprLibType.NativeExpr);
                            break;
                        default:
                            formatter.printHelp("et", String.format("Expected a expression type, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                            formatter.printUsage(writer, 80, "et", options);
                    }
                    break;
                case "sb":
                case "sched-choice-bound":
                    try {
                        config.setSchedChoiceBound(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("sb", String.format("Expected an integer value, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                        formatter.printUsage(writer, 80, "sb", options);
                    }
                    break;
                case "ms":
                case "max-steps":
                    try {
                        config.setDepthBound(Integer.parseInt(option.getValue())+1);
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("ms", String.format("Expected an integer value, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                        formatter.printUsage(writer, 80, "ms", options);
                    }
                    break;
                case "me":
                case "max-executions":
                    try {
                        config.setMaxExecutions(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("me", String.format("Expected an integer value, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                        formatter.printUsage(writer, 80, "me", options);
                    }
                    break;
                case "cb":
                case "choice-bound":
                    try {
                        config.setInputChoiceBound(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("cb", String.format("Expected an integer value, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                    }
                    break;
                case "v":
                case "verbose":
                    try {
                        config.setVerbosity(Integer.parseInt(option.getValue()));
                    }
                    catch (NumberFormatException ex) {
                        formatter.printHelp("v", String.format("Expected an integer value (0, 1 or 2), got %s", option.getValue()), options, "Try \"--help\" option for details.");
                    }
                    break;
                case "sc":
                case "state-caching":
                    config.setUseStateCaching(true);
                    break;
                case "rq":
                case "receiver-queue":
                    config.setUseReceiverQueueSemantics(true);
                    break;
                case "sl":
                case "sleep-sets":
                    config.setUseSleepSets(true);
                    break;
                case "s":
                case "stats":
                    try {
                        config.setCollectStats(Integer.parseInt(option.getValue()));
                    }
                    catch (NumberFormatException ex) {
                        formatter.printHelp("s", String.format("Expected an integer value (0, 1 or 2), got %s", option.getValue()), options, "Try \"--help\" option for details.");
                    }
                    break;
                case "nf":
                case "no-filters":
                    config.setUseFilters(false);
                    break;
                case "dpor":
                case "use-dpor":
                    config.setDpor(true);
                    break;
                case "nb":
                case "no-backtrack":
                    config.setUseBacktrack(false);
                    break;
                case "nr":
                case "no-random":
                    config.setUseRandom(false);
                    break;
                case "seed":
                    try {
                        config.setRandomSeed(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("seed", String.format("Expected an integer value, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                    }
                    break;
                case "h":
                case "help":
                default:
                    formatter.printHelp(100, "-h or --help", "Commandline options for psymbolic", options, "");
                    System.exit(0);
            }
        }
        return config;
    }
}
