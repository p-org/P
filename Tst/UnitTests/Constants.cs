using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace UnitTests
{
    internal static class Constants
    {
        internal const string CategorySeparator = " | ";
        internal const string CRuntimeTesterDirectoryName = "PrtTester";
        internal const string NewLinePattern = @"\r\n|\n\r|\n|\r";
        internal const string XmlProfileFileName = "TestProfile.xml";
        internal const string PSolutionFileName = "P.sln";
        internal const string TestDirectoryName = "Tst";
        internal const string CTesterExecutableName = "tester.exe";
        internal const string CTesterVsProjectName = "Tester.vcxproj";
        internal const string CorrectOutputFileName = "acc_0.txt";
        internal const string TestConfigFileName = "testconfig.txt";
        internal const string DiffTool = "kdiff3";
        internal const string DisplayDiffsFile = "display-diffs.bat";
        internal const string ActualOutputFileName = "check-output.log";
#if DEBUG
        internal const string Configuration = "Debug";
#else
        internal const string Configuration = "Release";
#endif
        internal static string Platform { get; } = Environment.Is64BitProcess ? "x64" : "x86";

        private static readonly Lazy<string> LazySolutionDirectory = new Lazy<string>(
            () =>
            {
                string assemblyPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
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

        internal static string SolutionDirectory => LazySolutionDirectory.Value;

        internal static string TestDirectory => Path.Combine(SolutionDirectory, TestDirectoryName);

        internal static bool ResetTests => Properties.Settings.Default.ResetTests;
        internal static bool RunPc => Properties.Settings.Default.RunPc;
        internal static bool RunPrt => Properties.Settings.Default.RunPrt;
        internal static bool RunPt => Properties.Settings.Default.RunPt;
        internal static bool RunZing => Properties.Settings.Default.RunZing;
        internal static bool RunAll => Properties.Settings.Default.RunAll;

    }
}