using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Plang.Compiler;
using UnitTests.Core;

namespace UnitTests.Runners
{
    /// <summary>
    /// Runner for parametric tests that uses generated main.cs instead of creating a hardcoded one
    /// </summary>
    internal class ParametricPCheckerRunner : ICompilerTestRunner
    {
        private readonly FileInfo[] nativeSources;
        private readonly FileInfo[] sources;

        public ParametricPCheckerRunner(FileInfo[] sources)
        {
            this.sources = sources;
            nativeSources = new FileInfo[] { };
        }

        public ParametricPCheckerRunner(FileInfo[] sources, FileInfo[] nativeSources)
        {
            this.sources = sources;
            this.nativeSources = nativeSources;
        }

        private void FileCopy(string src, string target, bool overwrite)
        {
            // during parallel testing we might get "The process cannot access the file because it is being used by another process."
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

            // path to generated code
            DirectoryInfo scratchDirectoryGenerated = Directory.CreateDirectory(Path.Combine(scratchDirectory.FullName, "PChecker"));
            
            // For parametric tests, we don't create our own main function - we use the generated one
            // Only create csproj file
            CreateCSProjFile(scratchDirectoryGenerated);
            
            // copy the foreign code to the folder
            foreach (var nativeFile in nativeSources)
            {
                FileCopy(nativeFile.FullName, Path.Combine(scratchDirectoryGenerated.FullName, nativeFile.Name), true);
            }

            var exitCode = DoCompile(scratchDirectory);

            if (exitCode == 0)
            {
                exitCode = RunPChecker(scratchDirectoryGenerated.FullName,
                    Path.Combine(scratchDirectoryGenerated.FullName, "./net8.0/Main.dll"), out var testStdout, out var testStderr);
                stdout += testStdout;
                stderr += testStderr;
            }
            else
            {
                // compilation returned unexpected error
                exitCode = 2;
            }

            return exitCode;
        }

        private void CreateCSProjFile(DirectoryInfo scratchDirectory)
        {
            const string csprojTemplate = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ApplicationIcon />
    <OutputType>Exe</OutputType>
    <StartupObject />
    <LangVersion>latest</LangVersion>
    <OutputPath>.</OutputPath>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""$(PFolder)/Src/PChecker/CheckerCore/CheckerCore.csproj"" />
  </ItemGroup>
</Project>";
            using var outputFile = new StreamWriter(Path.Combine(scratchDirectory.FullName, "Main.csproj"), false);
            outputFile.WriteLine(csprojTemplate);
        }

        private int RunPChecker(string directory, string dllPath, out string stdout, out string stderr)
        {
            return ProcessHelper.RunWithOutput(directory, out stdout, out stderr, "dotnet", dllPath);
        }

        public int DoCompile(DirectoryInfo scratchDirectory)
        {
            var compiler = new Compiler();
            var outputStream = new TestExecutionStream(scratchDirectory);
            var compilerConfiguration = new CompilerConfiguration(outputStream, scratchDirectory, new List<CompilerOutput>{CompilerOutput.PChecker}, sources.Select(x => x.FullName).ToList(), "Main", scratchDirectory);
            try
            {
                return compiler.Compile(compilerConfiguration);
            }
            catch (Exception ex)
            {
                compilerConfiguration.Output.WriteError($"<Internal Error>:\n {ex.Message}\n<Please report to the P team or create an issue on GitHub, Thanks!>");
                return 1;
            }
        }
    }
}
