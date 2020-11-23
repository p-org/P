using System.IO;
using UnitTests.Core;

namespace UnitTests.Runners
{
    internal class RvmUnitRunner : ICompilerTestRunner
    {
        private readonly FileInfo[] sources;

        public RvmUnitRunner(FileInfo[] sources)
        {
            this.sources = sources;
        }

        public int? RunTest(DirectoryInfo scratchDirectory, out string stdout, out string stderr)
        {
            if (sources.Length == 0) {
                stdout = "Exiting empty test.";
                stderr = "Test directory without any tests!";
                return 1;
            }

            stdout = "";
            stderr = "";

            string scriptDir = sources[0].Directory.Parent.Parent.Parent.ToString();

            int result = RVMonitorBuilder.buildRVMonitorIfNeeded(scriptDir, ref stdout, ref stderr);
            if (result != 0)
            {
                return result;
            }

            PythonRunner p = new PythonRunner();

            string scriptPath = Path.Combine(scriptDir, "run_unittest.py");
            p.AddArg(scriptPath);
            p.AddArg(sources[0].Directory.Name);
            p.AddArg(scratchDirectory.FullName);

            p.Run();

            stdout += p.GetStdout();
            stderr += p.GetStderr();

            return p.GetExitCode();
        }

    }
}
