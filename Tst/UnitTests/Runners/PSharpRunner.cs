using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Pc;
using UnitTests.Core;

namespace UnitTests.Runners
{
    internal class PSharpRunner : ICompilerTestRunner
    {
        private readonly FileInfo[] sources;

        public PSharpRunner(FileInfo[] sources)
        {
            this.sources = sources;
        }

        public int? RunTest(DirectoryInfo scratchDirectory, out string stdout, out string stderr)
        {
            var compiledFiles = DoCompile(scratchDirectory).ToArray();
            var dependencies = new List<string>{"netstandard.dll", "System.Runtime.dll", "System.Collections.dll" };
            var psharpPath = Path.Combine(Constants.SolutionDirectory, "Ext", "psharp", "bin", "Microsoft.PSharp.dll");
            var psharpExtensionsPath = Path.Combine(Constants.SolutionDirectory, "Drops",Constants.BuildConfiguration,"AnyCPU","Binaries","PrtSharp.dll");
            dependencies.Add(psharpExtensionsPath);
            dependencies.Add(psharpPath);
            var args = new[] {"/t:library"}.Concat(dependencies.Select(dep => $"/r:\"{dep}\""))
                                           .Concat(compiledFiles.Select(file => file.Name)).ToArray();
            var exitCode = ProcessHelper.RunWithOutput(scratchDirectory.FullName, out stdout, out stderr, FindCsc(), args);
            foreach (var compiledFile in compiledFiles)
            {
                stdout += $"{compiledFile.Name}\n===\n{File.ReadAllText(compiledFile.FullName)}\n\n";
            }
            return exitCode;
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
            string[] cscPaths = {
                @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\Roslyn\csc.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\Roslyn\csc.exe",
                Environment.GetEnvironmentVariable("CSC") ?? ""
            };

            string cscPath = cscPaths.FirstOrDefault(File.Exists);
            if (cscPath == null)
            {
                throw new CompilerTestException(TestCaseError.GeneratedSourceCompileFailed, "Could not find MSBuild");
            }

            return cscPath;
        }
    }
}