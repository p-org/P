using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public void TestAllRegressions(CompilerTestCase testCase)
        {
            TestAssertions.AssertTestCase(testCase);
        }

        [Test]
        public void TestTemp()
        {
            DirectoryInfo tempDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, "TestTemp"));
            var tempFilePath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "tmp", "test.p"));

            if (!tempFilePath.Exists)
            {
                return;
            }

            var testCase = new CompilerTestCase(tempDir, new PrtRunner(new[] { tempFilePath }),
                                                new ExecutionOutputValidator(0, null, null));

            TestAssertions.AssertTestCase(testCase);
        }
    }
}
