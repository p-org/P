package psymbolic.commandline;

import org.apache.commons.cli.*;

import java.io.File;
import java.io.PrintWriter;

/**
 * Represents the commandline options for the tool
 */
public class PSymOptions {

    private static final Options options;

    static {
        options = new Options();

        // input file to be tested
        Option inputFile = Option.builder("t")
                .longOpt("test")
                .desc("Input jar file to be tested using the symbolic execution engine")
                .numberOfArgs(1)
                .hasArg()
                .argName("Test Input Jar file")
                .required().build();
        options.addOption(inputFile);

        // max depth bound for the search
        Option depthBound = Option.builder("db")
                .longOpt("depth-bound")
                .desc("Max Depth bound for the search")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Depth Bound")
                .build();
        options.addOption(depthBound);

        // max depth bound for the search
        Option inputChoiceBound = Option.builder("cb")
                .longOpt("choice-bound")
                .desc("Max choice bound at each depth during the search")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Choice Bound")
                .build();
        options.addOption(inputChoiceBound);

        // max depth bound for the search
        Option maxSchedBound = Option.builder("sb")
                .longOpt("sched-choice-bound")
                .desc("Max scheduling choice bound at each depth during the search")
                .numberOfArgs(1)
                .hasArg()
                .argName("Max Schedule Choice Bound")
                .build();

        Option help = Option.builder("h")
                .longOpt("help")
                .desc("Print the help message")
                .argName("Help")
                .build();
        options.addOption(maxSchedBound);
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
            System.out.println("Try \"--help\" option for details.");
            System.exit(1);
        }

        // Populate the configuration based on the commandline arguments
        PSymConfiguration config = new PSymConfiguration();
        for (Option option : cmd.getOptions()) {
            switch (option.getOpt()) {
                case "t":
                case "test":
                    File file = new File(option.getValue());
                    if (file.exists()) {
                        config.setInputFile(option.getValue());
                    } else {
                        formatter.printHelp("t", String.format("File %s not found", option.getValue()), options, "\"Try \\\"--help\\\" option for details.\"");
                        formatter.printUsage(writer, 80, "t", options);
                    }
                    break;
                case "cb":
                case "sched-choice-bound":
                    try {
                        config.setInputChoiceBound(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("sb", String.format("Expected an integer value, got %s", option.getValue()), options, "\"Try \\\"--help\\\" option for details.\"");
                        formatter.printUsage(writer, 80, "sb", options);
                    }
                    break;
                case "db":
                case "depth-bound":
                    try {
                        config.setDepthBound(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("db", String.format("Expected an integer value, got %s", option.getValue()), options, "\"Try \\\"--help\\\" option for details.\"");
                        formatter.printUsage(writer, 80, "db", options);
                    }
                    break;
                case "sb":
                case "choice-bound":
                    try {
                        config.setInputChoiceBound(Integer.parseInt(option.getValue()));
                    } catch (NumberFormatException ex) {
                        formatter.printHelp("cb", String.format("Expected an integer value, got %s", option.getValue()), options, "\"Try \\\"--help\\\" option for details.\"");
                        formatter.printUsage(writer, 80, "cb", options);
                    }
                    break;
                case "h":
                case "help":
                default:
                    formatter.printUsage(writer, 80, "PSymbolic", options);
                    writer.flush();
                    System.exit(0);
            }
        }

        return config;
    }
}
