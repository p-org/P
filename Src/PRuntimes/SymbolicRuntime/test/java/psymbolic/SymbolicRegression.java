package psymbolic;

import org.junit.jupiter.api.DynamicTest;
import org.junit.jupiter.api.TestFactory;
import org.junit.jupiter.api.function.Executable;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.Duration;
import java.util.*;
import java.util.stream.Collectors;
import java.util.stream.Stream;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTimeoutPreemptively;

/**
 * Runner for Symbolic P Regressions.
 * Pre-requisites:
 *  Run from P repository as a submodule
 *  Build the symbolic compiler to ../Bld/Drops/Release/Binaries/Pc.dll
 *  Place test cases as source P files at ../Tst/SymbolicRegressionTests/
 */
public class SymbolicRegression {

    Map<String, List<String>> getFiles(String testDirPath, String[] excluded) {
        Map<String, List<String>> result = new HashMap<>();
        File[] directories = new File(testDirPath).listFiles(File::isDirectory);
        for (File dir : directories) {
            try (Stream<Path> walk = Files.walk(Paths.get(dir.toURI()))) {
                Stream<String> projectFilesStream = walk.map(Path::toString)
                        .filter(f -> f.endsWith(".java") || f.endsWith(".p"));
                if (excluded != null) {
                    projectFilesStream = projectFilesStream.filter(f -> Arrays.stream(excluded).noneMatch(f::contains));
                }
                List<String> projectFiles = projectFilesStream.collect(Collectors.toList());
                projectFiles.forEach(System.out::println);
                if (!projectFiles.isEmpty())
                    result.put(dir.toString(), projectFiles);
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
        return result;
    }

    void runDynamicTest(int expected, List<String> testCasePaths, String testCasePath, Collection<DynamicTest> dynamicTests) {
        Executable exec = () -> assertEquals(expected, TestCaseExecutor.runTestCase(testCasePaths));
        DynamicTest dynamicTest = DynamicTest.dynamicTest(testCasePath, () -> assertTimeoutPreemptively(Duration.ofMinutes(60), exec));
        dynamicTests.add(dynamicTest);
    }

    Collection<DynamicTest> loadTests(String testDirPath, String[] excluded) {

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
            Executable exec;
            if (testDir.contains("Correct")) {
                for (Map.Entry<String, List<String>> entry : paths.entrySet()) {
                    runDynamicTest(0, entry.getValue(), entry.getKey(), dynamicTests);
                }
            } else if (testDir.contains("DynamicError")) {
                for (Map.Entry<String, List<String>> entry : paths.entrySet()) {
                    runDynamicTest(2, entry.getValue(), entry.getKey(), dynamicTests);
                }
            } else if (testDir.contains("StaticError")) {
                for (Map.Entry<String, List<String>> entry : paths.entrySet()) {
                    runDynamicTest(1, entry.getValue(), entry.getKey(), dynamicTests);
                }
            }
        }
        return dynamicTests;
    }

    @TestFactory
    //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest>  loadIntegrationTests() {
        return loadTests("../../../Tst/RegressionTests/Integration", null);
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest>  loadDataTypeTests() {
        return loadTests("../../../Tst/RegressionTests/Feature4DataTypes", null);
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest>  loadExpressionTests() {
        return loadTests("../../../Tst/RegressionTests/Feature3Exprs", null);
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest>  loadStmtsTests() {
        return loadTests("../../../Tst/RegressionTests/Feature2Stmts", null);
    }
}
