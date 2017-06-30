using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTests
{
    internal class ProcessHelper
    {
        public static int RunWithOutput(
            string exeName,
            string activeDirectory,
            IEnumerable<string> argumentList,
            out string stdout,
            out string stderr)
        {
            var psi = new ProcessStartInfo(exeName)
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

            var proc = new Process {StartInfo = psi};
            proc.OutputDataReceived += (s, e) => { mStdout += $"OUT: {e.Data}\n"; };
            proc.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    mStderr += $"ERROR: {e.Data}\n";
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