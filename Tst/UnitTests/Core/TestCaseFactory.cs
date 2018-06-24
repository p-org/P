using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnitTests.Runners;
using UnitTests.Validators;

namespace UnitTests.Core
{
    public class TestCaseFactory
    {
        private readonly DirectoryInfo testTempBaseDir;

        public TestCaseFactory(DirectoryInfo testTempBaseDir)
        {
            this.testTempBaseDir = testTempBaseDir;
        }

        public CompilerTestCase CreateTestCase(DirectoryInfo testDir, TestConfig runConfig)
        {
            // eg. RegressionTests/F1/Correct/TestCaseName
            var inputFiles = testDir.GetFiles("*.p");
            string testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar)
                              .MakeRelativeUri(new Uri(testDir.FullName))
                              .ToString();

            ICompilerTestRunner runner;
            ITestResultsValidator validator;

            if (runConfig != null)
            {
                runner = new ExecutionRunner(inputFiles);

                string expectedOutput = File.ReadAllText(Path.Combine(testDir.FullName, "Prt", Constants.CorrectOutputFileName));
                ParseExpectedOutput(expectedOutput, out string stdout, out string stderr, out int exitCode);
                // TODO: fix golden outputs for dynamic error assertions
                if (testName.Contains("/DynamicError/"))
                {
                    stdout = null;
                    stderr = null;
                }
                validator = new ExecutionOutputValidator(exitCode, stdout, stderr);
            }
            else
            {
                runner = new TranslationRunner(inputFiles);

                // TODO: validate information about the particular kind of compiler error
                bool isStaticError = testName.Contains("/StaticError/");
                validator = isStaticError ? (ITestResultsValidator) new StaticErrorValidator() : new CompileSuccessValidator();
            }

            DirectoryInfo tempDirName = Directory.CreateDirectory(Path.Combine(testTempBaseDir.FullName, testName));
            return new CompilerTestCase(tempDirName, runner, validator);
        }

        private void ParseExpectedOutput(string expected, out string stdout, out string stderr, out int exitCode)
        {
            exitCode = 0;

            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();
            bool sawExitCode = false;
            foreach (string line in expected.Split('\r', '\n'))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                Match match = Regex.Match(line, @"^(?<tag>OUT|ERROR|EXIT): (?<text>.*)$");
                if (!match.Success)
                {
                    throw new TestRunException(TestCaseError.InvalidOutputSpec);
                }

                string tag = match.Groups["tag"].Value;
                string lineText = match.Groups["text"].Value;
                switch (tag)
                {
                    case "OUT":
                        stdoutBuilder.Append($"{lineText}\n");
                        break;
                    case "ERROR":
                        stderrBuilder.Append($"{lineText}\n");
                        break;
                    case "EXIT":
                        if (sawExitCode || !int.TryParse(lineText, out exitCode))
                        {
                            throw new TestRunException(TestCaseError.InvalidOutputSpec);
                        }

                        sawExitCode = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(expected));
                }
            }

            if (!sawExitCode)
            {
                throw new TestRunException(TestCaseError.InvalidOutputSpec);
            }

            stdout = stdoutBuilder.ToString();
            stderr = stderrBuilder.ToString();
        }
    }
}
