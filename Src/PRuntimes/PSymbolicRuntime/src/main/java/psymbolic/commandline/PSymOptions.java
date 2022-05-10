package psymbolic.commandline;

import org.apache.commons.cli.*;
import org.reflections.Reflections;

import psymbolic.valuesummary.solvers.SolverType;
import psymbolic.valuesummary.solvers.sat.expr.ExprLibType;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.Set;

/**
 * Represents the commandline options for the tool
 */
public class PSymOptions {

    private static final Options options;

    static {
        options = new Options();

        // input file to be tested
        Option inputFile = Option.builder("m")
                .longOpt("main")
                .desc("Name of the main machine from where the symbolic engine should start exploration")
                .numberOfArgs(1)
                .hasArg()
                .argName("Name of Main Machine (string)")
                .build();
        options.addOption(inputFile);

        // project name
        Option projectName = Option.builder("p")
                .longOpt("project")
                .desc("Name of the project")
                .numberOfArgs(1)
                .hasArg()
                .argName("Project Name (string)")
                .build();
        options.addOption(projectName);

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
        Option depthBound = Option.builder("db")
                .longOpt("depth-bound")
                .desc("Max Depth bound for the search")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Depth Bound (integer)")
                .build();
        options.addOption(depthBound);

        // max iteration bound for the search
        Option maxIterationBound = Option.builder("ib")
                .longOpt("iteration-bound")
                .desc("Max iteration bound for the search")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Iteration Bound (integer)")
                .build();
        options.addOption(maxIterationBound);

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

        // whether or not to disable incremental backtracking
        Option backtrack = Option.builder("nb")
                .longOpt("no-backtrack")
                .desc("Disable incremental backtracking")
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
        Option randomSeed = Option.builder("rs")
                .longOpt("random-seed")
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
                case "m":
                case "main":
                    config.setMainMachine(option.getValue());
                    Reflections reflections = new Reflections("psymbolic");

                    Set<Class<? extends Program>> subTypes = reflections.getSubTypesOf(Program.class);
                    for(Class<? extends Program> clazz :subTypes)
                    {
                        System.out.println("Found Program implementations:" +  clazz.toString());
                    }
                    if(subTypes.stream().count() == 0)
                    {
                        formatter.printHelp("m", String.format("Main machine %s not found", option.getValue()), options, "Try \"--help\" option for details.");
                        formatter.printUsage(writer, 80, "m", options);
                    }
                    break;
                case "p":
                case "project":
                    config.setProjectName(option.getValue());
                    break;
                case "r":
                case "read":
                    config.setReadFromFile(option.getValue());
                    File file = new File(config.getReadFromFile());
                    try {
                        System.out.println(file.getCanonicalPath() + " exists? "+ file.exists());
                    } catch (IOException e) {
                        formatter.printHelp("r", String.format("File %s does not exist", option.getValue()), options, "Try \"--help\" option for details.");
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
                case "db":
                case "depth-bound":
                    try {
                        config.setDepthBound(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("db", String.format("Expected an integer value, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                        formatter.printUsage(writer, 80, "db", options);
                    }
                    break;
                case "ib":
                case "iteration-bound":
                    try {
                        config.setIterationBound(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("ib", String.format("Expected an integer value, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                        formatter.printUsage(writer, 80, "ib", options);
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
                case "rs":
                case "random-seed":
                    try {
                        config.setRandomSeed(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("rs", String.format("Expected an integer value, got %s", option.getValue()), options, "Try \"--help\" option for details.");
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
