using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PChecker.IO.Debugging;
using PChecker.IO.Logging;
using PChecker.SystematicTesting;
using PChecker.Utilities;
using Debug = System.Diagnostics.Debug;

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
        /// Run a shell command in the active directory.
        /// </summary>
        private void RunCommand(string activeDirectory, string exeName, string arguments)
        {
            var psi = new ProcessStartInfo(exeName)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = activeDirectory,
                Arguments = arguments
            };

            Process proc = null;
            Task task = null;
            try
            {
                if (_checkerConfiguration.Timeout > 0)
                {
                    CancellationTokenSource.CancelAfter(
                        (_checkerConfiguration.Timeout + 30) * 1000);
                }

                if (!CancellationTokenSource.IsCancellationRequested)
                {
                    proc = new Process { StartInfo = psi };
                    proc.Start();
                    task = proc.WaitForExitAsync(CancellationTokenSource.Token);
                    task.Wait(CancellationTokenSource.Token);

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
            catch (OperationCanceledException ex)
            {
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    Logger.WriteLine($"... Checker forced timed out.");
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
                proc?.Kill();
                proc?.WaitForExit();
                task?.Dispose();
                Debug.Assert(proc?.ExitCode != null, "proc?.ExitCode != null");
                Environment.Exit((int)proc?.ExitCode);
            }
        }

        /// <summary>
        /// Runs the testing engine.
        /// </summary>
        public void Run()
        {
            // run the jar file
            var arguments = new StringBuilder();

            arguments.Append($"{_checkerConfiguration.JvmArgs} ");

            arguments.Append($"-jar {_checkerConfiguration.AssemblyToBeAnalyzed} ");

            arguments.Append($"--outdir {_checkerConfiguration.OutputFilePath}/{_checkerConfiguration.Mode.ToString()} ");

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
                        arguments.Append("--strategy symex ");
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
                
                arguments.Append($"--iterations {_checkerConfiguration.TestingIterations} ");
                
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

            if (_checkerConfiguration.IsVerbose)
            {
                Logger.WriteLine($"... Executing command: java {arguments}");
            }
            
            arguments.Append($"{_checkerConfiguration.PSymArgs} ");

            RunCommand(Directory.GetCurrentDirectory(), "java", arguments.ToString());

        }
    }
}
