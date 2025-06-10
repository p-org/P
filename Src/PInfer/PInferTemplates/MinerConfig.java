import org.apache.commons.cli.*;

public class MinerConfig {
    public final String atomicPredicatesPath;
    public final String termsPath;
    public final int numGuardConjunctions;
    public final int numFilterConjunctions;
    public final boolean checkTrivialCombinations;
    public final int numForallQuantifiers;
    public final int numExistsQuantifiers;
    public final int numTermsToChoose;
    public final int numExtTerms;
    public final List<String> traces;
    public final Set<Integer> mustIncludeGuards;
    public final Set<Integer> mustIncludeFilters;
    public final String templateCategory;
    public final boolean verbose;
    public final int pruningLevel;
    public final String outputDir;

    public MinerConfig(CommandLine cmdOptions) {
        numForallQuantifiers = Integer.parseInt(cmdOptions.getOptionValue("num-forall", "%QUANTIFIED_EVENTS%"));
        numExistsQuantifiers = %QUANTIFIED_EVENTS% - numForallQuantifiers;
        if (numForallQuantifiers < 0 || numExistsQuantifiers < 0) {
            throw new RuntimeException("Invalide Number of forall/exists quantifiers: ∀" + numForallQuantifiers + "∃" + numExistsQuantifiers);
        }
        numTermsToChoose = Integer.parseInt(cmdOptions.getOptionValue("num-terms", "%NUM_TERMS_TO_CHOOSE%"));
        numExtTerms = Integer.parseInt(cmdOptions.getOptionValue("num-ext-terms", "0"));
        if (numTermsToChoose == 0) {
            throw new RuntimeException("Number of terms must be > 0");
        }
        if (numExtTerms > numTermsToChoose) {
            throw new RuntimeException("Number of terms involves existential quantification exceeds total number of terms in a combination");
        }
        outputDir = cmdOptions.getOptionValue("output-dir", "SpecMining");
        atomicPredicatesPath = cmdOptions.getOptionValue("predicates", "%PROJECT_NAME%.predicates.json");
        termsPath = cmdOptions.getOptionValue("terms", "%PROJECT_NAME%.terms.json");
        checkTrivialCombinations = cmdOptions.hasOption("skip-trivial");
        verbose = cmdOptions.hasOption("verbose");
        pruningLevel = cmdOptions.hasOption("output-pruning-level") ? Integer.parseInt(cmdOptions.getOptionValue("output-pruning-level")) : 1;
        if (pruningLevel < 0 || pruningLevel > 3) {
            throw new RuntimeException("Invalid pruning level: " + pruningLevel + ". -O takes a value from [0-2]");
        }
        traces = List.of(cmdOptions.getOptionValues("logs"));
        StringBuilder templateMetadata = new StringBuilder();
        StringBuilder templateNamePrefixBuilder = new StringBuilder();
        // Template names
        if (numForallQuantifiers > 0) {
            templateMetadata.append("Forall");
        }
        if (numExistsQuantifiers > 0) {
            templateMetadata.append("Exists");
        }
        templateCategory = templateMetadata.toString();
        // must-include sets
        if (cmdOptions.hasOption("include-guards")) {
            mustIncludeGuards = Arrays.stream(cmdOptions.getOptionValues("include-guards"))
                    .mapToInt(Integer::parseInt)
                    .boxed()
                    .collect(Collectors.toSet());
        } else {
            mustIncludeGuards = Set.of();
        }
        numGuardConjunctions = Math.max(0,
                Integer.parseInt(cmdOptions.getOptionValue("guard-depth", "0")) - mustIncludeGuards.size());
        if (cmdOptions.hasOption("include-filters")) {
            mustIncludeFilters = Arrays.stream(cmdOptions.getOptionValues("include-filters"))
                    .mapToInt(Integer::parseInt)
                    .boxed()
                    .collect(Collectors.toSet());
        } else {
            mustIncludeFilters = Set.of();
        }
        numFilterConjunctions = Math.max(0,
                Integer.parseInt(cmdOptions.getOptionValue("filter-depth", "0")) - mustIncludeFilters.size());
    }

