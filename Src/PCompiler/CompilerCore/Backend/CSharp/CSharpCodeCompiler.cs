using System.Diagnostics;
using System.IO;

namespace Plang.Compiler.Backend.CSharp
{
    internal class CSharpCodeCompiler
    {
        private static string csprojTemplate = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <ApplicationIcon />
    <OutputType>Exe</OutputType>
    <StartupObject />
    <LangVersion>latest</LangVersion>
    <OutputPath>.</OutputPath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.Coyote"" Version=""1.0.5""/>
    <PackageReference Include=""PCSharpRuntime"" Version=""*""/>
  </ItemGroup>
</Project>";

        private static string mainCode = @"
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using System;
using System.Linq;

namespace -projectName-
{
    public class _TestRegression {
        public static void Main(string[] args)
        {
            /*
            // Optional: increases verbosity level to see the Coyote runtime log.
            Configuration configuration = Configuration.Create().WithTestingIterations(1000);
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
             For replaying a bug and single stepping
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

        public static void Compile(ICompilationJob job)
        {
            var csprojName = $"{job.ProjectName}.csproj";
            var csprojPath = Path.Combine(job.ProjectRootPath.FullName, csprojName);
            var mainFilePath = Path.Combine(job.ProjectRootPath.FullName, "Test.cs");
            string stdout = "";
            string stderr = "";
            // if the file does not exist then create the file
            if (!File.Exists(csprojPath))
            {
                csprojTemplate = csprojTemplate.Replace("-directory-",
                    Path.GetRelativePath(job.ProjectRootPath.FullName, job.OutputDirectory.FullName));
                File.WriteAllText(csprojPath, csprojTemplate);
            }

            // if the Main file does not exist then create the file
            if (!File.Exists(mainFilePath))
            {
                mainCode = mainCode.Replace("-projectName-", job.ProjectName);
                File.WriteAllText(mainFilePath, mainCode);
            }

            // compile the csproj file
            string[] args = new[] { "build -c Release", csprojName };

            int exitCode = RunWithOutput(job.ProjectRootPath.FullName, out stdout, out stderr, "dotnet", args);
            if (exitCode != 0)
            {
                throw new TranslationException($"Compiling generated C# code FAILED!\n" + $"{stdout}\n" + $"{stderr}\n");
            }
            else
            {
                job.Output.WriteInfo($"{stdout}");
            }
        }

        public static int RunWithOutput(string activeDirectory,
            out string stdout,
            out string stderr, string exeName,
            params string[] argumentList)
        {
            ProcessStartInfo psi = new ProcessStartInfo(exeName)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WorkingDirectory = activeDirectory,
                Arguments = string.Join(" ", argumentList)
            };

            string mStdout = "", mStderr = "";

            Process proc = new Process { StartInfo = psi };
            proc.OutputDataReceived += (s, e) => { mStdout += $"{e.Data}\n"; };
            proc.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    mStderr += $"{e.Data}\n";
                }
            };

            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            stdout = mStdout;
            stderr = mStderr;
            return proc.ExitCode;
        }
    }
}