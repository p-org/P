using System.Diagnostics;
using System.IO;

namespace Plang.Compiler.Backend.CSharp
{
    internal class Constants
    {
        internal static readonly string csprojTemplate = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <ApplicationIcon />
    <OutputType>Exe</OutputType>
    <StartupObject />
    <LangVersion>latest</LangVersion>
    <OutputPath>POutput/</OutputPath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.Coyote"" Version=""1.0.5""/>
    <PackageReference Include=""PCSharpRuntime"" Version=""*""/>
  </ItemGroup>
</Project>";

        internal static readonly string mainCode = @"
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.Actors;
using System;
using System.Linq;
using System.IO;

namespace -projectName-
{
    public class _TestRegression {
        public static void Main(string[] args)
        {
            /*
            Configuration configuration = Configuration.Create();
            configuration.WithVerbosityEnabled(true);
            // update the path to the schedule file.
            string schedule = File.ReadAllText(""absolute path to *.schedule file"");
            configuration.WithReplayStrategy(schedule);
            TestingEngine engine = TestingEngine.Create(configuration, (Action<IActorRuntime>)PImplementation.<Name of the test case>.Execute);
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
    }
}
