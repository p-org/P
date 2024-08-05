// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using PChecker.SystematicTesting;
using PChecker.Utilities;

namespace PChecker.Testing
{
    /// <summary>
    /// A testing process, this can also be the client side of a multi-process test
    /// </summary>
    public class TestingProcess
    {

        /// <summary>
        /// A name for the test client
        /// </summary>
        private readonly string Name = "PCheckerProcess";

        /// <summary>
        /// CheckerConfiguration.
        /// </summary>
        private readonly CheckerConfiguration _checkerConfiguration;

        /// <summary>
        /// The testing engine associated with
        /// this testing process.
        /// </summary>
        private readonly TestingEngine TestingEngine;

        /// <summary>
        /// Set if ctrl-c or ctrl-break occurred.
        /// </summary>
        public static bool IsProcessCanceled;
        
        /// <summary>
        /// The testing profiler.
        /// </summary>
        private readonly Profiler Profiler;
        
        /// <summary>
        /// Creates a Coyote testing process.
        /// </summary>
        public static TestingProcess Create(CheckerConfiguration checkerConfiguration)
        {
            return new TestingProcess(checkerConfiguration);
        }
        
        /// <summary>
        /// Get the current test report.
        /// </summary>
        public TestReport GetTestReport()
        {
            return TestingEngine.TestReport.Clone();
        }

        // Gets a handle to the standard output and error streams.
        private readonly TextWriter StdOut = Console.Out;

        /// <summary>
        /// Runs the Coyote testing process.
        /// </summary>
        public void Run()
        {
            Profiler.StartMeasuringExecutionTime();
            RunAsync().Wait();
            Profiler.StopMeasuringExecutionTime();
            if (!IsProcessCanceled)
            {
                // Merges and emits the test report.
                EmitTestReport();
            }
        }

        private async Task RunAsync()
        {
            TestingEngine.Run();

            Console.SetOut(StdOut);

            if (!_checkerConfiguration.PerformFullExploration &&
                TestingEngine.TestReport.NumOfFoundBugs > 0)
            {
                Console.WriteLine($"Checker found a bug.");
            }

            // we want the graph generation even if doing full exploration.
            if ((!_checkerConfiguration.PerformFullExploration && TestingEngine.TestReport.NumOfFoundBugs > 0) ||
                (_checkerConfiguration.IsDgmlGraphEnabled && !_checkerConfiguration.IsDgmlBugGraph))
            {
                await EmitTraces();
            }

            // Closes the remote notification listener.
            if (_checkerConfiguration.IsVerbose)
            {
                Console.WriteLine($"... ### Process {_checkerConfiguration.TestingProcessId} is terminating");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestingProcess"/> class.
        /// </summary>
        private TestingProcess(CheckerConfiguration checkerConfiguration)
        {
            Name = Name + "." + checkerConfiguration.TestingProcessId;

            if (checkerConfiguration.RandomGeneratorSeed.HasValue)
            {
                checkerConfiguration.RandomGeneratorSeed = checkerConfiguration.RandomGeneratorSeed.Value +
                                                           (673 * checkerConfiguration.TestingProcessId);
            }

            checkerConfiguration.EnableColoredConsoleOutput = true;
            
            _checkerConfiguration = checkerConfiguration;
            TestingEngine = TestingEngine.Create(_checkerConfiguration);
            Profiler = new Profiler();
            IsProcessCanceled = false;
        }


        /// <summary>
        /// Emits the testing traces.
        /// </summary>
        private Task EmitTraces()
        {
            var file = Path.GetFileNameWithoutExtension(_checkerConfiguration.AssemblyToBeAnalyzed);
            file += "_" + _checkerConfiguration.TestingProcessId;

            Console.WriteLine($"... Emitting traces:");
            TestingEngine.TryEmitTraces(_checkerConfiguration.OutputDirectory, file);
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Emits the test report.
        /// </summary>
        private void EmitTestReport()
        {
            TestReport testReport = GetTestReport();
            if (testReport == null)
            {
                Environment.ExitCode = (int)ExitCode.InternalError;
                return;
            }

            if (_checkerConfiguration.ReportActivityCoverage)
            {
                Console.WriteLine($"... Emitting coverage report:");
                Reporter.EmitTestingCoverageReport(testReport);
            }

            if (_checkerConfiguration.DebugActivityCoverage)
            {
                Console.WriteLine($"... Emitting debug coverage report:");
                Reporter.EmitTestingCoverageReport(testReport);
            }

            Console.WriteLine(testReport.GetText(_checkerConfiguration, "..."));
            Console.WriteLine($"... Elapsed {Profiler.GetElapsedTime()} sec.");

            if (testReport.InternalErrors.Count > 0)
            {
                Environment.ExitCode = (int)ExitCode.InternalError;
            }
            else if (testReport.NumOfFoundBugs > 0)
            {
                Environment.ExitCode = (int)ExitCode.BugFound;
            }
            else
            {
                Environment.ExitCode = (int)ExitCode.Success;
            }
        }
    }
}