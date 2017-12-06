using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Pc;
using NUnit.Framework;
using UnitTests.CBackend;

namespace UnitTests.PSharpBackend
{
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class PSharpTests
    {
        public static IEnumerable<TestCaseData> TestCases =>
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);

        private static FileInfo SolutionPath(params string[] names)
        {
            return new FileInfo(Path.Combine(new[] {Constants.SolutionDirectory}.Concat(names).ToArray()));
        }

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
        [TestCaseSource(nameof(TestCases))]
        public void TestAllRegressions(DirectoryInfo testDir, Dictionary<TestType, TestConfig> testConfigs)
        {
            string testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar)
                .MakeRelativeUri(new Uri(testDir.FullName))
                .ToString();
            bool expectCorrect = testName.Contains("Correct") || testName.Contains("DynamicError");
            bool result = RunTest(out string output, testDir.GetFiles("*.p"));
            if (expectCorrect && !result)
            {
                Assert.Fail($"Expected correct, but error was found: {output}");
            }
            else if (!expectCorrect && result)
            {
                Assert.Fail("Expected error, but none were found!");
            }
        }
        
        [Test]
        public void TestAnalyzeTemp()
        {
            RunTest(out string _, SolutionPath("tmp", "tupOrder.p"), SolutionPath("tmp", "N.p"));
        }
    }
}