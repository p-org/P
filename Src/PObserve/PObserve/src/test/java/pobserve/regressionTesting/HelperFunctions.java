package pobserve.regressionTesting;

import pobserve.commandline.PObserveCommandLineParameters;
import pobserve.config.PObserveConfig;
import pobserve.executor.PObserveExecutor;
import pobserve.logger.PObserveLogger;
import pobserve.metrics.EventMetrics;
import pobserve.report.TrackErrors;
import pobserve.runtime.Monitor;
import pobserve.runtime.events.PEvent;
import pobserve.testing.BaseHelperFunctions;

import com.beust.jcommander.JCommander;
import com.beust.jcommander.ParameterException;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.AbstractMap;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.function.Consumer;
import java.util.stream.Stream;
import org.junit.jupiter.params.provider.Arguments;

import static pobserve.config.PObserveConfig.getPObserveConfig;
import static pobserve.metrics.PObserveMetrics.getPObserveMetrics;

public class HelperFunctions extends BaseHelperFunctions {
    // These base args are set up with empty values.
    // Params consumer, parser, specName, & file will need to be updated for each
    // test, using createArgs()
    private static String[] baseArgs = {
            "--parser", "parserName",
            "--parserConfiguration", "configuration to ignore",
            "--logs", "logName",
            "--spec", "specName",
            "--jars", PObserveArgsConstants.regressionTestingJarPath,
            "--replaySize", PObserveArgsConstants.replay,
            "--outputDir", PObserveArgsConstants.errorDirPath
    };

    protected static String getEndString(String line, String prefix) {
        int startIndex = line.indexOf(prefix) + prefix.length();
        String message = line.substring(startIndex);
        return message;
    }

    /**
     * getErrorDetailsInDirectory(String dirPathStr)
     * return type: AbstractMap.SimpleEntry<List<Long>, List<String>>
     * - This function returns a SimpleEntry containing two lists, one for error
     * timestamps and one for error messages. It pairs each error message with its
     * associated timestamp.
     */
    private static AbstractMap.SimpleEntry<List<Long>, List<String>> getErrorDetailsInDirectory(String dirPathStr)
            throws IOException {
        List<Long> timeStampsList = new ArrayList<>();
        List<String> errorMessagesList = new ArrayList<>();
        Path startingDir = Paths.get(dirPathStr);

        if (!Files.exists(startingDir)) {
            return new AbstractMap.SimpleEntry<List<Long>, List<String>>(timeStampsList, errorMessagesList);
        }
        String timestampPrefix = "errorTimeStamp=";
        String messagePrefix = "errorMessage=";

        Stream<Path> paths = Files.walk(startingDir);
        paths.filter(Files::isRegularFile).forEach(path -> {
            Stream<String> lines = null;
            try {
                lines = Files.lines(path);
            } catch (IOException e) {
                throw new RuntimeException(e);
            }
            lines.forEachOrdered(line -> {
                if (line.contains(messagePrefix)) {
                    errorMessagesList.add(getEndString(line, messagePrefix));
                } else if (line.contains(timestampPrefix)) {
                    timeStampsList.add(Long.valueOf(getEndString(line, timestampPrefix)));
                }
            });
            lines.close();
        });
        paths.close();
        return new AbstractMap.SimpleEntry<List<Long>, List<String>>(timeStampsList, errorMessagesList);
    }

    /**
     * filterArgs(Stream<Arguments> allTests)
     * return type: Stream<Arguments>
     * This function takes a stream of the tests' arguments, checks for any system
     * properties, and filters based on the desired filters.
     */
    private static Stream<Arguments> filterArgs(Stream<Arguments> allTests) {
        // Retrieve system properties for each parameter
        String selectedTestCases = System.getProperty("tc");
        String selectedKeys = System.getProperty("k");
        String selectedLogLines = System.getProperty("l");

        // Parse and store filters
        Set<String> tcSet = new HashSet<>();
        Set<String> keySet = new HashSet<>();
        Set<String> logLinesSet = new HashSet<>();

        if (selectedTestCases != null && !selectedTestCases.isEmpty()) {
            tcSet.addAll(Arrays.asList(selectedTestCases.split(",")));
        }
        if (selectedKeys != null && !selectedKeys.isEmpty()) {
            keySet.addAll(Arrays.asList(selectedKeys.split(",")));
        }
        if (selectedLogLines != null && !selectedLogLines.isEmpty()) {
            logLinesSet.addAll(Arrays.asList(selectedLogLines.split(",")));
        }

        // Filter the test cases
        return allTests.filter(args -> {
            String testCase = (String) args.get()[0];
            String key = (String) args.get()[1];
            String logLines = (String) args.get()[2];

            return (tcSet.isEmpty() || tcSet.contains(testCase)) &&
                    (keySet.isEmpty() || keySet.contains(key)) &&
                    (logLinesSet.isEmpty() || logLinesSet.contains(logLines));
        });
    }

