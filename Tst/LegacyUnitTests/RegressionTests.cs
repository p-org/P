using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Pc;
using NUnit.Framework;
using UnitTestsCore;

namespace LegacyUnitTests
{

    [TestFixture]
    //Parallel execution is not working:
    [Parallelizable(ParallelScope.Children)]
    //[NonParallelizable]
    public class RegressionTests
    {
        private ThreadLocal<LegacyCompiler> PCompiler { get; } = new ThreadLocal<LegacyCompiler>(() => new LegacyCompiler(true));

        public static IEnumerable<TestCaseData> TestCases => TestCaseLoader.FindTestCasesInDirectory(Constants.TestDirectory);

        private static DirectoryInfo PrepareTestDir(DirectoryInfo testDir)
        {
            var testRoot = new Uri(Constants.TestDirectory + Path.DirectorySeparatorChar);
            var curTest = new Uri(testDir.FullName);
            Uri relativePath = testRoot.MakeRelativeUri(curTest);
            string destinationDir = Path.GetFullPath(Path.Combine(Constants.TestResultsDirectory, relativePath.OriginalString));
            //Why below is commented out:
            //Some tests failed to copy without any exception
            //Removing TestResult_Debug_x86 dir in FindTestCasesInDirectory instead
            //try
            //{
            //    if (Directory.Exists(destinationDir))
            //    {
            //        Directory.Delete(destinationDir, true);
            //    }
            //}
            //catch (Exception e)
            //{
            //    WriteError("ERROR: Could not delete old test directory: {0}", e.Message);
            //}

            FileHelper.DeepCopy(testDir, destinationDir);
            return new DirectoryInfo(destinationDir);
        }

        private int TestPc(
            TestConfig config,
            TextWriter tmpWriter,
            DirectoryInfo workDirectory,
            string activeDirectory,
            CompilerOutput outputLanguage)
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
                //liveness = LivenessOption.None,
                liveness = LivenessOption.None,
                compilerOutput = outputLanguage
            };

            // Compile
            if (!PCompiler.Value.Compile(compilerOutput, compileArgs))
            //if (!PCompilerService.Value.Compile(compilerOutput, compileArgs))
            {
                tmpWriter.WriteLine("EXIT: -1");
                return -1;
            }
          
            //(TODO)Skip the link step if outputLanguage == CompilerOutput.CSharp?
            // Link
            compileArgs.dependencies.Add(linkFileName);
            compileArgs.inputFileNames.Clear();

            if (config.Link != null)
            {
                compileArgs.inputFileNames.Add(Path.Combine(activeDirectory, config.Link));
            }

