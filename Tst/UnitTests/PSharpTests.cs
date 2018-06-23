using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Pc;
using Microsoft.Pc.Backend;
using NUnit.Framework;
using UnitTestsCore;

namespace UnitTests
{
    public enum TestCaseError
    {
        TranslationFailed,
        GeneratedSourceCompileFailed
    }

    [Serializable]
    public class TestRunException : Exception
    {
        public TestRunException(TestCaseError reason)
        {
            Reason = reason;
        }

        public TestRunException(TestCaseError reason, string message) : base(message)
        {
            Reason = reason;
        }

        public TestRunException(TestCaseError reason, string message, Exception inner) : base(message, inner)
        {
            Reason = reason;
        }

        protected TestRunException(
            TestCaseError reason,
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            Reason = reason;
        }

        public TestCaseError Reason { get; }
    }

    public interface ICompilerTestRunner
    {
        int? RunTest(out string stdout, out string stderr);
    }

    public class TranslationRunner : ICompilerTestRunner
    {
        private readonly IReadOnlyList<FileInfo> inputFiles;

        public TranslationRunner(IReadOnlyList<FileInfo> inputFiles)
        {
            this.inputFiles = inputFiles;
        }

        public int? RunTest(out string stdout, out string stderr)
        {
            var compiler = new AntlrCompiler();
            var stdoutWriter = new StringWriter();
            var stderrWriter = new StringWriter();
            var outputStream = new TestCaseOutputStream(stdoutWriter, stderrWriter);
            bool success = compiler.Compile(outputStream, new CommandLineOptions
            {
                compilerOutput = CompilerOutput.C,
                inputFileNames = inputFiles.Select(file => file.FullName).ToList()
            });
            stdout = stdoutWriter.ToString().Trim();
            stderr = stderrWriter.ToString().Trim();
            if (!success)
            {
                throw new TestRunException(TestCaseError.TranslationFailed, stderr);
            }

            return 0;
        }
    }

    public class ExecutionRunner : ICompilerTestRunner
    {
        private readonly DirectoryInfo prtTestProjDirectory;
        private readonly IReadOnlyList<FileInfo> sources;
        private readonly DirectoryInfo temporaryDirectory;

        public ExecutionRunner(DirectoryInfo temporaryDirectory, IReadOnlyList<FileInfo> sources)
        {
            this.temporaryDirectory = temporaryDirectory;
            this.sources = sources;
            prtTestProjDirectory = Directory.CreateDirectory(Path.Combine(Constants.TestDirectory, Constants.CRuntimeTesterDirectoryName));
        }

        public int? RunTest(out string testStdout, out string testStderr)
        {
            DoCompile();

            string tmpDirName = temporaryDirectory.FullName;

            // Copy tester into destination directory.
            FileHelper.CopyFiles(prtTestProjDirectory, tmpDirName);
            if (!RunMsBuildExe(tmpDirName, out testStdout))
            {
                throw new TestRunException(TestCaseError.GeneratedSourceCompileFailed);
            }

            return ProcessHelper.RunWithOutput(
                Path.Combine(tmpDirName, Constants.BuildConfiguration, Constants.Platform, Constants.CTesterExecutableName),
                tmpDirName,
                Enumerable.Empty<string>(),
                out testStdout,
                out testStderr);
        }

        private void DoCompile()
        {
            var compiler = new AntlrCompiler();
            var outputStream = new TestExecutionStream(temporaryDirectory);
            bool success = compiler.Compile(outputStream, new CommandLineOptions
            {
                compilerOutput = CompilerOutput.C,
                inputFileNames = sources.Select(file => file.FullName).ToList(),
                projectName = "main"
            });

            if (!success)
            {
                throw new TestRunException(TestCaseError.TranslationFailed);
            }
        }

        private static bool RunMsBuildExe(string tmpDir, out string output)
        {
            const string msbuildpath = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe";
            int exitStatus = ProcessHelper.RunWithOutput(
                msbuildpath,
                tmpDir,
                new[] {$"/p:Configuration={Constants.BuildConfiguration}", $"/p:Platform={Constants.Platform}", "/t:Build"},
                out string stdout,
                out string stderr
            );
            output = $"{stdout}\n{stderr}";
            return exitStatus == 0;
        }
    }

    public interface ITestResultsValidator
    {
        bool ValidateResult(string stdout, string stderr, int? exitCode);
        bool ValidateException(TestRunException testRunException);
    }

    public class StaticErrorValidator : ITestResultsValidator
    {
        public bool ValidateResult(string stdout, string stderr, int? exitCode)
        {
            return exitCode == null;
        }

        public bool ValidateException(TestRunException testRunException)
        {
            return testRunException.Reason == TestCaseError.TranslationFailed;
        }
    }

    public class CompilerTestCase
    {
        private readonly ICompilerTestRunner runner;
        private readonly ITestResultsValidator validator;

        public CompilerTestCase(ICompilerTestRunner runner, ITestResultsValidator validator)
        {
            this.runner = runner;
            this.validator = validator;
        }

