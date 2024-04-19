package pexplicit;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.DynamicTest;
import org.junit.jupiter.api.TestFactory;
import org.junit.jupiter.api.function.Executable;
import pexplicit.runtime.logger.Log4JConfig;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.Duration;
import java.util.*;
import java.util.stream.Collectors;
import java.util.stream.Stream;

import static org.junit.jupiter.api.Assertions.assertTimeoutPreemptively;

/**
 * Runner for PExplicit regressions.
 */
public class TestPExplicit {
    private static final String outputDirectory = "output/testCases";
    private static final List<String> excluded = new ArrayList<>();
    private static String timeout = "60";
    private static String schedules = "100";
    private static String maxSteps = "10000";
    private static String runArgs = "--sch-coverage dfs -v";
    private static boolean initialized = false;

    private static void setRunArgs() {
        String to = System.getProperty("timeout");
        String it = System.getProperty("schedules");
        String ms = System.getProperty("max.steps");

        if (to != null && !to.isEmpty()) {
            timeout = to;
        }
        if (it != null && !it.isEmpty()) {
            schedules = it;
        }
        if (ms != null && !ms.isEmpty()) {
            maxSteps = ms;
        }

        runArgs += String.format(" --timeout %s --schedules %s --max-steps %s", timeout, schedules, maxSteps);

        PExplicitTestLogger.log(String.format("Running with arguments:  %s", runArgs));
    }

    private static void createExcludeList() {
        /**
         * TODO: Null events
         */
        // TODO: Null events are not supported
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/Correct/BugRepro");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/Correct/MoreThan32Events");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_36");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_37");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_38");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_39");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_41");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_42");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_10");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_12");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_16");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_17");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_18");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_19");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_9");
        excluded.add("../../../Tst/RegressionTests/Integration/Correct/openwsn1");
        // TODO: Null events are not supported, found in a receive statement
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/DynamicError/receive2");
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/DynamicError/receive7");
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/DynamicError/receive10");
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/DynamicError/receive11");
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/DynamicError/receive12");

        /**
         * TODO: liveness with temperatures
         */
        excluded.add("../../../Tst/RegressionTests/Liveness/Correct/Liveness_1");
        excluded.add("../../../Tst/RegressionTests/Liveness/Correct/Liveness_1_falsePass");
        excluded.add("../../../Tst/RegressionTests/Liveness/Correct/Liveness_FAIRNONDET");
        excluded.add("../../../Tst/RegressionTests/Liveness/Correct/Liveness_FAIRNONDET2");
    }

    private static void initialize() {
        Log4JConfig.configureLog4J();
        PExplicitTestLogger.Initialize(outputDirectory);
        setRunArgs();
        createExcludeList();
        initialized = true;
    }

    Map<String, List<String>> getFiles(String testDirPath) {
        Map<String, List<String>> result = new HashMap<>();
        File[] directories = new File(testDirPath).listFiles(File::isDirectory);
        for (File dir : directories) {
            if (excluded.stream().anyMatch(dir.toString()::equals)) {
                continue;
            }
            try (Stream<Path> walk = Files.walk(Paths.get(dir.toURI()))) {
                Stream<String> projectFilesStream =
                        walk.map(Path::toString).filter(f -> f.endsWith(".java") || f.endsWith(".p"));
                projectFilesStream =
                        projectFilesStream.filter(f -> excluded.stream().noneMatch(f::contains));
                List<String> projectFiles = projectFilesStream.collect(Collectors.toList());
                if (!projectFiles.isEmpty()) result.put(dir.toString(), projectFiles);
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
        PExplicitTestLogger.log(String.format("  Found %s tests in %s", result.size(), testDirPath));
        return result;
    }

    void runDynamicTest(
            int expected,
            List<String> testCasePaths,
            String testCasePath,
            Collection<DynamicTest> dynamicTests) {
        Executable exec =
                () ->
                        Assertions.assertEquals(
                                expected,
                                TestCaseExecutor.runTestCase(
                                        testCasePaths,
                                        testCasePath,
                                        TestPExplicit.runArgs,
                                        outputDirectory,
                                        expected));
        DynamicTest dynamicTest =
                DynamicTest.dynamicTest(
                        testCasePath, () -> assertTimeoutPreemptively(Duration.ofMinutes(60), exec));
        dynamicTests.add(dynamicTest);
    }

    Collection<DynamicTest> loadTests(String testDirPath) {
        if (!initialized) {
            initialize();
        }

        Collection<DynamicTest> dynamicTests = new ArrayList<>();

        List<String> testDirs = new ArrayList<>();

        try (Stream<Path> walk = Files.walk(Paths.get(testDirPath))) {
            testDirs =
                    walk.map(Path::toString)
                            .filter(
                                    f ->
                                            f.endsWith("Correct")
                                                    || f.endsWith("DynamicError")
                                                    || f.endsWith("StaticError"))
                            .collect(Collectors.toList());
        } catch (IOException e) {
            e.printStackTrace();
        }

        for (String testDir : testDirs) {
            Map<String, List<String>> paths = getFiles(testDir);
            List<String> pathKeys = new ArrayList<>(paths.keySet());
            Collections.sort(pathKeys, String.CASE_INSENSITIVE_ORDER);

            if (testDir.contains("Correct")) {
                for (String key : pathKeys) {
                    runDynamicTest(0, paths.get(key), key, dynamicTests);
                }
            } else if (testDir.contains("DynamicError")) {
                for (String key : pathKeys) {
                    runDynamicTest(2, paths.get(key), key, dynamicTests);
                }
            } else if (testDir.contains("StaticError")) {
                for (String key : pathKeys) {
                    runDynamicTest(1, paths.get(key), key, dynamicTests);
                }
            }
        }
        return dynamicTests;
    }

    @TestFactory
        // @Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest> loadSymbolicRegressionsTests() {
        return loadTests("../PSymRuntime/SymbolicRegressionTests/Integration");
    }

    @TestFactory
    // @Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    public Collection<DynamicTest> loadIntegrationTests() {
        return loadTests("../../../Tst/RegressionTests/Integration");
    }

    @TestFactory
        // @Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest> loadCombinedTests() {
        return loadTests("../../../Tst/RegressionTests/Combined");
    }

    @TestFactory
    Collection<DynamicTest> loadSMLevelDeclsTests() {
        return loadTests("../../../Tst/RegressionTests/Feature1SMLevelDecls");
    }

    @TestFactory
        // @Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest> loadStmtsTests() {
        return loadTests("../../../Tst/RegressionTests/Feature2Stmts");
    }

    @TestFactory
        // @Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest> loadExpressionTests() {
        return loadTests("../../../Tst/RegressionTests/Feature3Exprs");
    }

    @TestFactory
        // @Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest> loadDataTypeTests() {
        return loadTests("../../../Tst/RegressionTests/Feature4DataTypes");
    }

    // TODO Unsupported: module system
    //    @TestFactory
    //        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    //    Collection<DynamicTest>  loadModuleSystemTests() {
    //        return loadTests("../../../Tst/RegressionTests/Feature5ModuleSystem");
    //    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest> loadLivenessTests() {
        return loadTests("../../../Tst/RegressionTests/Liveness");
    }
}
