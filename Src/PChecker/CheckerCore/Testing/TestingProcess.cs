// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using PChecker.SystematicTesting;

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
            RunAsync().Wait();
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

            if (checkerConfiguration.SchedulingStrategy is "portfolio")
            {
                TestingPortfolio.ConfigureStrategyForCurrentProcess(checkerConfiguration);
            }

            if (checkerConfiguration.RandomGeneratorSeed.HasValue)
            {
                checkerConfiguration.RandomGeneratorSeed = checkerConfiguration.RandomGeneratorSeed.Value +
                                                           (673 * checkerConfiguration.TestingProcessId);
            }

            checkerConfiguration.EnableColoredConsoleOutput = true;

            _checkerConfiguration = checkerConfiguration;
            TestingEngine = TestingEngine.Create(_checkerConfiguration);
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
    }
}