        public bool EvaluateTest(out string stdout, out string stderr, out int? exitCode)
        {
            stdout = "";
            stderr = "";
            exitCode = null;

            try
            {
                exitCode = runner.RunTest(out stdout, out stderr);
                return validator.ValidateResult(stdout, stderr, exitCode);
            }
            catch (TestRunException e)
            {
                if (!validator.ValidateException(e))
                {
                    throw;
                }

                return true;
            }
        }
    }

    public class TestCaseFactory
    {
        private readonly DirectoryInfo testTempBaseDir;

        public TestCaseFactory(DirectoryInfo testTempBaseDir)
        {
            this.testTempBaseDir = testTempBaseDir;
        }

        public CompilerTestCase CreateTestCase(DirectoryInfo testDir, Dictionary<TestType, TestConfig> testConfigs)
        {
            // eg. RegressionTests/F1/Correct/TestCaseName
            var inputFiles = testDir.GetFiles("*.p");
            string testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar)
                              .MakeRelativeUri(new Uri(testDir.FullName))
                              .ToString();

            ICompilerTestRunner runner;
            ITestResultsValidator validator;

            if (!testConfigs.ContainsKey(TestType.Prt))
            {
                runner = new TranslationRunner(inputFiles);

                // TODO: validate information about the particular kind of compiler error
                bool isStaticError = testName.Contains("/StaticError/");
                validator = isStaticError ? (ITestResultsValidator) new StaticErrorValidator() : new CompileSuccessValidator();
            }
            else
            {
                DirectoryInfo tempDirName = Directory.CreateDirectory(Path.Combine(testTempBaseDir.FullName, testName));
                runner = new ExecutionRunner(tempDirName, inputFiles);

                bool isCorrectTest = testName.Contains("/Correct/");
                validator = isCorrectTest
                    ? new ExecutionOutputValidator(exitCode => exitCode == 0, "", "")
                    : new ExecutionOutputValidator(exitCode => exitCode != 0, "", "");
            }

            return new CompilerTestCase(runner, validator);
        }
    }

    public class ExecutionOutputValidator : ITestResultsValidator
    {
        private readonly string expectedStderr;
        private readonly string expectedStdout;
        private readonly Func<int?, bool> isGoodExitCode;

        public ExecutionOutputValidator(Func<int?, bool> isGoodExitCode, string expectedStdout, string expectedStderr)
        {
            this.isGoodExitCode = isGoodExitCode;
            this.expectedStdout = expectedStdout;
            this.expectedStderr = expectedStderr;
        }

        public bool ValidateResult(string stdout, string stderr, int? exitCode)
        {
            return isGoodExitCode(exitCode);
        }

        public bool ValidateException(TestRunException testRunException)
        {
            return false;
        }
    }

    public class CompileSuccessValidator : ITestResultsValidator
    {
        public bool ValidateResult(string stdout, string stderr, int? exitCode)
        {
            return exitCode == 0;
        }

        public bool ValidateException(TestRunException testRunException)
        {
            return false;
        }
    }

    internal class TestExecutionStream : ICompilerOutput
    {
        private readonly DirectoryInfo outputDirectory;
        private readonly List<FileInfo> outputFiles = new List<FileInfo>();

        public TestExecutionStream(DirectoryInfo outputDirectory)
        {
            this.outputDirectory = outputDirectory;
        }

        public void WriteMessage(string msg, SeverityKind severity)
        {
            Console.WriteLine(msg);
        }

        public void WriteFile(CompiledFile file)
        {
            string fileName = Path.Combine(outputDirectory.FullName, file.FileName);
            File.WriteAllText(fileName, file.Contents);
            outputFiles.Add(new FileInfo(fileName));
        }
    }

    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class PSharpTests
    {
        private static IEnumerable<TestCaseData> TestCases =>
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);

        private static void AssertTestCase(CompilerTestCase testCase)
        {
            if (!testCase.EvaluateTest(out string stdout, out string stderr, out var exitCode))
            {
                Console.WriteLine("Test failed!\n");
                WriteOutput(stdout, stderr, exitCode);
                Assert.Fail(stderr);
            }

            Console.WriteLine("Test succeeded!\n");
            WriteOutput(stdout, stderr, exitCode);
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

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void TestAllRegressions(DirectoryInfo testDir, Dictionary<TestType, TestConfig> testConfigs)
        {
            DirectoryInfo tempDir = Directory.CreateDirectory(Path.Combine(Constants.TestDirectory, "temp_builds"));
            var factory = new TestCaseFactory(tempDir);

            CompilerTestCase testCase = factory.CreateTestCase(testDir, testConfigs);
            AssertTestCase(testCase);
        }

        [Test]
        public void TestTemp()
        {
            DirectoryInfo tempDir = Directory.CreateDirectory(Path.Combine(Constants.TestDirectory, "temp_builds", "TestTemp"));
            string path = Path.Combine(Constants.SolutionDirectory, "tmp", "fun.p");
            FileInfo[] inputFiles = {new FileInfo(path)};

            var testCase = new CompilerTestCase(new ExecutionRunner(tempDir, inputFiles),
                                                new ExecutionOutputValidator(code => code == 0, "", ""));

            AssertTestCase(testCase);
        }
    }
}
