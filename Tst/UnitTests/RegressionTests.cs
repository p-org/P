using System;
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

        private static void AssertTestCase(CompilerTestCase testCase)
        {
            if (!testCase.EvaluateTest(out string stdout, out string stderr, out var exitCode))
            {
                Console.WriteLine("Test failed!\n");
                WriteOutput(stdout, stderr, exitCode);
                Assert.Fail($"EXIT: {exitCode}\n{stderr}");
            }

            Console.WriteLine("Test succeeded!\n");
            WriteOutput(stdout, stderr, exitCode);

            // Delete ONLY if inside the solution directory
            SafeDeleteDirectory(testCase.ScratchDirectory);
        }

        private static void SafeDeleteDirectory(DirectoryInfo toDelete)
        {
            var safeBase = new DirectoryInfo(Constants.SolutionDirectory);
            for (DirectoryInfo scratch = toDelete; scratch.Parent != null; scratch = scratch.Parent)
            {
                if (string.Compare(scratch.FullName, safeBase.FullName, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    toDelete.Delete(true);
                    return;
                }
            }
        }

        private static void WriteOutput(string stdout, string stderr, int? exitCode)
        {
            if (!string.IsNullOrEmpty(stdout))
            {
                Console.WriteLine($"STDOUT\n======\n{stdout}\n\n");
            }

            if (!string.IsNullOrEmpty(stderr))
            {
                Console.WriteLine($"STDERR\n======\n{stderr}\n\n");
            }

            if (exitCode != null)
            {
                Console.WriteLine($"Exit code = {exitCode}");
            }
        }
        
        [TestCaseSource(nameof(RegressionTestSuite))]
        public void TestAllRegressions(CompilerTestCase testCase)
        {
            AssertTestCase(testCase);
        }

        [Test]
        public void TestTemp()
        {
            DirectoryInfo tempDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, "TestTemp"));
            FileInfo[] inputFiles = {new FileInfo(Path.Combine(Constants.SolutionDirectory, "tmp", "fun.p")) };

            var testCase = new CompilerTestCase(tempDir, new ExecutionRunner(inputFiles),
                                                new ExecutionOutputValidator(0, null, null));

            AssertTestCase(testCase);
        }
    }
}
