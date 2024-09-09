using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PChecker.IO.Debugging;
using PChecker.IO.Logging;

namespace PChecker.ExhaustiveSearch
{
    /// <summary>
    /// Exhaustive engine that can run a controlled concurrency exhaustive search using
    /// a specified checkerConfiguration.
    /// </summary>
    public class ExhaustiveEngine
    {
        /// <summary>
        /// CheckerConfiguration.
        /// </summary>
        private readonly CheckerConfiguration _checkerConfiguration;

        /// <summary>
        /// Logger.
        /// </summary>
        private TextWriter Logger;

        /// <summary>
        /// The testing task cancellation token source.
        /// </summary>
        private readonly CancellationTokenSource CancellationTokenSource;

        /// <summary>
        /// Creates a new exhaustive engine.
        /// </summary>
        public static ExhaustiveEngine Create(CheckerConfiguration checkerConfiguration)
        {
            return new ExhaustiveEngine(checkerConfiguration);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExhaustiveEngine"/> class.
        /// </summary>
        private ExhaustiveEngine(CheckerConfiguration checkerConfiguration)
        {
            _checkerConfiguration = checkerConfiguration;
            Logger = new ConsoleLogger();
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Creates the set of arguments for the exhaustive engine.
        /// </summary>
        private String CreateArguments()
        {
            var arguments = new StringBuilder();

            arguments.Append($"{_checkerConfiguration.JvmArgs} ");

            arguments.Append($"-jar {_checkerConfiguration.AssemblyToBeAnalyzed} ");

            arguments.Append($"--outdir {_checkerConfiguration.OutputPath}/{_checkerConfiguration.Mode.ToString()} ");

            if (!string.IsNullOrEmpty(_checkerConfiguration.TestCaseName))
            {
                arguments.Append($"--testcase {_checkerConfiguration.TestCaseName} ");
            }

            if (_checkerConfiguration.SchedulingStrategy is "replay")
            {
                arguments.Append($"--replay {_checkerConfiguration.ScheduleFile} ");
            }
            else
            {
                switch (_checkerConfiguration.Mode)
                {
                    case CheckerMode.Verification:
                        arguments.Append("--strategy symbolic ");
                        break;
                    case CheckerMode.Coverage:
                        arguments.Append($"--strategy {_checkerConfiguration.SchedulingStrategy} ");
                        break;
                    default:
                        Error.ReportAndExit($"Unexpected checker mode: {_checkerConfiguration.Mode}.");
                        break;
                }

                arguments.Append($"--timeout {_checkerConfiguration.Timeout} ");

                arguments.Append($"--memout {_checkerConfiguration.MemoryLimit} ");

                if (_checkerConfiguration.IsVerbose)
                {
                    arguments.Append($"--verbose 1 ");
                }

                arguments.Append($"--schedules {_checkerConfiguration.TestingIterations} ");

                arguments.Append($"--max-steps {_checkerConfiguration.MaxUnfairSchedulingSteps} ");

                if (_checkerConfiguration.ConsiderDepthBoundHitAsBug)
                {
                    arguments.Append("--fail-on-maxsteps ");
                }

                if (_checkerConfiguration.RandomGeneratorSeed.HasValue)
                {
                    arguments.Append($"--seed {_checkerConfiguration.RandomGeneratorSeed.Value} ");
                }
            }

            arguments.Append($"{_checkerConfiguration.PSymArgs} ");

            return arguments.ToString();
        }

        /// <summary>
        /// Creates the process from the shell command
        /// </summary>
        private Process CreateProcess(string activeDirectory, string exeName, string arguments)
        {
            var psi = new ProcessStartInfo(exeName)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = activeDirectory,
                Arguments = arguments
            };

            return new Process { StartInfo = psi };
        }

        /// <summary>
        /// Wait for task to finish
        /// </summary>
        private async Task CreateTaskFromProcess(Process proc)
        {
            if (proc != null)
            {
                if (_checkerConfiguration.Timeout > 0)
                {
                    CancellationTokenSource.CancelAfter(
                        (_checkerConfiguration.Timeout + 30) * 1000);
                }

                await  proc.WaitForExitAsync(CancellationTokenSource.Token);

                switch (proc.ExitCode)
                {
                    case 0:
                        Logger.WriteLine($"... Checker run finished.");
                        break;
                    case 2:
                        Logger.WriteLine($"... Checker found a bug.");
                        break;
                    case 3:
                        Logger.WriteLine($"... Checker timed out.");
                        break;
                    case 4:
                        Logger.WriteLine($"... Checker ran out of memory.");
                        break;
                    default:
                        Logger.WriteLine($"... Checker run exited with code {proc.ExitCode}.");
                        break;
                }
            }
        }

        private void Cleanup(Process proc, Task task)
        {
            proc?.Kill(true);
            proc?.WaitForExit();
            task?.Dispose();
        }

        /// <summary>
        /// Runs the exhaustive engine.
        /// </summary>
        public void Run()
        {
            var arguments = CreateArguments();

            if (_checkerConfiguration.IsVerbose)
            {
                Logger.WriteLine($"... Executing command: java {arguments}");
            }

            var proc = CreateProcess(Directory.GetCurrentDirectory(), "java", arguments);
            proc.Start();
            Task task = CreateTaskFromProcess(proc);
            bool interrupted = false;

            // For graceful shutdown, trap unload event
            AppDomain.CurrentDomain.ProcessExit += delegate
            {
                interrupted = true;
                CancellationTokenSource.Cancel();
                Cleanup(proc, task);
                Logger.WriteLine($"... Checker run terminated.");
            };

            Console.CancelKeyPress += delegate
            {
                interrupted = true;
                CancellationTokenSource.Cancel();
                Cleanup(proc, task);
                Logger.WriteLine($"... Checker run cancelled by user.");
            };

            try
            {
                task.Wait(CancellationTokenSource.Token);
            }
            catch (OperationCanceledException ex)
            {
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    if (!interrupted)
                    {
                        Logger.WriteLine($"... Checker run forcefully cancelled on timeout.");
                    }
                    Error.Report($"{ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"... Checker failed due to an internal error: {ex}");
                Error.Report($"{ex.Message}");
            }
            finally
            {
                Cleanup(proc, task);
                Environment.Exit(proc.ExitCode);
            }
        }
    }
}