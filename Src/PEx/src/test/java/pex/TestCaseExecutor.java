package pex;

import org.apache.commons.io.FileUtils;

import java.io.*;
import java.nio.file.Paths;
import java.util.List;
import java.util.Random;
import java.util.concurrent.Executors;
import java.util.function.Consumer;
import java.util.stream.Collectors;

public class TestCaseExecutor {
    private static int testCounter = 0;

    /**
     * @param testCasePaths paths to test case; only accepts list of p files
     * @return 0 = successful, 1 = compile error, 2 = dynamic error
     */
    static int runTestCase(
            List<String> testCasePaths,
            String testCasePathPrefix,
            String runArgs,
            String mainOutputDirectory,
            int expected) {
        int resultCode;

        testCounter++;

        // Invoke the P compiler to compile the test Case
        boolean isWindows = System.getProperty("os.name").toLowerCase().startsWith("windows");
        String compilerDirectory = "../../../P/Bld/Drops/Release/Binaries/net8.0/p.dll";

        assert testCasePaths.stream().allMatch(p -> p.contains(testCasePathPrefix));
        String testName = testCasePathPrefix.substring(testCasePathPrefix.lastIndexOf("/") + 1);
        if (testName.isEmpty()) {
            List<String> testCaseRelPaths =
                    testCasePaths.stream()
                            .map(p -> p.substring(p.indexOf(testCasePathPrefix) + testCasePathPrefix.length()))
                            .toList();
            testName = Paths.get(testCaseRelPaths.get(0)).getFileName().toString();
        }
        testName = testName.replaceAll("_", "");
        testName = testName.replaceAll("-", "");

        String outputDirectory = mainOutputDirectory + "/" + testName;
        recreateDirectory(outputDirectory);
        PExTestLogger.log(String.format("  [%d] %s", testCounter, testName));

        List<String> pTestCasePaths =
                testCasePaths.stream().filter(p -> p.contains(".p")).collect(Collectors.toList());
        String testCasePathsString = String.join(" ", pTestCasePaths);

        Process process;
        try {
            String pCompileCommand =
                    String.format(
                            "dotnet %s compile --mode pex --projname %s --outdir %s --pfiles %s",
                            compilerDirectory, testName, outputDirectory + "/PGenerated", testCasePathsString);
            PExTestLogger.log("      compiling");
            process = buildCompileProcess(pCompileCommand, outputDirectory);

            StreamGobbler errorStreamGobbler =
                    new StreamGobbler(process.getErrorStream(), System.out::println);
            Executors.newSingleThreadExecutor().submit(errorStreamGobbler);
            StreamGobbler streamGobbler =
                    new StreamGobbler(process.getInputStream(), System.out::println);
            Executors.newSingleThreadExecutor().submit(streamGobbler);
            resultCode = process.waitFor();
        } catch (IOException | InterruptedException e) {
            e.printStackTrace();
            resultCode = -1;
        }

        String pathToJar =
                outputDirectory + "/PGenerated/PEx/target/" + testName + "-jar-with-dependencies.jar";

        File jarFile = new File(pathToJar);
        if (!jarFile.exists() || jarFile.isDirectory()) {
            resultCode = 1;
        }

        if (resultCode != 0) {
            PExTestLogger.log("      compile-fail");
            if (resultCode != expected) {
                PExTestLogger.log(
                        String.format(
                                "      unexpected result for %s (expected: %d, got: %d)",
                                testCasePathPrefix, expected, resultCode));
            }
            return resultCode;
        }

        // Next, try to dynamically load and compile this file
        try {
            int seed = Math.abs((new Random()).nextInt());
            String runJarCommand =
                    String.format(
                            "dotnet %s check %s --mode pex --outdir %s --seed %d %s",
                            compilerDirectory, pathToJar, outputDirectory + "/PCheckerOutput", seed, runArgs);
            PExTestLogger.log(String.format("      running with seed %d", seed));
            process = buildRunProcess(runJarCommand, outputDirectory);

            StreamGobbler streamGobbler =
                    new StreamGobbler(process.getErrorStream(), System.out::println);
            Executors.newSingleThreadExecutor().submit(streamGobbler);
            StreamGobbler outstreamGobbler =
                    new StreamGobbler(process.getInputStream(), System.out::println);
            Executors.newSingleThreadExecutor().submit(outstreamGobbler);
            resultCode = process.waitFor();

            if (resultCode == 0) {
                PExTestLogger.log("      ok");
            } else if (resultCode == 2) {
                PExTestLogger.log("      bug");
            } else if (resultCode == 3) {
                PExTestLogger.log("      timeout");
                resultCode = 0;
            } else if (resultCode == 4) {
                PExTestLogger.log("      memout");
            } else {
                PExTestLogger.log("      error");
            }
        } catch (IOException | InterruptedException e) {
            PExTestLogger.error("      fail");
            e.printStackTrace();
            resultCode = -1;
        }
        if (resultCode != expected) {
            PExTestLogger.log(
                    String.format(
                            "      unexpected result for %s (expected: %d, got: %d)",
                            testCasePathPrefix, expected, resultCode));
        }
        return resultCode;
    }

    /**
     * A method to build a new Process object for given compile command.
     *
     * @param cmd       Jar command as string
     * @param outFolder output folder
     * @return A new process for the given task
     * @throws IOException
     */
    private static Process buildCompileProcess(String cmd, String outFolder) throws IOException {
        ProcessBuilder builder = new ProcessBuilder(cmd.split(" "));
        File outFile = new File(outFolder + "/compile.out");
        outFile.getParentFile().mkdirs();
        outFile.createNewFile();
        builder.redirectOutput(outFile);

        File errFile = new File(outFolder + "/compiler.err");
        errFile.getParentFile().mkdirs();
        errFile.createNewFile();
        builder.redirectError(errFile);
        return builder.start();
    }

    /**
     * A method to build a new Process object for given run command.
     *
     * @param cmd       Jar command as string
     * @param outFolder output folder
     * @return A new process for the given task
     * @throws IOException
     */
    private static Process buildRunProcess(String cmd, String outFolder) throws IOException {
        ProcessBuilder builder = new ProcessBuilder(cmd.split("\\s+"));
        File outFile = new File(outFolder + "/run.out");
        outFile.getParentFile().mkdirs();
        outFile.createNewFile();
        builder.redirectOutput(outFile);

        File errFile = new File(outFolder + "/run.err");
        errFile.getParentFile().mkdirs();
        errFile.createNewFile();
        builder.redirectError(errFile);
        return builder.start();
    }

    private static void recreateDirectory(String dir) {
        try {
            File f = new File(dir);
            if (f.isDirectory()) {
                FileUtils.cleanDirectory(f); // clean out directory (this is optional -- but good know)
                FileUtils.forceDelete(f); // delete directory
            }
            FileUtils.forceMkdir(f); // create directory
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    private record StreamGobbler(InputStream inputStream, Consumer<String> consumer) implements Runnable {

        @Override
        public void run() {
            new BufferedReader(new InputStreamReader(inputStream)).lines().forEach(consumer);
        }
    }
}
