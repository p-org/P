using Microsoft.Coyote.SystematicTesting;
using Plang.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnitTests.Core;

namespace UnitTests.Runners
{
    internal class CoyoteRunner : ICompilerTestRunner
    {
        private static readonly string CoyoteAssemblyLocation =
            Path.GetDirectoryName(typeof(TestingEngine).GetTypeInfo().Assembly.Location);

        private readonly FileInfo[] nativeSources;
        private readonly FileInfo[] sources;

        public CoyoteRunner(FileInfo[] sources)
        {
            this.sources = sources;
            nativeSources = new FileInfo[] { };
        }

        public CoyoteRunner(FileInfo[] sources, FileInfo[] nativeSources)
        {
            this.sources = sources;
            this.nativeSources = nativeSources;
        }

        private void FileCopy(string src, string target, bool overwrite)
        {
            // during parallel testing we might get "The process cannot access the file because it is being used by another process."
            int retries = 5;
            while (retries-- > 0)
            {
                try
                {
                    File.Copy(src, target, overwrite);
                    return;
                } 
                catch (System.IO.IOException)
                {
                    if (retries == 1)
                    {
                        throw;
                    }
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        public int? RunTest(DirectoryInfo scratchDirectory, out string stdout, out string stderr)
        {
            FileInfo[] compiledFiles = DoCompile(scratchDirectory).ToArray();
            CreateFileWithMainFunction(scratchDirectory);
            CreateProjectFile(scratchDirectory);

            string coyoteExtensionsPath = Path.Combine(Constants.SolutionDirectory, "Bld", "Drops", Constants.BuildConfiguration, "Binaries", "CoyoteRuntime.dll");
            FileCopy(coyoteExtensionsPath, Path.Combine(scratchDirectory.FullName, "CoyoteRuntime.dll"), true);

            foreach (FileInfo nativeFile in nativeSources)
            {
                FileCopy(nativeFile.FullName, Path.Combine(scratchDirectory.FullName, nativeFile.Name), true);
            }

            string[] args = new[] { "build", "Test.csproj" };

            int exitCode =
                ProcessHelper.RunWithOutput(scratchDirectory.FullName, out stdout, out stderr, "dotnet", args);

            if (exitCode == 0)
            {
                exitCode = RunCoyoteTester(scratchDirectory.FullName,
                    Path.Combine(scratchDirectory.FullName, "Test.dll"), out string testStdout, out string testStderr);
                stdout += testStdout;
                stderr += testStderr;

                // TODO: bug Coyote folks to either set an exit code or print obvious indicator that can be machine-processed.
                if (testStdout.Contains("buggy schedules"))
                {
                    exitCode = 1;
                }
            }

            return exitCode;
        }

        private void CreateFileWithMainFunction(DirectoryInfo dir)
        {
            string testCode = @"
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using System;
using System.Linq;

namespace Main
{
    public class _TestRegression {
        public static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the Coyote runtime log.
            Configuration configuration = Configuration.Create().WithTestingIterations(100);
            configuration.WithMaxSchedulingSteps(1000);
            TestingEngine engine = TestingEngine.Create(configuration, DefaultImpl.Execute);
            engine.Run();
            string bug = engine.TestReport.BugReports.FirstOrDefault();
            if (bug != null)
            {
                Console.WriteLine(bug);
                Environment.Exit(1);
            }
            Environment.Exit(0);

            // for debugging:
            /* For replaying a bug and single stepping
            Configuration configuration = Configuration.Create();
            configuration.WithVerbosityEnabled(true);
            // update the path to the schedule file.
            configuration.WithReplayStrategy(""AfterNewUpdate.schedule"");
            TestingEngine engine = TestingEngine.Create(configuration, DefaultImpl.Execute);
            engine.Run();
            string bug = engine.TestReport.BugReports.FirstOrDefault();
            if (bug != null)
            {
                Console.WriteLine(bug);
            }
            */
        }
    }
}";
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(dir.FullName, "Test.cs"), false))
            {
                outputFile.WriteLine(testCode);
            }
        }

        private void CreateProjectFile(DirectoryInfo dir)
        {
            string projectFileContents = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework >netcoreapp3.1</TargetFramework>
    <ApplicationIcon />
    <OutputType>Exe</OutputType>
    <StartupObject />
    <LangVersion >latest</LangVersion>
    <OutputPath>.</OutputPath>
  </PropertyGroup >
  <PropertyGroup Condition = ""'$(Configuration)|$(Platform)'=='Debug|AnyCPU'"">
    <WarningLevel>0</WarningLevel>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.Coyote"" Version=""1.0.5""/>
    <Reference Include = ""CoyoteRuntime.dll""/>
  </ItemGroup>
</Project>";
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(dir.FullName, "Test.csproj"), false))
            {
                outputFile.WriteLine(projectFileContents);
            }
        }

        private int RunCoyoteTester(string directory, string dllPath, out string stdout, out string stderr)
        {
            return ProcessHelper.RunWithOutput(directory, out stdout, out stderr, "dotnet", dllPath);
        }

        private IEnumerable<FileInfo> DoCompile(DirectoryInfo scratchDirectory)
        {
            Compiler compiler = new Compiler();
            TestExecutionStream outputStream = new TestExecutionStream(scratchDirectory);
            CompilationJob compilationJob = new CompilationJob(outputStream, CompilerOutput.Coyote, sources, "Main");
            compiler.Compile(compilationJob);
            return outputStream.OutputFiles;
        }
    }
}