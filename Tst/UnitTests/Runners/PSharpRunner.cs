using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Plang.Compiler;
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
            CreateProjectFile(scratchDirectory);

            var psharpExtensionsPath = Path.Combine(Constants.SolutionDirectory, "Bld", "Drops", Constants.BuildConfiguration, "AnyCPU", "Binaries", "PrtSharp.dll");
            File.Copy(psharpExtensionsPath, Path.Combine(scratchDirectory.FullName, "PrtSharp.dll"), true);

            foreach (var nativeFile in nativeSources)
            {
                File.Copy(nativeFile.FullName, Path.Combine(scratchDirectory.FullName, nativeFile.Name), true);
            }

            var args = new[] { "build", "Test.csproj" };

            var exitCode =
                ProcessHelper.RunWithOutput(scratchDirectory.FullName, out stdout, out stderr, FindDotnet(), args);

            /*foreach (var compiledFile in compiledFiles)
                stdout += $"{compiledFile.Name}\n===\n{File.ReadAllText(compiledFile.FullName)}\n\n";*/

            if (exitCode == 0)
            {
                exitCode = RunPSharpTester(scratchDirectory.FullName,
                    Path.Combine(scratchDirectory.FullName, "Test.dll"), out var testStdout, out var testStderr);
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

        private void CreateProjectFile(DirectoryInfo dir)
        {
            var projectFileContents = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework >netcoreapp2.1</TargetFramework>
    <ApplicationIcon />
    <OutputType>library</OutputType>
    <StartupObject />
    <LangVersion >latest</LangVersion>
    <OutputPath>.</OutputPath>
  </PropertyGroup >
  <PropertyGroup Condition = ""'$(Configuration)|$(Platform)'=='Debug|AnyCPU'"">
    <WarningLevel>0</WarningLevel>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.PSharp"" Version=""1.6.9""/>
    <Reference Include = ""PrtSharp.dll""/>
  </ItemGroup>
</Project>";
            using (var outputFile = new StreamWriter(Path.Combine(dir.FullName, "Test.csproj"), false))
            {
                outputFile.WriteLine(projectFileContents);
            }
        }

        private int RunPSharpTester(string directory, string dllPath, out string stdout, out string stderr)
        {
            // TODO: bug P# team for how to run a test w/o invoking executable
            string testerPath = Path.Combine(PSharpAssemblyLocation, "..", "netcoreapp2.1", "PSharpTester.dll");
            return ProcessHelper.RunWithOutput(directory, out stdout, out stderr, "dotnet", testerPath, $"\"/test:{dllPath}\"", $"\"/max-steps:1000\"", $"\"/i:2000\"", $"\"/sch:dfs\"");
        }

        private IEnumerable<FileInfo> DoCompile(DirectoryInfo scratchDirectory)
        {
            var compiler = new Compiler();
            var outputStream = new TestExecutionStream(scratchDirectory);
            var compilationJob = new CompilationJob(outputStream, CompilerOutput.PSharp, sources, "Main");
            compiler.Compile(compilationJob);
            return outputStream.OutputFiles;
        }

        private static string FindDotnet()
        {
            string[] dotnetPaths =
            {
                @"C:\Program Files\dotnet\dotnet.exe",
                @"C:\Program Files (x86)\dotnet\dotnet.exe",
                Environment.GetEnvironmentVariable("DOTNET") ?? ""
            };

            var dotnetPath = dotnetPaths.FirstOrDefault(File.Exists);
            if (dotnetPath == null)
                throw new CompilerTestException(TestCaseError.GeneratedSourceCompileFailed, "Could not find MSBuild");

            return dotnetPath;
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
