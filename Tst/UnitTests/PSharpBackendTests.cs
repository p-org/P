using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Pc;
using NUnit.Framework;
using UnitTests.Core;
using UnitTests.Runners;
using UnitTests.Validators;

namespace UnitTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    internal class PSharpBackendTests
    {
        [Test]
        public void TestCompilePSharpTemp()
        {
            DirectoryInfo tempDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory, "TestTemp"));
            var tempFilePath = new FileInfo(Path.Combine(Constants.SolutionDirectory, "tmp", "test.p"));

            if (!tempFilePath.Exists)
            {
                return;
            }

            var testCase = new CompilerTestCase(
                tempDir,
                new PSharpRunner(new[] {tempFilePath}),
                new CompileSuccessValidator());

            TestAssertions.AssertTestCase(testCase);
        }
    }

    internal class PSharpRunner : ICompilerTestRunner
    {
        private readonly FileInfo[] sources;

        public PSharpRunner(FileInfo[] sources)
        {
            this.sources = sources;
        }

        public int? RunTest(DirectoryInfo scratchDirectory, out string stdout, out string stderr)
        {
            var compiledFiles = DoCompile(scratchDirectory);
            var psharpPath = Path.Combine(Constants.SolutionDirectory, "Ext", "PSharp", "bin", "Microsoft.PSharp.dll");
            var args = new[] {$"/r:{psharpPath}"}.Concat(compiledFiles.Select(file => file.Name)).ToArray();
            return ProcessHelper.RunWithOutput(scratchDirectory.FullName, out stdout, out stderr, FindCsc(), args);
        }

        private IEnumerable<FileInfo> DoCompile(DirectoryInfo scratchDirectory)
        {
            var compiler = new AntlrCompiler();
            var outputStream = new TestExecutionStream(scratchDirectory);
            bool success = compiler.Compile(outputStream, new CommandLineOptions
            {
                compilerOutput = CompilerOutput.PSharp,
                inputFileNames = sources.Select(file => file.FullName).ToList(),
                projectName = "Main"
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
