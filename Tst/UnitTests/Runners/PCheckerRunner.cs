using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using PChecker;
using PChecker.SystematicTesting;
using Plang;
using Plang.Compiler;
using UnitTests.Core;

namespace UnitTests.Runners
{
    internal class PCheckerRunner : ICompilerTestRunner
    {
        private static readonly object CheckerLock = new object();
        private static readonly object RunTestLock = new object();
        private readonly FileInfo[] nativeSources;
        private readonly FileInfo[] sources;
        private readonly string sourceDirectoryName;

        public PCheckerRunner(FileInfo[] sources)
        {
            this.sources = sources;
            nativeSources = new FileInfo[] { };
            this.sourceDirectoryName = GetSourceDirectoryName(sources);
        }

        public PCheckerRunner(FileInfo[] sources, FileInfo[] nativeSources)
        {
            this.sources = sources;
            this.nativeSources = nativeSources;
            this.sourceDirectoryName = GetSourceDirectoryName(sources);
        }

        private string GetSourceDirectoryName(FileInfo[] sources)
        {
            if (sources == null)
            {
                // Fallback to GUID if sources is null
                return $"Test_{Guid.NewGuid():N}";
            }
            var pFile = sources.FirstOrDefault(f => f.Extension == ".p");
            if (pFile?.Directory != null)
            {
                return pFile.Directory.Name;
            }
            // Fallback to GUID if no .p file found or no directory
            return $"Test_{Guid.NewGuid():N}";
        }

        private void FileCopy(string src, string target, bool overwrite)
        {
            var retries = 5;
            while (retries-- > 0)
            {
                try
                {
                    File.Copy(src, target, overwrite);
                    return;
                }
                catch (IOException)
                {
                    if (retries == 1)
                    {
                        throw;
                    }
                    Thread.Sleep(1000);
                }
            }
        }

        public int? RunTest(DirectoryInfo scratchDirectory, out string stdout, out string stderr)
        {
            stdout = "";
            stderr = "";

            DirectoryInfo scratchDirectoryGenerated = Directory.CreateDirectory(Path.Combine(scratchDirectory.FullName, "PChecker"));

            foreach (var nativeFile in nativeSources)
            {
                FileCopy(nativeFile.FullName, Path.Combine(scratchDirectoryGenerated.FullName, nativeFile.Name), true);
            }

            var exitCode = DoCompile(scratchDirectory);
            if (exitCode != 0)
            {
                return exitCode;
            }


            // Step 2: Parametric tests
            string dllPath = Path.Combine(scratchDirectoryGenerated.FullName, $"./net8.0/{sourceDirectoryName}.dll");
            var pFile = sources.FirstOrDefault(f => f.Extension == ".p");
            var sourceDirectory = pFile?.Directory;
            if (sourceDirectory.Name.ToLowerInvariant().Contains("param"))
            {
                var expectedFile = Path.Combine(sourceDirectory.FullName, "ExpectedParametricTests.txt");
                return ValidateParametricTestOutput(dllPath, expectedFile, out stdout, out stderr);
            }

            // Step 3: Check
            var config = CheckerConfiguration.Create()
                .WithTestingIterations(1000)
                .WithMaxSchedulingSteps(1000);

            exitCode = RunCheckerInProcess(config, dllPath, "DefaultImpl", out var testStdout, out var testStderr);
            stdout += testStdout;
            stderr += testStderr;
            return exitCode;

        }

