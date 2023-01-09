using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace UnitTests.Core
{
    /// <summary>
    ///     Constants used by the unit tests.
    /// </summary>
    public static class Constants
    {
        public const string CategorySeparator = " | ";
        public const string CRuntimeTesterDirectoryName = "PrtTester";
        public const string PSolutionFileName = "P.sln";
        public const string TestDirectoryName = "Tst";
        public const string CTesterExecutableName = "tester.exe";
        public const string CorrectOutputFileName = "acc_0.txt";
#if DEBUG
        public const string BuildConfiguration = "Debug";
#else
        public const string BuildConfiguration = "Release";
#endif
        public static string Platform { get; } = Environment.Is64BitProcess ? "x64" : "x86";

        private static readonly Lazy<string> LazySolutionDirectory = new Lazy<string>(
            () =>
            {
                var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                Debug.Assert(assemblyDirectory != null);
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

        public static readonly string ScratchParentDirectory = Path.Combine(TestDirectory, "temp_builds");
    }
}