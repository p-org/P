using System;
using System.IO;
using UnitTests.Core;

namespace UnitTests.Runners
{
    internal class RvmExampleRunner : ICompilerTestRunner
    {
        private readonly FileInfo[] sources;

        public RvmExampleRunner(FileInfo[] sources)
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

            string buildScriptDir = sources[0].Directory.Parent.Parent.Parent.ToString();

            int result = RVMonitorBuilder.buildRVMonitorIfNeeded(buildScriptDir, ref stdout, ref stderr);
            if (result != 0)
            {
                return result;
            }

            PythonRunner p = new PythonRunner();

            string scriptPath = Path.Combine(sources[0].Directory.Parent.ToString(), "run_test.py");
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
