using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Pc;
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
    public class RegressionTests
    {
        private static IEnumerable<TestCaseData> RegressionTestSuite =>
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);
        
        [TestCaseSource(nameof(RegressionTestSuite))]
        public void TestPrtRegressions(DirectoryInfo testDir, TestConfig runConfig)
        {
            var scratchDir = Directory.CreateDirectory(Constants.ScratchParentDirectory);
            var factory = new TestCaseFactory(scratchDir);
            var testCaseC = factory.CreateTestCase(testDir, runConfig, CompilerOutput.C);
            TestAssertions.AssertTestCase(testCaseC);
        }

        [Test]
        public void TestTemp()
        {
            DirectoryInfo tempDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, "TestTemp"));
            var tempFilePath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "tmp", "test.p"));
            var nativeFiles = tempFilePath.Directory.GetFiles("*.c");

            if (!tempFilePath.Exists)
            {
                return;
            }

            var testCase = new CompilerTestCase(tempDir, new PrtRunner(new[] { tempFilePath }, nativeFiles),
                                                new ExecutionOutputValidator(0, null, null));

            TestAssertions.AssertTestCase(testCase);
        }
    }

    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class PSharpRegressionTests
    {
        private static IEnumerable<TestCaseData> RegressionTestSuite =>
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);

        [TestCaseSource(nameof(RegressionTestSuite))]
        public void TestPSharpRegressions(DirectoryInfo testDir, TestConfig runConfig)
        {
            // TODO: static error test cases are run twice here.
            var scratchDir = Directory.CreateDirectory(Constants.ScratchParentDirectory);
            var factory = new TestCaseFactory(scratchDir);
            var testCasePSharp = factory.CreateTestCase(testDir, runConfig, CompilerOutput.PSharp);
            TestAssertions.AssertTestCase(testCasePSharp);
        }
    }
}
