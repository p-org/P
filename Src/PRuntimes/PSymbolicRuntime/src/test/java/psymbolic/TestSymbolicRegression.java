package psymbolic;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.DynamicTest;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.TestFactory;
import org.junit.jupiter.api.function.Executable;
import psymbolic.runtime.logger.Log4JConfig;

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
 * Runner for Symbolic P Regressions.
 * Pre-requisites:
 *  Run from P repository as a submodule
 *  Build the symbolic compiler to ../Bld/Drops/Release/Binaries/Pc.dll
 *  Place test cases as source P files at ../Tst/SymbolicRegressionTests/
 */
public class TestSymbolicRegression {
    private String runArgs = "-sb 1 -cb 1 -me 2";
    private String outputDirectory = "output/testCases";

    Map<String, List<String>> getFiles(String testDirPath, String[] excluded) {
        Map<String, List<String>> result = new HashMap<>();
        File[] directories = new File(testDirPath).listFiles(File::isDirectory);
        for (File dir : directories) {
            if (excluded != null) {
                if (Arrays.stream(excluded).anyMatch(dir.toString()::equals)) {
                    continue;
                }
            }
            try (Stream<Path> walk = Files.walk(Paths.get(dir.toURI()))) {
                Stream<String> projectFilesStream = walk.map(Path::toString)
                        .filter(f -> f.endsWith(".java") || f.endsWith(".p"));
                if (excluded != null) {
                    projectFilesStream = projectFilesStream.filter(f -> Arrays.stream(excluded).noneMatch(f::contains));
                }
                List<String> projectFiles = projectFilesStream.collect(Collectors.toList());
                if (!projectFiles.isEmpty())
                    result.put(dir.toString(), projectFiles);
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
        PSymTestLogger.log(String.format("  Found %s tests in %s", result.size(), testDirPath));
        return result;
    }

    void runDynamicTest(int expected, List<String> testCasePaths, String testCasePath, String runArgs, Collection<DynamicTest> dynamicTests) {
        Executable exec = () -> Assertions.assertEquals(expected, TestCaseExecutor.runTestCase(testCasePaths, testCasePath, runArgs, outputDirectory, expected));
        DynamicTest dynamicTest = DynamicTest.dynamicTest(testCasePath, () -> assertTimeoutPreemptively(Duration.ofMinutes(60), exec));
        dynamicTests.add(dynamicTest);
    }

    Collection<DynamicTest> loadTests(String testDirPath, String[] excluded) {
        if (!PSymTestLogger.isInitialized()) {
            Log4JConfig.configureLog4J();
            PSymTestLogger.Initialize(outputDirectory);
        }

        Collection<DynamicTest> dynamicTests = new ArrayList<>();

        List<String> testDirs = new ArrayList<>();

        try (Stream<Path> walk = Files.walk(Paths.get(testDirPath))) {
            testDirs = walk.map(Path::toString)
                    .filter(f -> f.endsWith("Correct") || f.endsWith("DynamicError") || f.endsWith("StaticError")).collect(Collectors.toList());
        } catch (IOException e) {
            e.printStackTrace();
        }

        for (String testDir : testDirs) {
            Map<String, List<String>> paths = getFiles(testDir, excluded);
            List<String> pathKeys = new ArrayList<>(paths.keySet());
            Collections.sort(pathKeys, String.CASE_INSENSITIVE_ORDER);

            if (testDir.contains("Correct")) {
                for (String key : pathKeys) {
                    runDynamicTest(0, paths.get(key), key, runArgs, dynamicTests);
                }
            } else if (testDir.contains("DynamicError")) {
                for (String key : pathKeys) {
                    runDynamicTest(2, paths.get(key), key, runArgs, dynamicTests);
                }
            } else if (testDir.contains("StaticError")) {
                for (String key : pathKeys) {
                    runDynamicTest(1, paths.get(key), key, runArgs, dynamicTests);
                }
            }
        }
        return dynamicTests;
    }

    @Test
    void Dummy() {}

    @TestFactory
    //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    public Collection<DynamicTest>  loadPingPongTests() {
        return loadTests("./SymbolicRegressionTests/PingPong", null);
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest>  loadSymbolicRegressionsTests() {
        return loadTests("./SymbolicRegressionTests/Integration", null);
    }

    @TestFactory
    //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    public Collection<DynamicTest>  loadIntegrationTests() {
        return loadTests("../../../Tst/RegressionTests/Integration", null);
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest>  loadCombinedTests() {
        return loadTests("../../../Tst/RegressionTests/Combined", null);
    }

    @TestFactory
    Collection<DynamicTest>  loadSMLevelDeclsTests() {
        return loadTests("../../../Tst/RegressionTests/Feature1SMLevelDecls", null);
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest>  loadStmtsTests() {
        return loadTests("../../../Tst/RegressionTests/Feature2Stmts", null);
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest>  loadExpressionTests() {
        String[] excluded = new String[]{
                "../../../Tst/RegressionTests/Feature3Exprs/Correct/ShortCircuitEval"
        };

        return loadTests("../../../Tst/RegressionTests/Feature3Exprs", excluded);
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest>  loadDataTypeTests() {
        return loadTests("../../../Tst/RegressionTests/Feature4DataTypes", null);
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest>  loadModuleSystemTests() {
        return loadTests("../../../Tst/RegressionTests/Feature5ModuleSystem", null);
    }
}
