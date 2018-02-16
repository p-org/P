using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Pc;
using NUnit.Framework;
using UnitTestsCore;

namespace UnitTests.PSharpBackend
{
    [TestFixture]
    //[Parallelizable(ParallelScope.Children)]
    public class PSharpTests
    {
        private static IEnumerable<TestCaseData> TestCases =>
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);

        private static bool RunTest(out string output, params FileInfo[] inputFiles)
        {
            var compiler = new AntlrCompiler();
            var compilerOutput = new StringWriter();
            bool success = compiler.Compile(new CompilerOutputStream(compilerOutput), new CommandLineOptions
            {
                compilerOutput = CompilerOutput.PSharp,
                inputFileNames = inputFiles.Select(file => file.FullName).ToList()
            });
            output = compilerOutput.ToString().Trim();
            return success;
        }

        [Test]
        public void DetectUnsplitTests()
        {
            var totalUnsplitTests = 0;
            var exceptions = new HashSet<string>
            {
                "RegressionTests/Combined/StaticError/DuplicateActions",
                "RegressionTests/Combined/StaticError/DuplicateTransitions",
                "RegressionTests/Feature1SMLevelDecls/StaticError/DeferIgnoreSameEvent"
            };
            foreach (TestCaseData test in TestCases)
            {
                var testDir = (DirectoryInfo) test.Arguments[0];
                string testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar)
                                  .MakeRelativeUri(new Uri(testDir.FullName))
                                  .ToString();
                bool expectCorrect = testName.Contains("Correct") || testName.Contains("DynamicError");
                if (!expectCorrect)
                {
                    string[] lines = File.ReadAllLines(Path.Combine(testDir.FullName, "Pc", "acc_0.txt"));
                    if (lines.Count(line => line.StartsWith("OUT:")) != 2 && !exceptions.Contains(testName))
                    {
                        Console.WriteLine($"==== {testName} ====");
                        Console.WriteLine(
                            string.Join(Environment.NewLine, lines.Where(line => line.StartsWith("OUT:"))));
                        Console.WriteLine();

                        totalUnsplitTests++;
                    }
                }
            }

            Console.WriteLine($"Total remaining = {totalUnsplitTests} / {TestCases.Count()}");
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void TestAllRegressions(DirectoryInfo testDir, Dictionary<TestType, TestConfig> testConfigs)
        {
            string testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar)
                              .MakeRelativeUri(new Uri(testDir.FullName))
                              .ToString();
            bool expectCorrect = testName.Contains("Correct") || testName.Contains("DynamicError");
            FileInfo[] inputFiles = testDir.GetFiles("*.p");
            bool result = RunTest(out string output, inputFiles);
            string fileList = string.Join("\n\t", inputFiles.Select(fi => $"file: {fi.FullName}"));
            if (expectCorrect && !result)
            {
                Assert.Fail($"Expected correct, but error was found: {output}\n\t{fileList}\n");
            }
            else if (!expectCorrect && result)
            {
                Assert.Fail($"Expected error, but none were found!\n\t{fileList}\n");
            }

            Assert.Pass($"{output}\n\t{fileList}\n");
        }
    }
}
