using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Pc;
using Microsoft.Pc.Backend;
using NUnit.Framework;
using UnitTestsCore;

namespace UnitTests
{
    public class PrtTestRunner
    {
        private readonly DirectoryInfo prtTestProjDirectory;
        private readonly List<FileInfo> sources;

        public PrtTestRunner(List<FileInfo> sources)
        {
            this.sources = sources;
            prtTestProjDirectory = new DirectoryInfo(Path.Combine(Constants.TestDirectory, Constants.CRuntimeTesterDirectoryName));
        }

        public int? RunTest(IEnumerable<string> arguments, out string testStdout, out string testStderr,
                            TestRunMode mode = TestRunMode.CompileAndRun)
        {
            // TODO: do it right, Alex. Only delete tmpDir when the test passed. SOR!
            string tmpDir = null;
            testStdout = null;
            testStderr = null;
            try
            {
                tmpDir = CreateTemporaryDirectory();
                FileHelper.CopyFiles(prtTestProjDirectory, tmpDir);
                foreach (FileInfo source in sources)
                {
                    File.Copy(source.FullName, Path.Combine(tmpDir, source.Name));
                }

                if (!RunMSBuildExe(tmpDir, out testStdout))
                {
                    return null;
                }

                if (mode == TestRunMode.CompileAndRun)
                {
                    return ProcessHelper.RunWithOutput(
                        Path.Combine(tmpDir, Constants.BuildConfiguration, Constants.Platform, Constants.CTesterExecutableName),
                        tmpDir,
                        arguments,
                        out testStdout,
                        out testStderr
                    );
                }
                else
                {
                    return 0;
                }
            }
            finally
            {
                //Directory.Delete(tmpDir, true);
            }
        }

        private static bool RunMSBuildExe(string tmpDir, out string output)
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

        private static bool RunMSBuild(string tmpDir, out string output)
        {
            var pc = new ProjectCollection();

            var properties = new Dictionary<string, string>
            {
                {"Configuration", Constants.BuildConfiguration},
                {"Platform", Constants.Platform}
            };

            string projectFullPath = Path.Combine(tmpDir, Constants.CTesterVsProjectName);
            var build = new BuildRequestData(projectFullPath, properties, null, new[] {"Build"}, null);
            var buildParameters = new BuildParameters(pc)
            {
                Loggers = new[] {new ConsoleLogger(LoggerVerbosity.Diagnostic)}
            };
            BuildResult result = BuildManager.DefaultBuildManager.Build(buildParameters, build);
            output = result.Exception?.Message ?? "";
            bool buildSuccess = result.OverallResult == BuildResultCode.Success;
            return buildSuccess;
        }

        private static string CreateTemporaryDirectory()
        {
            string tmpDir = Path.Combine(Constants.TestDirectory, "temp_builds", Path.GetRandomFileName());
            var numTries = 10;
            while (Directory.Exists(tmpDir) && numTries > 0)
            {
                tmpDir = Path.Combine(Constants.TestDirectory, Path.GetRandomFileName());
                numTries--;
            }

            if (Directory.Exists(tmpDir))
            {
                throw new Exception("Could not create unique temporary directory!");
            }

            Directory.CreateDirectory(tmpDir);
            return tmpDir;
        }
    }

    internal class TestCompilerStream : ICompilerOutput
    {
        private readonly DirectoryInfo outputDirectory;
        private readonly List<FileInfo> outputFiles = new List<FileInfo>();

        public TestCompilerStream(DirectoryInfo outputDirectory)
        {
            this.outputDirectory = outputDirectory;
        }

        public IEnumerable<FileInfo> OutputFiles => outputFiles;

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

    public enum TestRunMode
    {
        CompileOnly,
        CompileAndRun
    }

    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class PSharpTests
    {
        private static IEnumerable<TestCaseData> TestCases =>
            TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);

