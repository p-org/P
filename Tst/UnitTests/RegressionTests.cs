using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Pc;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    //TODO: Why can't we run the compiler in parallel?
    //[Parallelizable(ParallelScope.Children)]
    public class RegressionTests
    {
        private readonly Lazy<Compiler> compiler = new Lazy<Compiler>(() =>
        {
            var compiler = new Compiler(true);
            var xmlProfiler = new XmlProfiler();
            compiler.Profiler = xmlProfiler;
            xmlProfiler.Data.Save(Path.Combine(Environment.CurrentDirectory, "TestProfile.xml"));
            return compiler;
        });

        private static readonly Lazy<string> SolutionDirectory = new Lazy<string>(() =>
        {
            string assemblyPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            string assemblyDirectory = Path.GetDirectoryName(assemblyPath);
            Contract.Assert(assemblyDirectory != null);
            for (var dir = new DirectoryInfo(assemblyDirectory); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "P.sln")))
                {
                    return dir.FullName;
                }
            }
            throw new FileNotFoundException();
        });

        private static string TestDirectory => Path.Combine(SolutionDirectory.Value, "Tst");

        private static Lazy<string> TestResultsDirectory { get; } =
            new Lazy<string>(() => Path.Combine(TestDirectory, $"TestResult_{BuildSettings.Configuration}_{BuildSettings.Platform}"));

        public static IEnumerable<TestCaseData> TestCases => TestCaseLoader.FindTestCasesInDirectory(TestDirectory);

        private static DirectoryInfo PrepareTestDir(DirectoryInfo testDir)
        {
            var testRoot = new Uri(TestDirectory + Path.DirectorySeparatorChar);
            var curTest = new Uri(testDir.FullName);
            Uri relativePath = testRoot.MakeRelativeUri(curTest);
            string destinationDir = Path.GetFullPath(Path.Combine(TestResultsDirectory.Value, relativePath.OriginalString));
            DeepCopy(testDir, destinationDir);
            return new DirectoryInfo(destinationDir);
        }

        private static void DeepCopy(DirectoryInfo src, string target)
        {
            Directory.CreateDirectory(target);
            CopyFiles(src, target);
            foreach (DirectoryInfo dir in src.GetDirectories())
            {
                DeepCopy(dir, Path.Combine(target, dir.Name));
            }
        }

        private static void CopyFiles(DirectoryInfo src, string target)
        {
            foreach (FileInfo file in src.GetFiles())
            {
                File.Copy(file.FullName, Path.Combine(target, file.Name), true);
            }
        }

        private Compiler PCompiler => compiler.Value;

        private void TestPc(TestConfig config, TextWriter tmpWriter, DirectoryInfo workDirectory, string activeDirectory)
        {
            // Clean up any remaining generated files
            foreach (FileInfo file in workDirectory.EnumerateFiles())
            {
                if (file.Extension == ".c" || file.Extension == ".h" || file.Extension == ".4ml" || file.Extension == ".zing" ||
                    file.Extension == ".dll")
                {
                    file.Delete();
                }
            }

            WriteHeader(tmpWriter);

            List<string> pFiles = workDirectory.EnumerateFiles("*.p").Select(pFile => pFile.FullName).ToList();
            if (!pFiles.Any())
            {
                throw new Exception("no .p file found in test directory");
            }

            string inputFileName = pFiles.First();
            string linkFileName = Path.ChangeExtension(inputFileName, ".4ml");

            var compilerOutput = new CompilerTestOutputStream(tmpWriter);
            var compileArgs = new CommandLineOptions
            {
                inputFileNames = new List<string>(pFiles),
                shortFileNames = true,
                outputDir = workDirectory.FullName,
                unitName = linkFileName,
                liveness = LivenessOption.None,
                compilerOutput = CompilerOutput.C
            };

            using (PCompiler.Profiler.Start("compile and link", inputFileName))
            {
                // Compile
                if (!PCompiler.Compile(compilerOutput, compileArgs))
                {
                    tmpWriter.WriteLine("EXIT: -1");
                    return;
                }

                // Link
                compileArgs.dependencies.Add(linkFileName);
                compileArgs.inputFileNames.Clear();

                if (config.Link != null)
                {
                    compileArgs.inputFileNames.Add(Path.Combine(activeDirectory, config.Link));
                }

                if (!PCompiler.Link(compilerOutput, compileArgs))
                {
                    tmpWriter.WriteLine("EXIT: -1");
                    return;
                }
            }

            // compile *.p again, this time with Zing option.
            compileArgs.inputFileNames = new List<string>(pFiles);
            compileArgs.dependencies.Clear();
            compileArgs.compilerOutput = CompilerOutput.Zing;
            if (config.Arguments.Contains("/liveness"))
            {
                compileArgs.liveness = LivenessOption.Standard;
            }
            using (PCompiler.Profiler.Start("compile zing", inputFileName))
            {
                if (!PCompiler.Compile(compilerOutput, compileArgs))
                {
                    tmpWriter.WriteLine("EXIT: -1");
                    return;
                }
            }

            tmpWriter.WriteLine("EXIT: 0");
        }

        private static void WriteHeader(TextWriter tmpWriter)
        {
            tmpWriter.WriteLine("=================================");
            tmpWriter.WriteLine("         Console output          ");
            tmpWriter.WriteLine("=================================");
        }

        private void TestZing(TestConfig config, StringWriter tmpWriter, DirectoryInfo workDirectory, string activeDirectory)
        {
            Debug.WriteLine("TODO: Zing not implemented");
        }

        private void TestPrt(TestConfig config, StringWriter tmpWriter, DirectoryInfo workDirectory, string activeDirectory)
        {
            // copy PrtTester to the work directory
            var testerDir = new DirectoryInfo(Path.Combine(TestDirectory, "PrtTester"));
            CopyFiles(testerDir, workDirectory.FullName);

            string testerExeDir = Path.Combine(workDirectory.FullName, BuildSettings.Configuration, BuildSettings.Platform);
            string testerExePath = Path.Combine(testerExeDir, "tester.exe");
            string prtTesterProj = Path.Combine(workDirectory.FullName, "Tester.vcxproj");

            // build the Pc output with the test harness
            using (PCompiler.Profiler.Start("build prttester", workDirectory.FullName))
            {
                BuildTester(prtTesterProj, activeDirectory, true);
                BuildTester(prtTesterProj, activeDirectory, false);
            }

            // run the harness
            using (PCompiler.Profiler.Start("run prttester", workDirectory.FullName))
            {
                var psi = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = activeDirectory,
                    Arguments = string.Join(" ", config.Arguments),
                    FileName = testerExePath
                };

                WriteHeader(tmpWriter);

                string stdout, stderr;
                int exitCode = RunWithOutput(psi, out stdout, out stderr);
                tmpWriter.Write(stdout);
                tmpWriter.Write(stderr);
                tmpWriter.WriteLine($"EXIT: {exitCode}");
            }
        }

        private void BuildTester(string prtTesterProj, string activeDirectory, bool clean)
        {
            var startInfo = new ProcessStartInfo("msbuild.exe")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WorkingDirectory = activeDirectory,
                Arguments = string.Join(" ", prtTesterProj, clean ? "/t:Clean" : "/t:Build",
                                        $"/p:Configuration={BuildSettings.Configuration}", $"/p:Platform={BuildSettings.Platform}",
                                        "/nologo")
            };

            string stdout, stderr;
            if (RunWithOutput(startInfo, out stdout, out stderr) != 0)
            {
                throw new Exception($"Failed to build {prtTesterProj}\nOutput:\n{stdout}\n\nErrors:\n{stderr}\n");
            }
        }

        private static int RunWithOutput(ProcessStartInfo startInfo, out string stdout, out string stderr)
        {
            string m_stdout = "", m_stderr = "";

            var proc = new Process {StartInfo = startInfo};
            proc.OutputDataReceived += (s, e) =>
            {
                string line = $"OUT: {e.Data}\n";
                Debug.WriteLine(line);
                m_stdout += line;
            };

            proc.ErrorDataReceived += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                }
                string line = $"ERROR: {e.Data}\n";
                Debug.WriteLine(line);
                m_stderr += line;
            };

            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            stdout = m_stdout;
            stderr = m_stderr;
            return proc.ExitCode;
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void TestProgramAndBackends(DirectoryInfo origTestDir, Dictionary<TestType, TestConfig> testConfigs)
        {
            // First step: clone test folder to new spot
            DirectoryInfo workDirectory = PrepareTestDir(origTestDir);

            foreach (KeyValuePair<TestType, TestConfig> kv in testConfigs.OrderBy(kv => kv.Key))
            {
                TestType testType = kv.Key;
                TestConfig config = kv.Value;

                Debug.WriteLine($"*** {config.Description}");

                string activeDirectory = Path.Combine(workDirectory.FullName, testType.ToString());

                // Delete temp files as specified by test configuration.
                IEnumerable<FileInfo> toDelete = config
                    .Deletes.Select(file => new FileInfo(Path.Combine(activeDirectory, file))).Where(file => file.Exists);
                foreach (FileInfo fileInfo in toDelete)
                {
                    fileInfo.Delete();
                }

                var sb = new StringBuilder();
                using (var tmpWriter = new StringWriter(sb))
                {
                    switch (testType)
                    {
                        case TestType.Pc:
                            TestPc(config, tmpWriter, workDirectory, activeDirectory);
                            break;
                        case TestType.Prt:
                            TestPrt(config, tmpWriter, workDirectory, activeDirectory);
                            break;
                        case TestType.Zing:
                            return;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                string correctText = File.ReadAllText(Path.Combine(activeDirectory, "acc_0.txt"));
                correctText = Regex.Replace(correctText, @"\r\n|\n\r|\n|\r", "\n");
                string actualText = sb.ToString();
                actualText = Regex.Replace(actualText, @"\r\n|\n\r|\n|\r", "\n");
                Assert.AreEqual(correctText, actualText);
                Debug.WriteLine(actualText);
            }
        }
    }
}