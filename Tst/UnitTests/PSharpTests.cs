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

        public int? RunTest(IEnumerable<string> arguments, out string testStdout, out string testStderr)
        {
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

                return ProcessHelper.RunWithOutput(
                    Path.Combine(tmpDir, Constants.BuildConfiguration, Constants.Platform, Constants.CTesterExecutableName),
                    tmpDir,
                    arguments,
                    out testStdout,
                    out testStderr
                );
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

        private static bool ExecuteTest(out string output, params FileInfo[] inputFiles)
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
                return false;
            }

            var runner = new PrtTestRunner(outputStream.OutputFiles.ToList());
            var result = runner.RunTest(Enumerable.Empty<string>(), out string stdout, out string stderr);
            output = $"{stdout}\n{stderr}\nEXIT: {result}";
            return result != null;
        }

        [Test]
        public void DetectUnsplitTests()
        {
            var totalUnsplitTests = 0;
            var exceptions = new HashSet<string>
            {
                "RegressionTests/Combined/StaticError/DuplicateActions",
                "RegressionTests/Combined/StaticError/DuplicateTransitions",
                "RegressionTests/Feature1SMLevelDecls/StaticError/DeferIgnoreSameEvent",
                "RegressionTests/Feature1SMLevelDecls/StaticError/EventDeferredDoSameState",
                "RegressionTests/Feature1SMLevelDecls/StaticError/EventDeferredHandledSameState",
                "RegressionTests/Feature1SMLevelDecls/StaticError/FunctionMissingArgs",
                "RegressionTests/Feature1SMLevelDecls/StaticError/FunctionReturnsNothingInAssignment",
                "RegressionTests/Feature1SMLevelDecls/StaticError/RaisedNullEvent",
                "RegressionTests/Feature1SMLevelDecls/StaticError/SentNullEvent",
                "RegressionTests/Feature4DataTypes/StaticError/EventSets_1",
                "RegressionTests/Feature4DataTypes/StaticError/EventSets_2",
                "RegressionTests/Feature4DataTypes/StaticError/EventSets_3",
                "RegressionTests/Feature4DataTypes/StaticError/EventSets_4",
                "RegressionTests/Feature4DataTypes/StaticError/EventSets_5",
                "RegressionTests/Feature4DataTypes/StaticError/typedef"
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
                    var lines = File.ReadAllLines(Path.Combine(testDir.FullName, "Pc", "acc_0.txt"));
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
            var inputFiles = testDir.GetFiles("*.p");
            bool result = ExecuteTest(out string output, inputFiles);
            string fileList = string.Join("\n\t", inputFiles.Select(fi => $"file: {fi.FullName}"));
            if (expectCorrect && !result)
            {
                Assert.Fail($"Expected correct, but error was found: {output}\n\t{fileList}\n");
            }
            else if (!expectCorrect && result)
            {
                Assert.Fail($"Expected error, but none were found!\n\t{fileList}\n");
            }

            Console.WriteLine(output);
        }

        [Test, Ignore("broken test")]
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
            bool result = ExecuteTest(out string output, inputFiles);
            string fileList = string.Join("\n\t", inputFiles.Select(fi => $"file: {fi.FullName}"));
            if (!result)
            {
                Assert.Fail($"Expected correct, but error was found: {output}\n\t{fileList}\n");
            }

            Console.WriteLine(output);
        }
    }
}
