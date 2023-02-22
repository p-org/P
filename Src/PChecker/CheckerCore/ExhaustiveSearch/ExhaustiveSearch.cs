using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
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
        /// Set of callbacks to invoke at the end
        /// of each iteration.
        /// </summary>
        private readonly ISet<Action<int>> PerIterationCallbacks;

        /// <summary>
        /// The installed logger.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/core/logging" >Logging</see> for more information.
        /// </remarks>
        private TextWriter Logger;

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
        }

        /// <summary>
        /// Run a shell command in the active directory.
        /// </summary>
        private static int RunCommand(string activeDirectory, string exeName, string arguments)
        {
            var psi = new ProcessStartInfo(exeName)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = activeDirectory,
                Arguments = arguments
            };

            var proc = new Process { StartInfo = psi };
            proc.Start();
            proc.WaitForExit();
            return proc.ExitCode;
        }

        /// <summary>
        /// Runs the testing engine.
        /// </summary>
        public void Run()
        {
            // run the jar file
            var arguments = new StringBuilder();

            arguments.Append($"-jar {_checkerConfiguration.AssemblyToBeAnalyzed} ");

            if (!string.IsNullOrEmpty(_checkerConfiguration.OutputFilePath))
            {
                arguments.Append($"--outdir {_checkerConfiguration.OutputFilePath} ");
            }

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
                    case CheckerMode.Verify:
                        arguments.Append("--strategy symex ");
                        break;
                    case CheckerMode.Cover:
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

            int exitCode = -1;
            exitCode = RunCommand(Directory.GetCurrentDirectory(), "java", arguments.ToString());
            
            if (exitCode != 0)
            {
                Error.Report($"Checker run exited with code {exitCode}.");
                Environment.Exit(exitCode);
            }
            else
            {
                Logger.WriteLine($"... Checker run finished.");
            }
        }
    }
}