    /**
     * generateTestData(String resourcePath)
     * return type: Stream<Map<String, String>>
     * This function generates all of the arguments we need for each test, and
     * returns them in a stream.
     */
    public static Stream<Arguments> generateTestData(String resourcePath) throws IOException {
        Map<String, Map<String, String>> params;
        params = extractParamsFromTempDir(createTempFilesFromResource("params"));

        Map<String, Integer> files = extractSubDirs(resourcePath);

        List<Arguments> testArguments = new ArrayList<>();

        String filePrefix = "_logs/test_case_";

        for (String file : files.keySet()) {
            int prefixIndex = file.indexOf(filePrefix) + filePrefix.length();

            String relevantPart = file.substring(prefixIndex);
            String[] paramFields = relevantPart.split("_");
            String paramKey = "test_case_" + paramFields[0] + "_params.txt";

            String logFilePath = createTempFilesFromResource(file);

            Map<String, String> updates = new HashMap<>(params.get(paramKey));
            updates.put("--logs", logFilePath);

            testArguments
                    .add(Arguments.of(paramFields[0], paramFields[3], paramFields[2], paramFields[5].split("\\.")[0],
                            updates));
        }
        return filterArgs(testArguments.stream());
    }

    /**
     * runPObserveJob(Map<String, String> updates)
     * return type: AbstractMap.SimpleEntry<List<Long>, List<String>>
     * - This function encapsulates the process of setting up and running the
     * PObserve job. After running the job, it retrieves error details from the
     * specified log directory and returns a SimpleEntry containing two lists: one
     * for error timestamps and one for error messages collected during the job
     * execution.
     */
    public static AbstractMap.SimpleEntry<List<Long>, List<String>> runPObserveJob(Map<String, String> updates)
            throws Exception {
        if (updates.containsKey("--specName")) {
            String specName = updates.remove("--specName");
            updates.put("--spec", specName);
        }

        String[] args = createArgs(updates, baseArgs);

        getPObserveConfig().setParserSupplier(null);
        getPObserveConfig().setSpecificationSupplier(null);
        getPObserveConfig().setSupplierJars(new ArrayList<>());

        JCommander jc = JCommander.newBuilder().addObject(new PObserveCommandLineParameters()).build();
        try {
            jc.parse(args);
            PObserveConfig.validateAndLoadPObserveConfig();
            Consumer<PEvent<?>> consumer = getPObserveConfig().getSpecificationSupplier().get();
            List<Class<? extends pobserve.runtime.events.PEvent<?>>> eventTypes = ((Monitor<?>) consumer).getEventTypes();
            getPObserveMetrics().getEventMetrics().clear();
            eventTypes.forEach(eventType -> getPObserveMetrics().getEventMetrics().put(eventType.getSimpleName(),
                    new EventMetrics(null)));
        } catch (ParameterException e) {
            PObserveLogger.error("Failed parsing ::");
            PObserveLogger.error(e.getMessage());
            jc.usage();
        }

        try {
            var job = new PObserveExecutor();
            job.run();
            TrackErrors.emitErrorsReport();
        } catch (Exception ex) {
            PObserveLogger.error("Failed with an unhandled exception:: " + ex.getMessage());
            PObserveLogger.error("Please report this issue to the P team");
        }

        // Returns timestamps and messages (kv pair)
        return getErrorDetailsInDirectory(PObserveArgsConstants.errorDirPath);
    }
}
