using System.Diagnostics;

namespace UnitTests.Core
{
    /// <summary>
    ///     Helpers for running external programs
    /// </summary>
    public static class ProcessHelper
    {
        /// <summary>
        ///     Run a process and collect its output
        /// </summary>
        /// <param name="activeDirectory">The working directory to run the program in</param>
        /// <param name="stdout">The output produced during execution</param>
        /// <param name="stderr">The error output produced during execution</param>
        /// <param name="exeName">The program to run</param>
        /// <param name="argumentList">The arguments to pass to the program</param>
        /// <returns>The exit code produced by the program</returns>
        public static int RunWithOutput(string activeDirectory,
            out string stdout,
            out string stderr, string exeName,
            params string[] argumentList)
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

            var proc = new Process { StartInfo = psi };
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