    public String getOutputFilename() {
        StringBuilder sb = new StringBuilder("invariants");
        sb.append("_forall").append(this.numForallQuantifiers);
        sb.append("_exists").append(this.numExistsQuantifiers);
        sb.append("_g").append(this.numGuardConjunctions);
        sb.append("_f").append(this.numFilterConjunctions);
        sb.append(".txt");
        return sb.toString();
    }

    public static MinerConfig fromCommandLineArgs(String[] args) {
        Options opts = argParserSetup();
        CommandLine cmd = parseArgs(args, opts);
        return new MinerConfig(cmd);
    }

    private static Options argParserSetup() {
        Options options = new Options();
        Option predicatePathOpt = new Option("p", "predicates", true,
                "Path to predicates.json (Default: %PROJECT_NAME%.predicates.json)");
        predicatePathOpt.setRequired(false);
        options.addOption(predicatePathOpt);

        Option termsPathOpt = new Option("t", "terms", true,
                "Path to terms.json (Default: %PROJECT_NAME%.terms.json)");
        termsPathOpt.setRequired(false);
        options.addOption(termsPathOpt);

        Option predDepthOpt = new Option("gd", "guard-depth", true,
                "Number of conjunctions in guards");
        predDepthOpt.setRequired(true);
        options.addOption(predDepthOpt);

        Option filterDepthOpt = new Option("fd", "filter-depth", true,
                "Number of conjunctions in filter (only required when template contains existential quantifications)");
        filterDepthOpt.setRequired(false);
        options.addOption(filterDepthOpt);

        Option numTermsToChoose = new Option("nt", "num-terms", true,
                "Number of terms to choose for checking properties (Default: %NUM_TERMS_TO_CHOOSE%)");
        numTermsToChoose.setRequired(false);
        options.addOption(numTermsToChoose);

        Option trivialityCheck = new Option("st", "skip-trivial", false,
                "Filter out trivial combinations of predicates and terms");
        trivialityCheck.setRequired(false);
        options.addOption(trivialityCheck);

        Option numForalls = new Option("nforall", "num-forall", true, "Number of forall quantifiers (Default: %NUM_QUANTIFIERS%)");
        numForalls.setRequired(false);
        options.addOption(numForalls);

        Option traceFilesOpt = new Option("l", "logs", true,
                "A list of P logs in JSON format");
        traceFilesOpt.setRequired(true);
        traceFilesOpt.setArgs(Option.UNLIMITED_VALUES);
        options.addOption(traceFilesOpt);

        Option outputDirOpt = new Option("od", "output-dir", true,
                "Output directory for the mined invariants under `PInferOutputs` (Default: SpecMining)");
        outputDirOpt.setRequired(false);
        options.addOption(outputDirOpt);

        Option mustIncludeOpt = new Option("g", "include-guards", true,
                "A list of predicates ids that must be included in the guards");
        mustIncludeOpt.setRequired(false);
        mustIncludeOpt.setArgs(Option.UNLIMITED_VALUES);
        options.addOption(mustIncludeOpt);

        Option mustIncludeFilterOpt = new Option("f", "include-filters", true,
                "A list of predicate ids that must be included in the existential filter");
        mustIncludeFilterOpt.setRequired(false);
        mustIncludeFilterOpt.setArgs(Option.UNLIMITED_VALUES);
        options.addOption(mustIncludeFilterOpt);

        Option verbose = new Option("v", "verbose", false, "Print all debugging outputs (e.g. Daikon StdErr) from Daikon");
        verbose.setRequired(false);
        options.addOption(verbose);

        Option pruningLv = new Option("O", "output-pruning-level", true,
                                "Level of pruning outputs (0-3, default: 1). 0: no pruning. 1: standard pruning (exclude nullptr comparison, type name comparison)" +
                                "2: In addition to 1, exclude properties whose set of bouonded events is equal to some selected guards/filters" +
                                "3: In addition to 2, exclude all properties of [>, >=, < and <] comparisons of a term with a constant");
        pruningLv.setRequired(false);
        options.addOption(pruningLv);

        return options;
    }

    private static CommandLine parseArgs(String[] args, Options options) {
        DefaultParser parser = new DefaultParser();
        HelpFormatter formatter = new HelpFormatter();

        try {
            return parser.parse(options, args);
        } catch (ParseException e) {
            System.err.println(e.getMessage());
            formatter.printHelp("%PROJECT_NAME%.pinfer.Main", options);
            System.exit(1);
        }
        return null;
    }
}
