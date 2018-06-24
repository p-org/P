using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Pc;
using Microsoft.Pc.Backend;
using UnitTests.Core;

namespace UnitTests.Runners
{
    public class ExecutionRunner : ICompilerTestRunner
    {
        private readonly DirectoryInfo prtTestProjDirectory;
        private readonly IReadOnlyList<FileInfo> sources;

        public ExecutionRunner(IReadOnlyList<FileInfo> sources)
        {
            this.sources = sources;
            prtTestProjDirectory = Directory.CreateDirectory(Path.Combine(Constants.TestDirectory, Constants.CRuntimeTesterDirectoryName));
        }

        public int? RunTest(DirectoryInfo scratchDirectory, out string stdout, out string stderr)
        {
            DoCompile(scratchDirectory);

            string tmpDirName = scratchDirectory.FullName;

            // Copy source files into destination directory
            foreach (FileInfo source in sources)
            {
                source.CopyTo(Path.Combine(tmpDirName, source.Name), true);
            }

            // Copy tester into destination directory.
            CopyFiles(prtTestProjDirectory, tmpDirName);
            if (!RunMsBuildExe(tmpDirName, out stdout, out stderr))
            {
                throw new TestRunException(TestCaseError.GeneratedSourceCompileFailed);
            }

            return ProcessHelper.RunWithOutput(
                Path.Combine(tmpDirName, Constants.BuildConfiguration, Constants.Platform, Constants.CTesterExecutableName),
                tmpDirName,
                Enumerable.Empty<string>(),
                out stdout,
                out stderr);
        }

        private static void CopyFiles(DirectoryInfo src, string target)
        {
            foreach (var file in src.GetFiles())
            {
                File.Copy(file.FullName, Path.Combine(target, file.Name), true);
            }
        }

        private void DoCompile(DirectoryInfo scratchDirectory)
        {
            var compiler = new AntlrCompiler();
            var outputStream = new TestExecutionStream(scratchDirectory);
            bool success = compiler.Compile(outputStream, new CommandLineOptions
            {
                compilerOutput = CompilerOutput.C,
                inputFileNames = sources.Select(file => file.FullName).ToList(),
                projectName = "main"
            });

            if (!success)
            {
                throw new TestRunException(TestCaseError.TranslationFailed);
            }
        }

        private static bool RunMsBuildExe(string tmpDir, out string stdout, out string stderr)
        {
            const string msbuildpath = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe";
            int exitStatus = ProcessHelper.RunWithOutput(
                msbuildpath,
                tmpDir,
                new[] {$"/p:Configuration={Constants.BuildConfiguration}", $"/p:Platform={Constants.Platform}", "/t:Build"},
                out stdout,
                out stderr
            );
            return exitStatus == 0;
        }

        private class TestExecutionStream : ICompilerOutput
        {
            private readonly DirectoryInfo outputDirectory;
            private readonly List<FileInfo> outputFiles = new List<FileInfo>();

            public TestExecutionStream(DirectoryInfo outputDirectory)
            {
                this.outputDirectory = outputDirectory;
            }

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
    }
}
