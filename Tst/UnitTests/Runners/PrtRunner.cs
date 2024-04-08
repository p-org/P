using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plang.Compiler;
using UnitTests.Core;

namespace UnitTests.Runners
{
    /// <inheritdoc />
    /// <summary>
    ///     Runs a test using the Prt backend, calling MSBuild in the process
    /// </summary>
    public class PrtRunner : ICompilerTestRunner
    {
        private readonly IReadOnlyList<FileInfo> nativeSources;

        private readonly DirectoryInfo prtTestProjDirectory =
            new DirectoryInfo(Path.Combine(Constants.TestDirectory, Constants.CRuntimeTesterDirectoryName));

        private readonly IReadOnlyList<FileInfo> sources;

        /// <summary>
        ///     Box a new test runner from the given P source files
        /// </summary>
        /// <param name="sources">P source files to compile</param>
        public PrtRunner(IReadOnlyList<FileInfo> sources)
        {
            this.sources = sources;
            nativeSources = new FileInfo[] { };
        }

        public PrtRunner(IReadOnlyList<FileInfo> sources, IReadOnlyList<FileInfo> nativeSources)
        {
            this.sources = sources;
            this.nativeSources = nativeSources;
        }

        public int? RunTest(DirectoryInfo scratchDirectory, out string stdout, out string stderr)
        {
            DoCompile(scratchDirectory);

            var tmpDirName = scratchDirectory.FullName;

            // Copy source files into destination directory
            foreach (var source in sources)
            {
                source.CopyTo(Path.Combine(tmpDirName, source.Name), true);
            }

            // Copy native source files into destination directory
            foreach (var source in nativeSources)
            {
                source.CopyTo(Path.Combine(tmpDirName, source.Name), true);
            }

            // Copy tester into destination directory.
            CopyFiles(prtTestProjDirectory, tmpDirName);
            if (!RunMsBuildExe(tmpDirName, out stdout, out stderr))
            {
                throw new CompilerTestException(TestCaseError.GeneratedSourceCompileFailed);
            }

            var testerExeName = Path.Combine(tmpDirName, Constants.BuildConfiguration, Constants.Platform,
                Constants.CTesterExecutableName);
            return ProcessHelper.RunWithOutput(tmpDirName, out stdout, out stderr, testerExeName);
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
            var compiler = new Compiler();
            var outputStream = new TestExecutionStream(scratchDirectory);
            var compilerConfiguration = new CompilerConfiguration(outputStream, scratchDirectory,
                new List<CompilerOutput>{CompilerOutput.C}, sources.Select(x => x.FullName).ToList(), "main");
            compiler.Compile(compilerConfiguration);
        }

        private static bool RunMsBuildExe(string tmpDir, out string stdout, out string stderr)
        {
            string[] msbuildpaths =
            {
                @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\msbuild.exe",
                Environment.GetEnvironmentVariable("MSBUILD", EnvironmentVariableTarget.Machine) ?? "",
                Environment.GetEnvironmentVariable("MSBUILD", EnvironmentVariableTarget.User) ?? ""
            };

            var msbuildpath = msbuildpaths.FirstOrDefault(File.Exists);
            if (msbuildpath == null)
            {
                Console.Error.WriteLine("Could not find MSBuild, set MSBUILD environment variable");
                throw new CompilerTestException(TestCaseError.GeneratedSourceCompileFailed, "Could not find MSBuild");
            }

            var exitStatus = ProcessHelper.RunWithOutput(
                tmpDir,
                out stdout,
                out stderr,
                msbuildpath,
                $"/p:CheckerConfiguration={Constants.BuildConfiguration}",
                $"/p:Platform={Constants.Platform}", "/t:Build");
            return exitStatus == 0;
        }
    }
}