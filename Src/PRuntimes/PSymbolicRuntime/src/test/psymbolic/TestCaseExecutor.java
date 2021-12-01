package psymbolic;

import psymbolic.runtime.logger.TraceLogger;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.List;
import java.util.concurrent.Executors;
import java.util.function.Consumer;
import java.util.stream.Collectors;

import static psymbolic.Utils.splitPath;


public class TestCaseExecutor {

    private static String packageNameFromRelDir(String relDir) {
        assert !relDir.contains("//");
        assert !relDir.startsWith("/");
        assert !relDir.endsWith("/");
        return "psymbolic.testCase." + relDir.replace('/', '.');
    }

    // We prepend the package directly to the file on disk, rather than to the file contents we read into memory, to
    // permit manual testing of the generated files from IntelliJ.
    private static String prependPackageDeclarationAndRead(String packageName, String filePath) {
        try {
            String fileContents = new String(Files.readAllBytes(Paths.get(filePath)), StandardCharsets.UTF_8);
            fileContents = "package " + packageName + ";\n" + fileContents;

            FileWriter writer = new FileWriter(filePath);
            writer.append(fileContents);
            writer.close();

            return fileContents;
        } catch (IOException exception) {
            throw new RuntimeException(exception);
        }
    }
    /**
     * @param testCasePaths paths to test case; only accepts list of p files
     * @return 0 = successful, 1 = compile error, 2 = dynamic error
     */
    static int runTestCase(List<String> testCasePaths, String testCasePathPrefix) {
        // Invoke the P compiler to compile the test Case
        boolean isWindows = System.getProperty("os.name")
                .toLowerCase().startsWith("windows");
        String compilerDirectory = "../../../Bld/Drops/Release/Binaries/netcoreapp3.1/P.dll";

        String prefix = testCasePathPrefix;
        assert testCasePaths.stream().allMatch(p -> p.contains(prefix));
        List<String> testCaseRelPaths = testCasePaths.stream().map(p -> p.substring(p.indexOf(prefix) + prefix.length()))
                                        .collect(Collectors.toList());
        testCasePaths.stream().map(p -> p.substring(p.indexOf(prefix) + prefix.length())).forEach(System.out::println);
        String testCaseRelDir = Paths.get(testCaseRelPaths.get(0)).getFileName().toString();
        String outputDirectory = "psymbolic/testCases/" + testCaseRelDir;

        List<String> pTestCasePaths = testCasePaths.stream().filter(p -> p.contains(".p")).collect(Collectors.toList());
        String testCasePathsString = String.join(" ", pTestCasePaths);
        Process process;
        try {
            String pCompileCommand = String.format("dotnet %s %s -generate:Symbolic -outputDir:%s\n"
                    , compilerDirectory, testCasePathsString, outputDirectory);
            System.out.println(pCompileCommand);
            process = Runtime.getRuntime().exec(pCompileCommand);

            StreamGobbler errorStreamGobbler = new StreamGobbler(process.getErrorStream(), System.out::println);
            Executors.newSingleThreadExecutor().submit(errorStreamGobbler);
            StreamGobbler streamGobbler = new StreamGobbler(process.getInputStream(), System.out::println);
            Executors.newSingleThreadExecutor().submit(streamGobbler);
            int exitCode = process.waitFor();

            if (exitCode != 0) {
                TraceLogger.logMessage("Compilation failure.");
                return 1;
            }
        }
        catch (IOException | InterruptedException e) {
            TraceLogger.logMessage("Compilation failure.");
            e.printStackTrace();
        }

        // Next, try to dynamically load and compile this file
        String[] path_split = splitPath(testCasePaths.get(0));
        String class_name = path_split[path_split.length-1].split("\\.")[0].toLowerCase();
        String pathToJar = outputDirectory + "/target/" + class_name + "-1.0-jar-with-dependencies.jar";

        try {
            String runJarCommand = String.format("java -jar %s\n", pathToJar);
            System.out.println(runJarCommand);
            process = Runtime.getRuntime().exec(runJarCommand);

            StreamGobbler streamGobbler = new StreamGobbler(process.getErrorStream(), System.out::println);
            Executors.newSingleThreadExecutor().submit(streamGobbler);
            StreamGobbler outstreamGobbler = new StreamGobbler(process.getInputStream(), System.out::println);
            Executors.newSingleThreadExecutor().submit(outstreamGobbler);
            int exitCode = process.waitFor();

            if (exitCode != 0) {
                TraceLogger.logMessage("Running the Jar Failed!");
                return 1;
            }
        } catch (Exception | AssertionError e) {
            e.printStackTrace();
            return 2;
        }
        return 0;
    }

    private static class StreamGobbler implements Runnable {
        private InputStream inputStream;
        private Consumer<String> consumer;

        StreamGobbler(InputStream inputStream, Consumer<String> consumer) {
            this.inputStream = inputStream;
            this.consumer = consumer;
        }

        @Override
        public void run() {
            new BufferedReader(new InputStreamReader(inputStream)).lines()
                    .forEach(consumer);
        }
    }

}
