using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
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
            foreach (FileInfo file in src.GetFiles())
            {
                File.Copy(file.FullName, Path.Combine(target, file.Name), true);
            }

            foreach (DirectoryInfo dir in src.GetDirectories())
            {
                DeepCopy(dir, Path.Combine(target, dir.Name));
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

            // Print handy header
            tmpWriter.WriteLine("=================================");
            tmpWriter.WriteLine("         Console output          ");
            tmpWriter.WriteLine("=================================");

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

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public bool TestProgramAndBackends(DirectoryInfo origTestDir, Dictionary<TestType, TestConfig> testConfigs)
        {
            // First step: clone test folder to new spot
            DirectoryInfo workDirectory = PrepareTestDir(origTestDir);

            foreach (KeyValuePair<TestType, TestConfig> kv in testConfigs.OrderBy(kv => kv.Key))
            {
                TestConfig config = kv.Value;
                Debug.WriteLine($"*********** Checking {config.Description} ***********");

                string activeDirectory = Path.Combine(workDirectory.FullName, kv.Key.ToString());
                foreach (string toDelete in config.Deletes)
                {
                    var fileToDelete = new FileInfo(Path.Combine(activeDirectory, toDelete));
                    if (fileToDelete.Exists)
                    {
                        fileToDelete.Delete();
                    }
                }

                var sb = new StringBuilder();
                using (var tmpWriter = new StringWriter(sb))
                {
                    try
                    {
                        switch (kv.Key)
                        {
                            case TestType.Pc:
                                TestPc(config, tmpWriter, workDirectory, activeDirectory);
                                break;
                            case TestType.Prt:
                                return true;
                            case TestType.Zing:
                                return true;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("************** CAUGHT EXCEPTION **************");
                        Console.WriteLine(e);
                        Console.WriteLine();
                    }
                }

                string correctText = File.ReadAllText(Path.Combine(activeDirectory, "acc_0.txt"));
                string actualText = sb.ToString();
                if (!correctText.Equals(actualText))
                {
                    Console.WriteLine("************** INCORRECT OUTPUT FOLLOWS **************");
                    Console.WriteLine(actualText);
                    Console.WriteLine();
                    Console.WriteLine("************** EXPECTED OUTPUT FOLLOWS **************");
                    Console.WriteLine(correctText);
                    Console.WriteLine();
                    return false;
                }

                Console.Write(actualText);
            }

            return true;
        }
    }
}