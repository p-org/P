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
            var psharpPath = Path.Combine(Constants.SolutionDirectory, "Ext", "PSharp", "bin", "Microsoft.PSharp.dll");
            var args = new[] {$"/r:{psharpPath}", "/t:library"}.Concat(compiledFiles.Select(file => file.Name)).ToArray();
            var exitCode = ProcessHelper.RunWithOutput(scratchDirectory.FullName, out stdout, out stderr, FindCsc(), args);
            foreach (FileInfo compiledFile in compiledFiles)
            {
                stdout += $"{compiledFile.Name}\n===\n{File.ReadAllText(compiledFile.FullName)}\n\n";
            }
            return exitCode;
        }

        private IEnumerable<FileInfo> DoCompile(DirectoryInfo scratchDirectory)
        {
            var compiler = new Compiler();
            var outputStream = new TestExecutionStream(scratchDirectory);
            bool success = compiler.Compile(outputStream, new CommandLineOptions
            {
                OutputLanguage = CompilerOutput.PSharp,
                InputFileNames = sources.Select(file => file.FullName).ToList(),
                ProjectName = "Main"
            });

            if (!success)
            {
                throw new CompilerTestException(TestCaseError.TranslationFailed);
            }

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