using System;
using System.Configuration;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;

namespace UnitTestsCore
{
    public static class Constants
    {
        public const string CategorySeparator = " | ";
        public const string CRuntimeTesterDirectoryName = "PrtTester";
        public const string NewLinePattern = @"\r\n|\n\r|\n|\r";
        public const string XmlProfileFileName = "TestProfile.xml";
        public const string PSolutionFileName = "P.sln";
        public const string TestDirectoryName = "Tst";
        public const string CTesterExecutableName = "tester.exe";
        public const string CTesterVsProjectName = "Tester.vcxproj";
        public const string CorrectOutputFileName = "acc_0.txt";
        public const string TestConfigFileName = "testconfig.txt";
        public const string DiffTool = "kdiff3";
        public const string DisplayDiffsFile = "display-diffs.bat";
        public const string ActualOutputFileName = "check-output.log";
        public const string FrontEndRegressionFileName = "frontend-regression.txt";
#if DEBUG
        public const string BuildConfiguration = "Debug";
#else
        public const string BuildConfiguration = "Release";
#endif
        public static string Platform { get; } = Environment.Is64BitProcess ? "x64" : "x86";

        private static readonly Lazy<string> LazySolutionDirectory = new Lazy<string>(
            () =>
            {
                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                Contract.Assert(assemblyDirectory != null);
                for (var dir = new DirectoryInfo(assemblyDirectory); dir != null; dir = dir.Parent)
                {
                    if (File.Exists(Path.Combine(dir.FullName, PSolutionFileName)))
                    {
                        return dir.FullName;
                    }
                }

                throw new FileNotFoundException();
            });

        public static string SolutionDirectory => LazySolutionDirectory.Value;

        public static string TestDirectory => Path.Combine(SolutionDirectory, TestDirectoryName);

        public static bool ResetTests => bool.Parse(ConfigurationManager.AppSettings["ResetTests"]);
        public static bool RunPc => bool.Parse(ConfigurationManager.AppSettings["RunPc"]);
        public static bool RunPrt => bool.Parse(ConfigurationManager.AppSettings["RunPrt"]);
        public static bool RunPt => bool.Parse(ConfigurationManager.AppSettings["RunPt"]);
        public static bool RunZing => bool.Parse(ConfigurationManager.AppSettings["RunZing"]);
        public static bool RunAll => bool.Parse(ConfigurationManager.AppSettings["RunAll"]);
        public static bool PtWithPSharp => bool.Parse(ConfigurationManager.AppSettings["PtWithPSharp"]);

        public static string TestResultsDirectory { get; } =
            Path.Combine(TestDirectory, $"TestResult_{BuildConfiguration}_{Platform}");
    }
}
