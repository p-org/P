package psym;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.List;
import java.util.Random;
import java.util.concurrent.Executors;
import java.util.function.Consumer;
import java.util.stream.Collectors;
import org.apache.commons.io.FileUtils;

public class TestCaseExecutor {
  private static int testCounter = 0;

  private static String packageNameFromRelDir(String relDir) {
    assert !relDir.contains("//");
    assert !relDir.startsWith("/");
    assert !relDir.endsWith("/");
    return "psym.testCase." + relDir.replace('/', '.');
  }

  // We prepend the package directly to the file on disk, rather than to the file contents we read
  // into memory, to
  // permit manual testing of the generated files from IntelliJ.
  private static String prependPackageDeclarationAndRead(String packageName, String filePath) {
    try {
      String fileContents =
          new String(Files.readAllBytes(Paths.get(filePath)), StandardCharsets.UTF_8);
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
  static int runTestCase(
      List<String> testCasePaths,
      String testCasePathPrefix,
      String mode,
      String runArgs,
      String mainOutputDirectory,
      int expected) {
    int resultCode;

    testCounter++;

    // Invoke the P compiler to compile the test Case
    boolean isWindows = System.getProperty("os.name").toLowerCase().startsWith("windows");
    String compilerDirectory = "../../../Bld/Drops/Release/Binaries/net8.0/p.dll";

    assert testCasePaths.stream().allMatch(p -> p.contains(testCasePathPrefix));
    String testName = testCasePathPrefix.substring(testCasePathPrefix.lastIndexOf("/") + 1);
    if (testName.isEmpty()) {
      List<String> testCaseRelPaths =
          testCasePaths.stream()
              .map(p -> p.substring(p.indexOf(testCasePathPrefix) + testCasePathPrefix.length()))
              .collect(Collectors.toList());
      testName = Paths.get(testCaseRelPaths.get(0)).getFileName().toString();
    }
    testName = testName.replaceAll("_", "");
    testName = testName.replaceAll("-", "");

    String outputDirectory = mainOutputDirectory + "/" + testName;
    recreateDirectory(outputDirectory);
    PSymTestLogger.log(String.format("  [%d] %s", testCounter, testName));

    List<String> pTestCasePaths =
        testCasePaths.stream().filter(p -> p.contains(".p")).collect(Collectors.toList());
    String testCasePathsString = String.join(" ", pTestCasePaths);

    Process process;
    try {
      String pCompileCommand =
          String.format(
              "dotnet %s compile --mode %s --projname %s --outdir %s --pfiles %s",
              compilerDirectory, mode, testName, outputDirectory, testCasePathsString);
      PSymTestLogger.log("      compiling");
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
        outputDirectory + "/Symbolic/target/" + testName + "-jar-with-dependencies.jar";

    File jarFile = new File(pathToJar);
    if (!jarFile.exists() || jarFile.isDirectory()) {
      resultCode = 1;
    }

    if (resultCode != 0) {
      PSymTestLogger.log("      compile-fail");
      if (resultCode != expected) {
        PSymTestLogger.log(
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
              "dotnet %s check %s --mode %s --outdir %s --seed %d %s",
              compilerDirectory, pathToJar, mode, outputDirectory, seed, runArgs);
      PSymTestLogger.log(String.format("      running with seed %d", seed));
      process = buildRunProcess(runJarCommand, outputDirectory);

      StreamGobbler streamGobbler =
          new StreamGobbler(process.getErrorStream(), System.out::println);
      Executors.newSingleThreadExecutor().submit(streamGobbler);
      StreamGobbler outstreamGobbler =
          new StreamGobbler(process.getInputStream(), System.out::println);
      Executors.newSingleThreadExecutor().submit(outstreamGobbler);
      resultCode = process.waitFor();

      if (resultCode == 0) {
        PSymTestLogger.log("      ok");
      } else if (resultCode == 2) {
        PSymTestLogger.log("      bug");
      } else if (resultCode == 3) {
        PSymTestLogger.log("      timeout");
      } else if (resultCode == 4) {
        PSymTestLogger.log("      memout");
      } else {
        PSymTestLogger.log("      error");
      }
    } catch (IOException | InterruptedException e) {
      PSymTestLogger.error("      fail");
      e.printStackTrace();
      resultCode = -1;
    }
    if (resultCode != expected) {
      PSymTestLogger.log(
          String.format(
              "      unexpected result for %s (expected: %d, got: %d)",
              testCasePathPrefix, expected, resultCode));
    }
    return resultCode;
  }

  /**
   * A method to build a new Process object for given compile command.
   *
   * @param cmd Jar command as string
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
   * @param cmd Jar command as string
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

  private static class StreamGobbler implements Runnable {
    private final InputStream inputStream;
    private final Consumer<String> consumer;

    StreamGobbler(InputStream inputStream, Consumer<String> consumer) {
      this.inputStream = inputStream;
      this.consumer = consumer;
    }

    @Override
    public void run() {
      new BufferedReader(new InputStreamReader(inputStream)).lines().forEach(consumer);
    }
  }
}
