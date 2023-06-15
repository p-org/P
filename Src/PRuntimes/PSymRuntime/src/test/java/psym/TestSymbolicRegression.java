package psym;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.DynamicTest;
import org.junit.jupiter.api.TestFactory;
import org.junit.jupiter.api.function.Executable;
import psym.runtime.logger.Log4JConfig;

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
 * Run from P repository as a submodule
 * Build the symbolic compiler to ../Bld/Drops/Release/Binaries/Pc.dll
 * Place test cases as source P files at ../Tst/SymbolicRegressionTests/
 */
public class TestSymbolicRegression {
    private static final String runArgs = "--iterations 20 --seed 0";
    private static final String outputDirectory = "output/testCases";
    private static final List<String> excluded = new ArrayList<>();

    private static boolean initialized = false;

    private static void createExcludeList() {
        // TODO Unsupported: deadlock detection
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/DynamicError/receive2");
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/DynamicError/receive6");
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/DynamicError/receive7");

        // TODO Unsupported: continue statement
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/DynamicError/continue1");

        // TODO Unsupported: receive in state exit functions
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/Correct/receive14");
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/Correct/receive15");
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/Correct/receive16");

        // TODO Unsupported: enum starting with non-zero integer values
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/Correct/enum4");

        // TODO Unsupported: relational operations over strings
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/Correct/stringcomp");

        // TODO Unsupported: complex type casting with any type
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/EnumType1");

        // TODO Unsupported: type casting collections with any type
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/Correct/CastInExprsAsserts");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/Correct/nonAtomicDataTypes");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/Correct/nonAtomicDataTypes12");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/Correct/nonAtomicDataTypes13");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/Correct/nonAtomicDataTypesAllAsserts");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/nonAtomicDataTypes1");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/nonAtomicDataTypes10");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/nonAtomicDataTypes2");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/nonAtomicDataTypes3");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/nonAtomicDataTypes4");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/nonAtomicDataTypes5");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/nonAtomicDataTypes6");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/nonAtomicDataTypes7");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/nonAtomicDataTypes8");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/nonAtomicDataTypes9");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/CastInExprs1");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/CastInExprs2");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/CastInExprs3");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/CastInExprs4");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/CastInExprs5");
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/DynamicError/CastInExprs6");

        // TODO Unsupported: comparison of null with any type
        excluded.add("../../../Tst/RegressionTests/Feature4DataTypes/Correct/anyTypeNullValue");

        // TODO Unsupported: null events
        excluded.add("../../../Tst/RegressionTests/Integration/Correct/openwsn1");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_9");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_42");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_12");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_41");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_38");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_16");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_36");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_18");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_19");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_37");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_17");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_OneMachine_39");
        excluded.add("../../../Tst/RegressionTests/Integration/DynamicError/SEM_TwoMachines_10");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/Correct/BugRepro");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/Correct/MoreThan32Events");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/StaticError/ActionAndTransitionSameEvent");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/StaticError/IgnoredNullEvent");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/StaticError/SentNullEvent");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/StaticError/RaisedNullEvent");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/StaticError/AnonFuns");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/StaticError/NullEventDecl");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/StaticError/DeferredNullEvent");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/StaticError/UndefinedStateInTransition");
        excluded.add("../../../Tst/RegressionTests/Feature1SMLevelDecls/StaticError/TransitionOnNullInSpecMachine");
        excluded.add("../../../Tst/RegressionTests/Feature3Exprs/StaticError/payloadEntry");
        excluded.add("../../../Tst/RegressionTests/Feature3Exprs/StaticError/payloadTransitions");
        excluded.add("../../../Tst/RegressionTests/Feature3Exprs/StaticError/payloadActions");
        excluded.add("../../../Tst/RegressionTests/Feature3Exprs/StaticError/payloadEntry_1");
        excluded.add("../../../Tst/RegressionTests/Feature3Exprs/StaticError/payloadActionsFuns");

        // TODO Wait4Fix: exclude test errors due to main machine with spec: issue #510
        excluded.add("../../../Tst/RegressionTests/Integration/Correct/SEM_TwoMachines_14");
        excluded.add("../../../Tst/RegressionTests/Integration/Correct/SEM_TwoMachines_15");
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/Correct/receive11");
        excluded.add("../../../Tst/RegressionTests/Feature2Stmts/Correct/receive11_1");
    }

    private static void initialize() {
        Log4JConfig.configureLog4J();
        PSymTestLogger.Initialize(outputDirectory);
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
                Stream<String> projectFilesStream = walk.map(Path::toString)
                        .filter(f -> f.endsWith(".java") || f.endsWith(".p"));
                projectFilesStream = projectFilesStream.filter(f -> excluded.stream().noneMatch(f::contains));
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

    void runDynamicTest(int expected, List<String> testCasePaths, String testCasePath, Collection<DynamicTest> dynamicTests) {
        Executable exec = () -> Assertions.assertEquals(expected, TestCaseExecutor.runTestCase(testCasePaths, testCasePath, TestSymbolicRegression.runArgs, outputDirectory, expected));
        DynamicTest dynamicTest = DynamicTest.dynamicTest(testCasePath, () -> assertTimeoutPreemptively(Duration.ofMinutes(60), exec));
        dynamicTests.add(dynamicTest);
    }

    Collection<DynamicTest> loadTests(String testDirPath) {
        if (!initialized) {
            initialize();
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
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest> loadSymbolicRegressionsTests() {
        return loadTests("./SymbolicRegressionTests/Integration");
    }

    @TestFactory
    //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    public Collection<DynamicTest> loadIntegrationTests() {
        return loadTests("../../../Tst/RegressionTests/Integration");
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest> loadCombinedTests() {
        return loadTests("../../../Tst/RegressionTests/Combined");
    }

    @TestFactory
    Collection<DynamicTest> loadSMLevelDeclsTests() {
        return loadTests("../../../Tst/RegressionTests/Feature1SMLevelDecls");
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest> loadStmtsTests() {
        return loadTests("../../../Tst/RegressionTests/Feature2Stmts");
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest> loadExpressionTests() {
        return loadTests("../../../Tst/RegressionTests/Feature3Exprs");
    }

    @TestFactory
        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
    Collection<DynamicTest> loadDataTypeTests() {
        return loadTests("../../../Tst/RegressionTests/Feature4DataTypes");
    }

    // TODO Unsupported: module system
//    @TestFactory
//        //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
//    Collection<DynamicTest>  loadModuleSystemTests() {
//        return loadTests("../../../Tst/RegressionTests/Feature5ModuleSystem");
//    }

    // TODO Unsupported: liveness
//    @TestFactory
//    //@Timeout(value = 1, unit = TimeUnit.MILLISECONDS)
//    Collection<DynamicTest>  loadLivenessTests() {
//        return loadTests("../../../Tst/RegressionTests/Liveness");
//    }
}
