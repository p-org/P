package psymbolic;

import psymbolic.run.EntryPoint;
import psymbolic.run.Program;
import psymbolic.runtime.CompilerLogger;

import javax.tools.JavaCompiler;
import javax.tools.JavaFileObject;
import javax.tools.StandardJavaFileManager;
import javax.tools.ToolProvider;
import java.io.*;
import java.lang.reflect.InvocationTargetException;
import java.net.MalformedURLException;
import java.net.URL;
import java.net.URLClassLoader;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.Executors;
import java.util.function.Consumer;
import java.util.stream.Collectors;


public class TestCaseExecutor {
    // This can cause collisions if you have two very similar directory names, like 'Foo_Bar' and 'Foo-Bar', but that
    // should be (?) acceptable for our purposes.
    private static String sanitizeRelDir(String relDir) {
        return relDir.replace(' ', '_').replace('-', '_');
    }

    private static String packageNameFromRelDir(String relDir) {
        assert relDir.equals(sanitizeRelDir(relDir));
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

    private final static boolean PRINT_STATIC_ERRORS = true;
    /**
     * @param testCasePaths paths to test case; only accepts list of p files
     * @return 0 = successful, 1 = compile error, 2 = dynamic error
     */
    static int runTestCase(List<String> testCasePaths) {
        // Invoke the P compiler to compile the test Case
        boolean isWindows = System.getProperty("os.name")
                .toLowerCase().startsWith("windows");
        String compilerDirectory = "../../../Bld/Drops/Release/Binaries/netcoreapp3.1/P.dll";

        String prefix = "SymbolicRegressionTests/";
        assert testCasePaths.stream().allMatch(p -> p.contains(prefix));
        List<String> testCaseRelPaths = testCasePaths.stream().map(p -> p.substring(p.indexOf(prefix) + prefix.length()))
                                        .collect(Collectors.toList());
        testCasePaths.stream().map(p -> p.substring(p.indexOf(prefix) + prefix.length())).forEach(System.out::println);
        String testCaseRelDir = sanitizeRelDir(Paths.get(testCaseRelPaths.get(0)).getParent().toString());
        String outputDirectory = "src/test/java/psymbolic/testCase/" + testCaseRelDir;

        // TODO: make separating out the .p and .java files more robust
        List<String> pTestCasePaths = testCasePaths.stream().filter(p -> p.contains(".p")).collect(Collectors.toList());
        String testCasePathsString = String.join(" ", pTestCasePaths);
        Process process;
        try {
            System.out.println(String.format("dotnet %s %s -generate:Symbolic -outputDir:%s\n"
                                                   , compilerDirectory, testCasePathsString, outputDirectory));
            if (isWindows) {
                process = Runtime.getRuntime()
                        .exec(String.format("dotnet %s %s -generate:Symbolic -outputDir:%s"
                                , compilerDirectory, testCasePathsString, outputDirectory));
            } else {
                process = Runtime.getRuntime()
                        .exec(String.format("dotnet %s %s -generate:Symbolic -outputDir:%s " , compilerDirectory, testCasePathsString, outputDirectory));
            }

            if (PRINT_STATIC_ERRORS) {
                StreamGobbler streamGobbler = new StreamGobbler(process.getErrorStream(), System.out::println);
                Executors.newSingleThreadExecutor().submit(streamGobbler);
            }

            int exitCode = process.waitFor();

            if (exitCode != 0) {
                CompilerLogger.log("Compilation failure.");
                return 1;
            }
        }
        catch (IOException | InterruptedException e) {
            CompilerLogger.log("Compilation failure.");
            e.printStackTrace();
        }

        // Next, try to dynamically load and compile this file
        String[] path_split = Utils.splitPath(testCasePaths.get(0));
        String class_name = path_split[path_split.length-1].split("\\.")[0].toLowerCase();
        String outputPath = outputDirectory + File.separator + class_name + ".java";
        List<String> toCopy = testCasePaths.stream().filter(f -> f.contains(".java")).collect(Collectors.toList());
        List<String> toLoad = new ArrayList<>();
        try {
            for (String copy : toCopy) {
                String[] copy_path_split = Utils.splitPath(copy);
                String external_class_name = copy_path_split[path_split.length-1].split("\\.")[0];
                String copyPath = outputDirectory + File.separator + external_class_name + ".java";
                Files.copy(Paths.get(copy), Paths.get(copyPath), StandardCopyOption.REPLACE_EXISTING);
                toLoad.add(copyPath);
            }
        } catch (IOException e) {
            CompilerLogger.log("Compilation failure.");
            e.printStackTrace();
            return 1;
        }

        // Program to run
        Program p = null;

        // Try to compile the files
        List<String> javaTestCasePaths = testCasePaths.stream().filter(x -> x.contains(".java")).collect(Collectors.toList());
        List<File> sourceFiles = javaTestCasePaths.stream().map(x -> new File(x)).collect(Collectors.toList());
        sourceFiles.add(new File(outputPath));

        List<String> optionList = new ArrayList<>();
        optionList.add("-classpath");
        optionList.add(System.getProperty("java.class.path") + ":" + outputDirectory); // source directory
        optionList.add("-d");
        optionList.add(outputDirectory); // output directory

        JavaCompiler compiler = ToolProvider.getSystemJavaCompiler();

        StandardJavaFileManager fileManager = compiler.getStandardFileManager(null, null, null);
        Iterable<? extends JavaFileObject> compilationUnits =
                fileManager.getJavaFileObjectsFromFiles(sourceFiles);

        boolean compiled = compiler.getTask(
                null,
                fileManager,
                null,
                optionList,
                null,
                compilationUnits).call();

        // Load and instantiate compiled class and external classes
        try {
            // external classes
            for (String fileName : javaTestCasePaths) {
                System.out.println("loading " + fileName);
                URLClassLoader classLoader = URLClassLoader.newInstance(new URL[]{new File(fileName).toURI().toURL()});
            }
            URLClassLoader classLoader = URLClassLoader.newInstance(new URL[]{new File(outputDirectory).toURI().toURL()});
            Class<?> cls = Class.forName(class_name, true, classLoader);
            Object instance = cls.getDeclaredConstructor().newInstance();
            p = (Program) instance;
        } catch (InstantiationException | MalformedURLException | IllegalAccessException | ClassNotFoundException |
                NoSuchMethodException | InvocationTargetException e) {
            CompilerLogger.log("Compilation failure.");
            e.printStackTrace();
            return 1;
        }
        try {
            EntryPoint.run(p, class_name,200, 50);
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
