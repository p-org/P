package psymbolic.commandline;

import org.apache.commons.cli.*;
import org.reflections.Reflections;

import psymbolic.valuesummary.solvers.SolverEngine;
import psymbolic.valuesummary.solvers.SolverType;

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
                .argName("Project name (string)")
                .build();
        options.addOption(projectName);

        // solver type
        Option solverType = Option.builder("st")
                .longOpt("solver")
                .desc("Solver type to use: bdd, boolector, cvc4, princess, smtinterpol, yices2, z3")
                .numberOfArgs(1)
                .hasArg()
                .argName("Solver type (string)")
                .build();
        options.addOption(solverType);

        // max depth bound for the search
        Option depthBound = Option.builder("db")
                .longOpt("depth-bound")
                .desc("Max Depth bound for the search")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Depth Bound (integer)")
                .build();
        options.addOption(depthBound);

        // max depth bound for the search
        Option inputChoiceBound = Option.builder("cb")
                .longOpt("choice-bound")
                .desc("Max choice bound at each depth during the search (integer)")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Choice Bound (integer)")
                .build();
        options.addOption(inputChoiceBound);

        // max depth bound for the search
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
                .desc("Enable collection of stats during the search")
                .numberOfArgs(0)
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
                case "st":
                case "solver":
                	switch (option.getValue()) {
                	case "bdd":			config.setSolverType(SolverType.BDD);
                		break;
                	case "boolector":	config.setSolverType(SolverType.JAVASMT_BOOLECTOR);
            			break;
                	case "cvc4":		config.setSolverType(SolverType.JAVASMT_CVC4);
            			break;
                	case "princess":	config.setSolverType(SolverType.JAVASMT_PRINCESS);
            			break;
                	case "smtinterpol":	config.setSolverType(SolverType.JAVASMT_SMTINTERPOL);
            			break;
                	case "yices2":		config.setSolverType(SolverType.JAVASMT_YICES2);
            			break;
                	case "z3":			config.setSolverType(SolverType.JAVASMT_Z3);
            			break;
        			default:	
                        formatter.printHelp("st", String.format("Expected a solver type, got %s", option.getValue()), options, "Try \"--help\" option for details.");
                        formatter.printUsage(writer, 80, "st", options);
                	}
                	SolverEngine.resetEngine(config.getSolverType());
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
                    config.setCollectStats(true);
                    break;
                case "nf":
                case "no-filters":
                    config.setUseFilters(false);
                    break;
                case "dpor":
                case "use-dpor":
                    config.setDpor(true);
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