            if (!PCompiler.Value.Link(compilerOutput, compileArgs))
            //if(!PCompilerService.Value.Link(compilerOutput, compileArgs))
            {
                tmpWriter.WriteLine("EXIT: -1");
                return -1;
            }
            else
            {
                tmpWriter.WriteLine("EXIT: 0");
            }
            return 0;
        }

        private static void WriteHeader(TextWriter tmpWriter)
        {
            tmpWriter.WriteLine("=================================");
            tmpWriter.WriteLine("         Console output          ");
            tmpWriter.WriteLine("=================================");
        }

        private static void TestPt(
            TestConfig config,
            TextWriter tmpWriter,
            DirectoryInfo workDirectory,
            string activeDirectory,
            DirectoryInfo origTestDir)
        {
            //Run CSharp compiler on generated .cs:
            // % 1: workDirectory
            // % 2: (origTestDir (test name)
            //csc.exe "%1\%2.cs" "%1\linker.cs" /debug /target:library /r:"D:\PLanguage\P\Bld\Drops\Debug\x86\Binaries\Prt.dll" /out:"%1\%2.dll"
            //string cscFilePath = "csc.exe";

            string frameworkPath = RuntimeEnvironment.GetRuntimeDirectory();
            string cscFilePath = Path.Combine(frameworkPath, "csc.exe");
            if (!File.Exists(cscFilePath))
            {
                throw new Exception("Could not find csc.exe");
            }

            // Find .cs input to pt.exe:
            // pick up either .cs file with the name of origTestDir (if any), or
            // any .cs file in the workDirectory (but not linker.cs):
            string csFileName = null;
            foreach (FileInfo fileName in workDirectory.EnumerateFiles())
            {
                if (fileName.Extension == ".cs" && Path
                        .GetFileNameWithoutExtension(fileName.Name).ToLower().Equals(origTestDir.Name.ToLower())
                    || fileName.Extension == ".cs" && !Path.GetFileNameWithoutExtension(fileName.Name).Equals("linker"))
                {
                    csFileName = fileName.FullName;
                }
            }

            if (csFileName == null)
            {
                throw new Exception("Could not find .cs input for pt.exe");
            }

            // Find linker.cs:
            string linkerFileName = (from fileName1 in workDirectory.EnumerateFiles()
                                     where fileName1.Extension == ".cs" && Path.GetFileNameWithoutExtension(fileName1.Name).Equals("linker")
                                     select fileName1.FullName).FirstOrDefault();
            if (linkerFileName == null)
            {
                throw new Exception("Could not find linker.cs input for pt.exe");
            }

            // Find Prt.dll:
            string prtDllPath = Path.Combine(
                Constants.SolutionDirectory,
                "Bld",
                "Drops",
                Constants.BuildConfiguration,
                Constants.Platform,
                "Binaries",
                "Prt.dll");
            if (!File.Exists(prtDllPath))
            {
                throw new Exception("Could not find Prt.dll");
            }

            // Output DLL file name:
            string outputDllName = origTestDir.Name + ".dll";
            string outputDllPath = Path.Combine(workDirectory.FullName, outputDllName);

            //Delete generated files from previous PTester run:
            //<test>.cs,  <test>.dll, <test>.pdb, 
            foreach (FileInfo file in workDirectory.EnumerateFiles())
            {
                if (file.Name == origTestDir.Name && (file.Extension == ".dll" || file.Extension == ".pdb"))
                {
                    file.Delete();
                }
            }
            // Run C# compiler
            //IMPORTANT: since there's no way to suppress all warnings, if warnings other than specified below are detected, those would have to be added
            //Another option would be to not write csc.exe output into the acceptor at all
            //var arguments = new List<string>(config.Arguments) { "/debug", "/nowarn:1692,168,162", "/nologo", "/target:library", "/r:" + prtDLLPath, "/out:" + outputDLLPath, csFileName, linkerFileName };
            var arguments = new List<string>(config.Arguments)
            {
                "/debug",
                "/target:library",
                "/r:" + prtDllPath,
                "/out:" + outputDllPath,
                csFileName,
                linkerFileName
            };
            int exitCode = ProcessHelper.RunWithOutput(cscFilePath, activeDirectory, arguments, 
                                                       out string stdout, out string stderr);
            //tmpWriter.Write(stdout);
            //tmpWriter.Write(stderr);
            tmpWriter.WriteLine($"EXIT (csc.exe): {exitCode}");
            if (exitCode != 0)
            {
                throw new Exception("csc.exe failed");
            }

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

            //Run pt.exe: pt.exe "%1\%2.dll"
            // Find pt.exe:
            string ptExePath = Path.Combine(
                Constants.SolutionDirectory,
                "Bld",
                "Drops",
                Constants.BuildConfiguration,
                Constants.Platform,
                "Binaries",
                "Pt.exe");
            if (!File.Exists(ptExePath))
            {
                throw new Exception("Could not find pt.exe");
            }

            // input DLL file name: same as outputDLLPath

            // Run pt.exe
            if (Constants.PtWithPSharp)
            {
                arguments = new List<string>(config.Arguments) { "/psharp", outputDllPath };
            }
            else
            {
                arguments = new List<string>(config.Arguments) { outputDllPath };
            }
            int exitCode1 = ProcessHelper.RunWithOutput(ptExePath, activeDirectory, arguments, out stdout, out stderr);
            
            if (!(exitCode1 == 0))
            {
                //Only copy into accceptor the lines with error reporting:
                bool copy = false;
                using (StringReader sr = new StringReader(stdout))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("ERROR"))
                        {
                            copy = true;
                        }
                        if (copy)
                        {
                            tmpWriter.WriteLine(line);
                        }
                    }
                }
                tmpWriter.Write(stderr);
                tmpWriter.WriteLine($"EXIT: {exitCode1}");
            }
            else
            {
                tmpWriter.Write(stdout);
                tmpWriter.Write(stderr);
                tmpWriter.WriteLine($"EXIT: {exitCode1}");
            } 
        }


        private void TestPrt(TestConfig config, TextWriter tmpWriter, DirectoryInfo workDirectory, string activeDirectory)
        {
            // copy PrtTester to the work directory
            var testerDir = new DirectoryInfo(Path.Combine(Constants.TestDirectory, Constants.CRuntimeTesterDirectoryName));
            FileHelper.CopyFiles(testerDir, workDirectory.FullName);

            string testerExeDir = Path.Combine(workDirectory.FullName, Constants.BuildConfiguration, Constants.Platform);
            string testerExePath = Path.Combine(testerExeDir, Constants.CTesterExecutableName);
            //if (!File.Exists(testerExePath))
            //{
            //    throw new Exception("Could not find tester.exe");
            //}

            string prtTesterProj = Path.Combine(workDirectory.FullName, Constants.CTesterVsProjectName);

            // build the Pc output with the test harness

            BuildTester(prtTesterProj, activeDirectory, true);
            BuildTester(prtTesterProj, activeDirectory, false);

            // run the harness
            string stdout, stderr;
            int exitCode = ProcessHelper.RunWithOutput(testerExePath, activeDirectory, config.Arguments, out stdout, out stderr);
            tmpWriter.Write(stdout);
            tmpWriter.Write(stderr);
            tmpWriter.WriteLine($"EXIT: {exitCode}");
        }

        private static string FindTool(string name)
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            string[] dirs = path?.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
            return dirs.Select(dir => Path.Combine(dir, name)).FirstOrDefault(File.Exists);
        }

        private static void BuildTester(string prtTesterProj, string activeDirectory, bool clean)
        {
            var argumentList = new[]
            {
                prtTesterProj, clean ? "/t:Clean" : "/t:Build", $"/p:Configuration={Constants.BuildConfiguration}",
                $"/p:Platform={Constants.Platform}", "/nologo"
            };
            ////////////////////////////
            //1. Define msbuildPath for msbuild.exe:
            string msbuildPath = FindTool("MSBuild.exe");
            if (msbuildPath == null)
            {
                string programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                if (string.IsNullOrEmpty(programFiles))
                {
                    programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
                }
                msbuildPath = Path.Combine(programFiles, @"MSBuild\15.0\Bin\MSBuild.exe");
                if (!File.Exists(msbuildPath))
                {
                    throw new Exception("msbuild.exe is not in your PATH.");
                }
            }

            //////////////////////////
            string stdout, stderr;
            //if (!File.Exists("msbuild.exe"))
            //{
            //    throw new Exception("Could not find msbuild.exe");
            //}
            if (ProcessHelper.RunWithOutput(msbuildPath, activeDirectory, argumentList, out stdout, out stderr) != 0)
            {
                throw new Exception($"Failed to build {prtTesterProj}\nOutput:\n{stdout}\n\nErrors:\n{stderr}\n");
            }
        }

        public void CheckResult(string activeDirectory, DirectoryInfo origTestDir, TestType testType, StringBuilder sb, bool reset)
        {
            //var sb = new StringBuilder();
            string correctOutputPath = Path.Combine(activeDirectory, Constants.CorrectOutputFileName);
            string correctText = File.ReadAllText(correctOutputPath);
            correctText = Regex.Replace(correctText, Constants.NewLinePattern, Environment.NewLine);
            string actualText = sb.ToString();
            actualText = Regex.Replace(actualText, Constants.NewLinePattern, Environment.NewLine);
            if (!Constants.ResetTests)
            {
                if (!actualText.Equals(correctText))
                {
                    try
                    {
                        //Save actual test output:
                        File.WriteAllText(Path.Combine(activeDirectory, Constants.ActualOutputFileName), actualText);
                        //add diffing command to "display-diffs.bat":
                        string diffCmd = string.Format(
                            "{0} {1}\\acc_0.txt {1}\\{2}",
                            Constants.DiffTool,
                            activeDirectory,
                            Constants.ActualOutputFileName);
                        diffCmd = Environment.NewLine + diffCmd;
                        File.AppendAllText(Path.Combine(Constants.TestDirectory, Constants.DisplayDiffsFile), diffCmd);
                    }
                    catch (Exception e)
                    {
                        ConsoleHelper.WriteError("ERROR: exception: {0}", e.Message);
                    }
                }
                Assert.AreEqual(correctText, actualText);
            }
            else if (reset)
            {
                // if test type is the one for which reset is requested,
                // reset the acceptor (if any), or create a new one:
                File.WriteAllText(Path.Combine(origTestDir.FullName, testType.ToString(), Constants.CorrectOutputFileName), actualText);
            }

            Console.WriteLine(actualText);
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
                    int pcResult;
                    switch (testType)
                    {
                        case TestType.Pc:
                            WriteHeader(tmpWriter);
                            TestPc(config, tmpWriter, workDirectory, activeDirectory, CompilerOutput.C);
                            if (Constants.RunPc || Constants.RunAll)
                            {
                                CheckResult(activeDirectory, origTestDir, testType, sb, true);
                            }
                            else
                            {
                                CheckResult(activeDirectory, origTestDir, testType, sb, false);
                            }
                            break;
                        case TestType.Prt:
                            if (Constants.RunPrt || Constants.RunAll)
                            {
                                WriteHeader(tmpWriter);
                                TestPrt(config, tmpWriter, workDirectory, activeDirectory);
                                CheckResult(activeDirectory, origTestDir, testType, sb, true);
                            }
                            break;
                        case TestType.Pt:
                            if (Constants.RunPt || Constants.RunAll)
                            {
                                WriteHeader(tmpWriter);
                                pcResult = TestPc(config, tmpWriter, workDirectory, activeDirectory, CompilerOutput.CSharp);
                                //CheckResult(activeDirectory, origTestDir, testType, sb);
                                if (pcResult == 0)
                                {
                                    WriteHeader(tmpWriter);
                                    TestPt(config, tmpWriter, workDirectory, activeDirectory, origTestDir);
                                    CheckResult(activeDirectory, origTestDir, testType, sb, true);
                                }
                                else
                                {
                                    throw new Exception("TestPc failed");
                                }
                            }
                            break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}
