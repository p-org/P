using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Pc;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class RegressionTests
    {
        private Compiler PCompiler => compiler.Value;

        private readonly ThreadLocal<Compiler> compiler = new ThreadLocal<Compiler>(() => new Compiler(true));

        private static string TestResultsDirectory { get; } = Path.Combine(
            Constants.TestDirectory,
            $"TestResult_{Constants.Configuration}_{Constants.Platform}");

        public static IEnumerable<TestCaseData> TestCases => TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);

        private static DirectoryInfo PrepareTestDir(DirectoryInfo testDir)
        {
            var testRoot = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar);
            var curTest = new Uri(testDir.FullName);
            Uri relativePath = testRoot.MakeRelativeUri(curTest);
            string destinationDir = Path.GetFullPath(Path.Combine(TestResultsDirectory, relativePath.OriginalString));
            if (Directory.Exists(destinationDir))
            {
                Directory.Delete(destinationDir, true);
            }
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

        private static void WriteError(string format, params object[] args)
        {
            ConsoleColor saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
            Console.ForegroundColor = saved;
        }

        private static bool OpenSummaryStreamWriter(string fileName, out StreamWriter wr)
        {
            wr = null;
            try
            {
                wr = new StreamWriter(Path.Combine(Constants.TestDirectory, fileName));
            }
            catch (Exception e)
            {
                WriteError("ERROR: Could not open summary file {0} - {1}", fileName, e.Message);
                return false;
            }

            return true;
        }

        private static bool CloseSummaryStreamWriter(string fileName, StreamWriter wr)
        {
            try
            {
                wr.Close();
            }
            catch (Exception e)
            {
                WriteError("ERROR: Could not close summary file {0} - {1}", fileName, e.Message);
                return false;
            }

            return true;
        }

        private void SafeDelete(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private void TestPc(TestConfig config, TextWriter tmpWriter, DirectoryInfo workDirectory, string activeDirectory)
        {
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

            // compile *.p again, this time with Zing option.
            compileArgs.inputFileNames = new List<string>(pFiles);
            compileArgs.dependencies.Clear();
            compileArgs.compilerOutput = CompilerOutput.Zing;
            if (config.Arguments.Contains("/liveness"))
            {
                compileArgs.liveness = LivenessOption.Standard;
            }

            int zingResult = PCompiler.Compile(compilerOutput, compileArgs) ? 0 : -1;
            tmpWriter.WriteLine($"EXIT: {zingResult}");
        }

        private static void WriteHeader(TextWriter tmpWriter)
        {
            tmpWriter.WriteLine("=================================");
            tmpWriter.WriteLine("         Console output          ");
            tmpWriter.WriteLine("=================================");
        }

        private static void TestPt(TestConfig config, TextWriter tmpWriter, DirectoryInfo workDirectory, string activeDirectory) { }

        private static void TestZing(TestConfig config, TextWriter tmpWriter, DirectoryInfo workDirectory, string activeDirectory)
        {
            // Find Zing tool
            string zingFilePath = Path.Combine(
                Constants.SolutionDirectory,
                "Bld",
                "Drops",
                Constants.Configuration,
                Constants.Platform,
                "Binaries",
                "zinger.exe");

            // Find DLL input to Zing
            string zingDllName = (from fileName in workDirectory.EnumerateFiles()
                                  where fileName.Extension == ".dll" && !fileName.Name.Contains("linker")
                                  select fileName.FullName).FirstOrDefault();
            if (zingDllName == null)
            {
                throw new Exception("Could not find Zinger input.");
            }

            // Run Zing tool
            var arguments = new List<string>(config.Arguments) {zingDllName};
            string stdout, stderr;
            int exitCode = RunWithOutput(zingFilePath, activeDirectory, arguments, out stdout, out stderr);
            tmpWriter.Write(stdout);
            tmpWriter.Write(stderr);
            tmpWriter.WriteLine($"EXIT: {exitCode}");

            // Append includes
            foreach (string include in config.Includes)
            {
                tmpWriter.WriteLine();
                tmpWriter.WriteLine("=================================");
                tmpWriter.WriteLine(include);
                tmpWriter.WriteLine("=================================");

                try
                {
                    using (var sr = new StreamReader(Path.Combine(activeDirectory, include)))
                    {
                        while (!sr.EndOfStream)
                        {
                            tmpWriter.WriteLine(sr.ReadLine());
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    if (!include.EndsWith("trace"))
                    {
                        throw;
                    }
                }
            }
        }

        private void TestPrt(TestConfig config, TextWriter tmpWriter, DirectoryInfo workDirectory, string activeDirectory)
        {
            // copy PrtTester to the work directory
            var testerDir = new DirectoryInfo(Path.Combine(Constants.TestDirectory, Constants.CRuntimeTesterDirectoryName));
            CopyFiles(testerDir, workDirectory.FullName);

            string testerExeDir = Path.Combine(workDirectory.FullName, Constants.Configuration, Constants.Platform);
            string testerExePath = Path.Combine(testerExeDir, Constants.CTesterExecutableName);
            string prtTesterProj = Path.Combine(workDirectory.FullName, Constants.CTesterVsProjectName);

            // build the Pc output with the test harness

            BuildTester(prtTesterProj, activeDirectory, true);
            BuildTester(prtTesterProj, activeDirectory, false);

            // run the harness

            string stdout, stderr;
            int exitCode = RunWithOutput(testerExePath, activeDirectory, config.Arguments, out stdout, out stderr);
            tmpWriter.Write(stdout);
            tmpWriter.Write(stderr);
            tmpWriter.WriteLine($"EXIT: {exitCode}");
        }

        private static void BuildTester(string prtTesterProj, string activeDirectory, bool clean)
        {
            var argumentList = new[]
            {
                prtTesterProj, clean ? "/t:Clean" : "/t:Build", $"/p:Configuration={Constants.Configuration}",
                $"/p:Platform={Constants.Platform}", "/nologo"
            };

            string stdout, stderr;
            if (RunWithOutput("msbuild.exe", activeDirectory, argumentList, out stdout, out stderr) != 0)
            {
                throw new Exception($"Failed to build {prtTesterProj}\nOutput:\n{stdout}\n\nErrors:\n{stderr}\n");
            }
        }

        private static int RunWithOutput(
            string exeName,
            string activeDirectory,
            IEnumerable<string> argumentList,
            out string stdout,
            out string stderr)
        {
            var psi = new ProcessStartInfo(exeName)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WorkingDirectory = activeDirectory,
                Arguments = string.Join(" ", argumentList)
            };

            string mStdout = "", mStderr = "";

            var proc = new Process {StartInfo = psi};
            proc.OutputDataReceived += (s, e) => { mStdout += $"OUT: {e.Data}\n"; };
            proc.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    mStderr += $"ERROR: {e.Data}\n";
                }
            };

            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            stdout = mStdout;
            stderr = mStderr;
            return proc.ExitCode;
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void TestProgramAndBackends(DirectoryInfo origTestDir, Dictionary<TestType, TestConfig> testConfigs)
        {
            // First step: clone test folder to new spot
            DirectoryInfo workDirectory = PrepareTestDir(origTestDir);

            //TODO(after /reset option is implemented): opening of the diffing file
            //only happens when !reset
            SafeDelete(Path.Combine(Constants.TestDirectory, Constants.DisplayDiffsFile));
            StreamWriter displayDiffsWriter = null;
            if (!OpenSummaryStreamWriter(Constants.DisplayDiffsFile, out displayDiffsWriter))
            {
                throw new Exception("Cannot open display-diffs.bat for writing");
            }
            displayDiffsWriter.AutoFlush = true;
            var sbd = new StringBuilder();
            foreach (KeyValuePair<TestType, TestConfig> kv in testConfigs.OrderBy(kv => kv.Key))
            {
                TestType testType = kv.Key;
                TestConfig config = kv.Value;

                Console.WriteLine($"*** {config.Description}");

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
                    WriteHeader(tmpWriter);
                    switch (testType)
                    {
                        case TestType.Pc:
                            TestPc(config, tmpWriter, workDirectory, activeDirectory);
                            break;
                        case TestType.Prt:
                            TestPrt(config, tmpWriter, workDirectory, activeDirectory);
                            break;
                        case TestType.Pt:
                            TestPt(config, tmpWriter, workDirectory, activeDirectory);
                            break;
                        case TestType.Zing:
                            TestZing(config, tmpWriter, workDirectory, activeDirectory);
                            break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }

                /* TODO: Add test case freezing code here. 
                    * Check for a FREEZE_P_TESTS environment variable, and if present, overwrite the contents of
                    * Path.Combine(origTestDir.FullName, testType.ToString(), Constants.CorrectOutputFileName)
                    * with the value in actualText and, of course, skip the assertion.
                    */
                string correctOutputPath = Path.Combine(activeDirectory, Constants.CorrectOutputFileName);
                string correctText = File.ReadAllText(correctOutputPath);
                correctText = Regex.Replace(correctText, Constants.NewLinePattern, Environment.NewLine);
                string actualText = sb.ToString();
                actualText = Regex.Replace(actualText, Constants.NewLinePattern, Environment.NewLine);
                File.WriteAllText(Path.Combine(activeDirectory, Constants.ActualOutputFileName), actualText);
                if (!actualText.Equals(correctText))
                {
                    //add diffing command to "display-diffs.bat":
                    displayDiffsWriter.WriteLine("{0} {1}\\acc_0.txt {1}\\{2}", Constants.DiffTool,
                        activeDirectory, Constants.ActualOutputFileName);
                }

                Assert.AreEqual(correctText, actualText);
                Console.WriteLine(actualText);
            }
            if (!CloseSummaryStreamWriter(Constants.DisplayDiffsFile, displayDiffsWriter))
            {
                throw new Exception("Cannot close display-diffs.bat");
            }
        }
    }
}