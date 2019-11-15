using NUnit.Framework;
using Plang.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnitTests.Core;

namespace UnitTests
{
    [SetUpFixture]
    public class RegressionScratchDirectorySetup
    {
        [OneTimeTearDown]
        public void RemoveEmptyDirectories()
        {
            DirectoryInfo baseDir = new DirectoryInfo(Constants.ScratchParentDirectory);
            if (baseDir.Exists)
            {
                RecursiveRemoveEmptyDirectories(baseDir);
            }
        }

        private static void RecursiveRemoveEmptyDirectories(DirectoryInfo dir)
        {
            foreach (DirectoryInfo subdir in dir.EnumerateDirectories())
            {
                RecursiveRemoveEmptyDirectories(subdir);
            }

            if (!dir.EnumerateFileSystemInfos().Any())
            {
                dir.Delete();
            }
        }
    }

    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class CompileOnlyRegressionTests
    {
        private static IEnumerable<TestCaseData> RegressionTestSuite =>
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory, new[] { "StaticError" });

        [TestCaseSource(nameof(RegressionTestSuite))]
        public void TestRegressions(DirectoryInfo testDir)
        {
            DirectoryInfo scratchDir = Directory.CreateDirectory(Constants.ScratchParentDirectory);
            TestCaseFactory factory = new TestCaseFactory(scratchDir);
            CompilerTestCase testCaseC = factory.CreateTestCase(testDir);
            TestAssertions.AssertTestCase(testCaseC);
        }
    }

    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class PrtRegressionTests
    {
        private static IEnumerable<TestCaseData> RegressionTestSuite =>
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory, new[] { "DynamicError", "Correct" });

        [TestCaseSource(nameof(RegressionTestSuite))]
        public void TestRegressions(DirectoryInfo testDir)
        {
            DirectoryInfo scratchDir = Directory.CreateDirectory(Constants.ScratchParentDirectory);
            TestCaseFactory factory = new TestCaseFactory(scratchDir);
            CompilerTestCase testCaseC = factory.CreateTestCase(testDir, CompilerOutput.C);
            TestAssertions.AssertTestCase(testCaseC);
        }

        /*[Test]
        public void TestTemp()
        {
            var tempDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, "TestTemp"));
            var tempFilePath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "tmp", "test.p"));
            var nativeFiles = tempFilePath.Directory.GetFiles("*.c");

            if (!tempFilePath.Exists) return;

            var testCase = new CompilerTestCase(tempDir, new PrtRunner(new[] {tempFilePath}, nativeFiles),
                new ExecutionOutputValidator(0, null, null));

            TestAssertions.AssertTestCase(testCase);
        }*/
    }

    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class PrtSharpRegressionTests
    {
        private static IEnumerable<TestCaseData> RegressionTestSuite =>
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory, new[] { "DynamicError", "DynamicErrorPrtSharp", "Correct", "CorrectPrtSharp" });

        [TestCaseSource(nameof(RegressionTestSuite))]
        public void TestRegressions(DirectoryInfo testDir)
        {
            DirectoryInfo scratchDir = Directory.CreateDirectory(Constants.ScratchParentDirectory);
            TestCaseFactory factory = new TestCaseFactory(scratchDir);
            CompilerTestCase testCaseCoyote = factory.CreateTestCase(testDir, CompilerOutput.Coyote);
            TestAssertions.AssertTestCase(testCaseCoyote);
        }
    }
}