        private int ValidateParametricTestOutput(string dllPath, string expectedFilePath, out string stdout, out string stderr)
        {
            stdout = string.Empty;
            stderr = string.Empty;

            if (!File.Exists(expectedFilePath))
            {
                stderr = $"ExpectedParametricTests.txt not found at: {expectedFilePath}.";
                return 1;
            }

            // Step 1: Run with --list-tests to get console output
            var listArgs = new[]
            {
                "check",
                dllPath,
                "--list-tests"
            };

            var exitCode = RunCommandLineWithArgs(listArgs, out var checkerOut, out var checkerErr);
            stdout += checkerOut;
            stderr += checkerErr;

            if (exitCode != 0)
            {
                stderr += "\nFailed to list parametric tests.";
                return exitCode;
            }

            // Step 2: Load expected lines (ignoring empty lines and comments)
            var expectedLines = File.ReadAllLines(expectedFilePath)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                .ToList();

            // Step 3: Verify each expected line appears somewhere in the checker output
            var missing = expectedLines
                .Where(expected => !checkerOut.Contains(expected))
                .ToList();

            if (missing.Any())
            {
                stderr += "\nSome expected lines were not found in the output:";
                stderr += $"\nMissing: {string.Join(", ", missing)}";
                return 1;
            }
            
            // If there is exactly one expected line, treat it as a test case name for further processing, else return here
            if (expectedLines.Count != 1)
            {
                return 0;
            }
            
            var config = CheckerConfiguration.Create()
                .WithTestingIterations(1000)
                .WithMaxSchedulingSteps(1000);
            config.TestCaseName = expectedLines[0];

            exitCode = RunCheckerInProcess(config, dllPath, "DefaultImpl", out var testStdout, out var testStderr);
            stdout += testStdout;
            stderr += testStderr;
            return exitCode;
        }

        public int RunCommandLineWithArgs(string[] args, out string stdout, out string stderr)
        {
            stdout = string.Empty;
            stderr = string.Empty;

            lock (CheckerLock)
            {
                var originalOut = Console.Out;
                var originalErr = Console.Error;
                var originalExitCode = Environment.ExitCode;
                Environment.ExitCode = 0;

                using var stdOutWriter = new StringWriter();
                using var stdErrWriter = new StringWriter();

                Console.SetOut(stdOutWriter);
                Console.SetError(stdErrWriter);
                try
                {
                    CommandLine.Main(args);

                    stdout = stdOutWriter.ToString();
                    stderr = stdErrWriter.ToString();

                    return Environment.ExitCode;
                }
                catch (Exception ex)
                {
                    stderr = $"Error during PChecker execution: {ex.Message}\nStack Trace: {ex.StackTrace}";
                    return 1;
                }
                finally
                {
                    Console.SetOut(originalOut);
                    Console.SetError(originalErr);
                    Environment.ExitCode = originalExitCode;
                }
            }
        }

        public int RunCheckerInProcess(CheckerConfiguration configuration, string testAssemblyPath, string testMethodName, out string stdout, out string stderr)
        {
            stdout = "";
            stderr = "";

            try
            {
                lock (CheckerLock)
                {
                    var exitCode = 0;
                    var context = new AssemblyLoadContext($"ALC_{Guid.NewGuid():N}", isCollectible: true);
                    Assembly assembly = context.LoadFromAssemblyPath(testAssemblyPath);

                    Type testClass = assembly.GetType($"PImplementation.{testMethodName}");
                    if (testClass == null)
                        throw new Exception($"Test class not found: PImplementation.{testMethodName}");

                    MethodInfo executeMethod = testClass.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
                    if (executeMethod == null)
                        throw new Exception($"Execute method not found in: {testClass.FullName}");

                    var del = (Action<ControlledRuntime>)Delegate.CreateDelegate(typeof(Action<ControlledRuntime>), executeMethod);
                    var engine = TestingEngine.Create(configuration, del);
                    engine.Run();

                    string bug = engine.TestReport.BugReports.FirstOrDefault();
                    if (bug != null)
                    {
                        stdout += bug;
                        exitCode = 1;
                    }

                    context.Unload();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    return exitCode;
                }
            }
            catch (Exception ex)
            {
                stderr = $"Error running Checker: {ex.Message}";
                stdout = $"Stack Trace: {ex.StackTrace}";
                Console.WriteLine(stderr);
                Console.WriteLine(stdout);
                Console.Out.Flush();
                return 1;
            }
        }
        public int DoCompile(DirectoryInfo scratchDirectory)
        {
            var compiler = new Compiler();
            var outputStream = new TestExecutionStream(scratchDirectory);
            var compilerConfiguration = new CompilerConfiguration(outputStream, scratchDirectory, new List<CompilerOutput>{CompilerOutput.PChecker}, sources.Select(x => x.FullName).ToList(), sourceDirectoryName, scratchDirectory);
            try
            {
                return compiler.Compile(compilerConfiguration);
            }
            catch (Exception ex)
            {
                compilerConfiguration.Output.WriteError($"<Internal Error>:\n {ex.Message} {ex.StackTrace}\n<Please report to the P team or create an issue on GitHub, Thanks!>");
                return 1;
            }
        }
    }
}
