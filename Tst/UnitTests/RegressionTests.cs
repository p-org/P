using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler;
using NUnit.Framework;
using UnitTests.Core;
using UnitTests.Runners;
using UnitTests.Validators;

namespace UnitTests
{
    [SetUpFixture]
    public class RegressionScratchDirectorySetup
    {
        [OneTimeTearDown]
        public void RemoveEmptyDirectories()
        {
            var baseDir = new DirectoryInfo(Constants.ScratchParentDirectory);
            if (baseDir.Exists) RecursiveRemoveEmptyDirectories(baseDir);
        }

        private static void RecursiveRemoveEmptyDirectories(DirectoryInfo dir)
        {
            foreach (var subdir in dir.EnumerateDirectories()) RecursiveRemoveEmptyDirectories(subdir);

            if (!dir.EnumerateFileSystemInfos().Any()) dir.Delete();
        }
    }

    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class CompileOnlyRegressionTests
    {
        private static IEnumerable<TestCaseData> RegressionTestSuite =>
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory, new[] {"Pc"});

        [TestCaseSource(nameof(RegressionTestSuite))]
        public void TestRegressions(DirectoryInfo testDir)
        {
            var scratchDir = Directory.CreateDirectory(Constants.ScratchParentDirectory);
            var factory = new TestCaseFactory(scratchDir);
            var testCaseC = factory.CreateTestCase(testDir);
            TestAssertions.AssertTestCase(testCaseC);
        }
    }

    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class PrtRegressionTests
    {
        private static IEnumerable<TestCaseData> RegressionTestSuite =>
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory, new[] {"Prt"});

        [TestCaseSource(nameof(RegressionTestSuite))]
        public void TestRegressions(DirectoryInfo testDir)
        {
            var scratchDir = Directory.CreateDirectory(Constants.ScratchParentDirectory);
            var factory = new TestCaseFactory(scratchDir);
            var testCaseC = factory.CreateTestCase(testDir, CompilerOutput.C);
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
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory, new[] {"Prt", "PrtSharp" });

        [TestCaseSource(nameof(RegressionTestSuite))]
        public void TestRegressions(DirectoryInfo testDir)
        {
            var scratchDir = Directory.CreateDirectory(Constants.ScratchParentDirectory);
            var factory = new TestCaseFactory(scratchDir);
            var testCasePSharp = factory.CreateTestCase(testDir, CompilerOutput.PSharp);
            TestAssertions.AssertTestCase(testCasePSharp);
        }
    }
}