        private static bool TestCompile(out string output, params FileInfo[] inputFiles)
        {
            var compiler = new AntlrCompiler();
            var compilerOutput = new StringWriter();
            var outputStream = new CompilerOutputStream(compilerOutput);
            bool success = compiler.Compile(outputStream, new CommandLineOptions
            {
                compilerOutput = CompilerOutput.C,
                inputFileNames = inputFiles.Select(file => file.FullName).ToList()
            });
            output = compilerOutput.ToString().Trim();
            return success;
        }


        private static int? ExecuteTest(out string output, IReadOnlyList<FileInfo> inputFiles, TestRunMode mode = TestRunMode.CompileAndRun)
        {
            output = null;

            var compiler = new AntlrCompiler();
            var outputStream = new TestCompilerStream(inputFiles[0].Directory);
            bool success = compiler.Compile(outputStream, new CommandLineOptions
            {
                compilerOutput = CompilerOutput.C,
                inputFileNames = inputFiles.Select(file => file.FullName).ToList(),
                projectName = "main"
            });

            if (!success)
            {
                return null;
            }

            var runner = new PrtTestRunner(outputStream.OutputFiles.ToList());
            var result = runner.RunTest(Enumerable.Empty<string>(), out string stdout, out string stderr, mode);
            output = $"{stdout}\n{stderr}\nEXIT: {result}";
            return result;
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void TestAllRegressions(DirectoryInfo testDir, Dictionary<TestType, TestConfig> testConfigs)
        {
            string testName = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar)
                              .MakeRelativeUri(new Uri(testDir.FullName))
                              .ToString();

            var inputFiles = testDir.GetFiles("*.p");
            int? result;
            string output;
            if (testDir.EnumerateDirectories("Prt").Any())
            {
                result = ExecuteTest(out output, inputFiles);
            }
            else
            {
                result = ExecuteTest(out output, inputFiles, TestRunMode.CompileOnly);
            }

            string fileList = string.Join("\n\t", inputFiles.Select(fi => $"file: {fi.FullName}"));

            if (testName.Contains("Correct") && result != 0)
            {
                string reason = result == null ? "failed to compile" : $"exited with code {result}";
                Assert.Fail($"Expected correct but {reason}. Output:\n{output}\n\t{fileList}\n");
            }
            else if (testName.Contains("DynamicError") && (result == 0 || result == null))
            {
                string reason = result == 0 ? "test passed without crashing" : "test failed to compile";
                Assert.Fail($"Expected runtime error but {reason}. Output:\n{output}\n\t{fileList}\n");
            }
            else if (testName.Contains("StaticError") && result != null)
            {
                Assert.Fail($"Expected static error but typechecker failed to catch! Output:\n{output}\n\t{fileList}\n");
            }

            Console.WriteLine(output);
        }

        [Test]
        [Ignore("broken test")]
        public void TestModuleSystem()
        {
            string path = Path.Combine(Constants.SolutionDirectory, "Tst", "RegressionTests", "Feature5ModuleSystem", "Correct", "Elevator",
                                       "Elevator.p");
            FileInfo[] inputFiles = {new FileInfo(path)};
            bool result = TestCompile(out string output, inputFiles);
            string fileList = string.Join("\n\t", inputFiles.Select(fi => $"file: {fi.FullName}"));
            if (!result)
            {
                Assert.Fail($"Expected correct, but error was found: {output}\n\t{fileList}\n");
            }

            Console.WriteLine(output);
        }

        [Test]
        public void TestTemp()
        {
            //string path = Path.Combine(Constants.TestDirectory, "RegressionTests", "Integration", "Correct", "SEM_TwoMachines_7", "RaisedHalt_bugFound.p");
            string path = Path.Combine(Constants.SolutionDirectory, "tmp", "fun.p");
            FileInfo[] inputFiles = {new FileInfo(path)};
            var result = ExecuteTest(out string output, inputFiles);
            string fileList = string.Join("\n\t", inputFiles.Select(fi => $"file: {fi.FullName}"));
            if (result != 0)
            {
                Assert.Fail($"Expected correct, but error was found: {output}\n\t{fileList}\n");
            }

            Console.WriteLine(output);
        }
    }
}
