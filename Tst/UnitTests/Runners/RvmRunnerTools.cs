using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnitTests.Runners
{
    // TODO: Move this in its own file if multiple runners start using it.
    internal class RvmRunnerInitializer
    {
        internal static RvmRunnerInitializer MonitorInitializer {get;} = new RvmRunnerInitializer();
        enum State
        {
            NOT_INITIALIZED,
            INITIALIZE_FAILURE,
            INITIALIZED
        }

        static State state = State.NOT_INITIALIZED;
        private readonly object stateLock = new object();

        internal delegate bool Initializer();

        private RvmRunnerInitializer() {}

        internal bool initialize(Initializer initializer)
        {
            lock(stateLock)
            {
                switch (state)
                {
                    case State.NOT_INITIALIZED:
                        // This will take a long time, keeping everyone locked,
                        // but there's no other option because they can't
                        // proceed before initialization.
                        if (initializer())
                        {
                            state = State.INITIALIZED;
                            return true;
                        }
                        else
                        {
                            state = State.INITIALIZE_FAILURE;
                            return false;
                        }
                    case State.INITIALIZED:
                        return true;
                    case State.INITIALIZE_FAILURE:
                        return false;
                    default:
                        throw new System.Exception("Invalid state: " + state);
                }
            }
        }
    }

    internal class PythonRunner
    {
        private System.Diagnostics.Process Process { get; }

        private StringBuilder stdout_sb = new StringBuilder();
        private StringBuilder stderr_sb = new StringBuilder();
        private bool hasStarted = false;

        internal PythonRunner()
        {
            string[] paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
            IEnumerable<string> pathext = (Environment.GetEnvironmentVariable("PATHEXT") ?? "")
                .Split(Path.PathSeparator).Where(e => e.StartsWith("."));
            IEnumerable<string> extensions = new[]{ String.Empty }.Concat(pathext);
            IEnumerable<string> combinations =
                paths.SelectMany(
                    x => extensions,
                    (path, extension) => Path.Combine(path, "python" + extension)
                );
            string command = combinations.FirstOrDefault(File.Exists);

            Process = new System.Diagnostics.Process();
            Process.StartInfo.FileName = command;

            Process.StartInfo.RedirectStandardOutput = true;
            Process.OutputDataReceived += (sender, args) => {
                stdout_sb.Append(args.Data);
                stdout_sb.Append("\n");
            };

            Process.StartInfo.RedirectStandardError = true;
            Process.ErrorDataReceived += (sender, args) => {
                stderr_sb.Append(args.Data);
                stderr_sb.Append("\n");
            };
        }

        internal void AddArg(string arg)
        {
            Process.StartInfo.ArgumentList.Add(arg);
        }

        internal void Run()
        {
            Process.Start();
            hasStarted = true;
            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();
            Process.WaitForExit();
        }

        internal string GetStdout()
        {
            return stdout_sb.ToString();
        }

        internal string GetStderr()
        {
            return stderr_sb.ToString();
        }

        internal int GetExitCode()
        {
            return Process.ExitCode;
        }

        internal bool GetHasExited()
        {
            return hasStarted && Process.HasExited;
        }
    }

    internal class RVMonitorBuilder
    {
        public static int buildRVMonitorIfNeeded(string scriptDir, ref string stdout, ref string stderr)
        {
            PythonRunner p = new PythonRunner();

            string scriptPath = Path.Combine(scriptDir, "build.py");
            p.AddArg(scriptPath);

            bool success = RvmRunnerInitializer.MonitorInitializer.initialize(() => {
                p.Run();
                return 0 == p.GetExitCode();
            });

            if (p.GetHasExited())
            {
                stdout += p.GetStdout();
                stderr += p.GetStderr();

                return p.GetExitCode();
            }

            if (success)
            {
                return 0;
            }
            return -1;
        }
    }

}
