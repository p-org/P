package psymbolic.commandline;

import org.apache.commons.cli.*;
import org.reflections.Reflections;

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
            System.exit(1);
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
                case "sb":
                case "sched-choice-bound":
                    try {
                        config.setInputChoiceBound(Integer.parseInt(option.getValue()));
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
