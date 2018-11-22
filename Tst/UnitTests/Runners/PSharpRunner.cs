using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Pc;
using Microsoft.PSharp.TestingServices;
using UnitTests.Core;

namespace UnitTests.Runners
{
    internal class PSharpRunner : ICompilerTestRunner
    {
        private static readonly string PSharpAssemblyLocation =
            Path.GetDirectoryName(typeof(TestingEngineFactory).GetTypeInfo().Assembly.Location);

        private readonly FileInfo[] nativeSources;
        private readonly FileInfo[] sources;

        public PSharpRunner(FileInfo[] sources)
        {
            this.sources = sources;
            nativeSources = new FileInfo[] { };
        }

        public PSharpRunner(FileInfo[] sources, FileInfo[] nativeSources)
        {
            this.sources = sources;
            this.nativeSources = nativeSources;
        }

        public int? RunTest(DirectoryInfo scratchDirectory, out string stdout, out string stderr)
        {
            var compiledFiles = DoCompile(scratchDirectory).ToArray();
            CreateFileWithMainFunction(scratchDirectory);

            var dependencies = new List<string> {"netstandard.dll", "System.Runtime.dll", "System.Collections.dll"};
            var psharpPath = Path.Combine(PSharpAssemblyLocation, "..", "net46", "Microsoft.PSharp.dll");
            var psharpExtensionsPath = Path.Combine(Constants.SolutionDirectory, "Bld", "Drops",
                Constants.BuildConfiguration, "AnyCPU", "Binaries", "PrtSharp.dll");
            dependencies.Add(psharpExtensionsPath);
            dependencies.Add(psharpPath);

            File.Copy(psharpExtensionsPath, Path.Combine(scratchDirectory.FullName, "PrtSharp.dll"), true);

            var args = new[] {"/t:exe"}.Concat(dependencies.Select(dep => $"/r:\"{dep}\""))
                .Concat(compiledFiles.Select(file => file.Name))
                .Append("Test.cs")
                .Concat(nativeSources.Select(file => file.FullName))
                .ToArray();

            var exitCode =
                ProcessHelper.RunWithOutput(scratchDirectory.FullName, out stdout, out stderr, FindCsc(), args);

            foreach (var compiledFile in compiledFiles)
                stdout += $"{compiledFile.Name}\n===\n{File.ReadAllText(compiledFile.FullName)}\n\n";

            if (exitCode == 0)
            {
                exitCode = RunPSharpTester(scratchDirectory.FullName,
                    Path.Combine(scratchDirectory.FullName, "Test.exe"), out var testStdout, out var testStderr);
                stdout += testStdout;
                stderr += testStderr;

                // TODO: bug P# folks to either set an exit code or print obvious indicator that can be machine-processed.
                if (testStdout.Contains("buggy schedules"))
                {
                    exitCode = 1;
                }
            }

            return exitCode;
        }

        private void CreateFileWithMainFunction(DirectoryInfo dir)
        {
            var testCode = @"
using Microsoft.PSharp;
using System;

namespace Main
{
    public class _TestRegression {
        public static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the P# runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled(2);

            // Creates a new P# runtime instance, and passes an optional configuration.
            var runtime = PSharpRuntime.Create(configuration);

            // Executes the P# program.
            DefaultImpl.Execute(runtime);

            // The P# runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine(""Press Enter to terminate..."");
        }
    }
}";
            using (var outputFile = new StreamWriter(Path.Combine(dir.FullName, "Test.cs"), false))
            {
                outputFile.WriteLine(testCode);
            }
        }

        private int RunPSharpTester(string directory, string dllPath, out string stdout, out string stderr)
        {
            // TODO: bug P# team for how to run a test w/o invoking executable
            var testerPath = Path.Combine(PSharpAssemblyLocation, "..", "net46", "PSharpTester.exe");
            return ProcessHelper.RunWithOutput(directory, out stdout, out stderr, testerPath, $"\"/test:{dllPath}\"",
                "\"/i:2000\"", "\"/sch:pct:2\"");
        }

        private IEnumerable<FileInfo> DoCompile(DirectoryInfo scratchDirectory)
        {
            var compiler = new Compiler();
            var outputStream = new TestExecutionStream(scratchDirectory);
            var compilationJob = new CompilationJob(outputStream, CompilerOutput.PSharp, sources, "Main");
            compiler.Compile(compilationJob);
            return outputStream.OutputFiles;
        }

        private static string FindCsc()
        {
            string[] cscPaths =
            {
                @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\Roslyn\csc.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\Roslyn\csc.exe",
                Environment.GetEnvironmentVariable("CSC") ?? ""
            };

            var cscPath = cscPaths.FirstOrDefault(File.Exists);
            if (cscPath == null)
                throw new CompilerTestException(TestCaseError.GeneratedSourceCompileFailed, "Could not find MSBuild");

            return cscPath;
        }
    }
}