// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using PChecker.IO.Debugging;
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
        /// Creates a testing process.
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
        /// Runs the testing process.
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
                Console.WriteLine("Checker found a bug.");
            }
            
            if ((!_checkerConfiguration.PerformFullExploration && TestingEngine.TestReport.NumOfFoundBugs > 0))
            {
                await EmitTraces();
            }
            
            // Closes the remote notification listener.
            if (_checkerConfiguration.IsVerbose)
            {
                Console.WriteLine($"... ### Process {_checkerConfiguration.TestingProcessId} is terminating");
            }
        }

        public static List<string> FetchTestCases(CheckerConfiguration checkerConfiguration)
        {
            Assembly assembly = TestingEngine.LoadAssembly(checkerConfiguration.AssemblyToBeAnalyzed);
            List<string> testCases = new List<string>();
            string testCaseName = checkerConfiguration.TestCaseName;
            try
            {
                var testMethods = TestMethodInfo.GetAllTestMethodsFromAssembly(assembly);
                if (checkerConfiguration.ListTestCases)
                {
                    Console.Out.WriteLine($".. List of test cases (total {testMethods.Count})");
                }
                else if (testCaseName is "")
                {
                    return [""];
                }
                else
                {
                    Console.Out.WriteLine($".. Running test cases with prefix '{testCaseName}':");
                }

                foreach (var mi in testMethods)
                {
                    if (checkerConfiguration.ListTestCases)
                    {
                        Console.Out.WriteLine($"{mi.DeclaringType.Name}");
                    }

                    else if (mi.DeclaringType.Name.StartsWith(testCaseName))
                    {
                        Console.Out.WriteLine($"{mi.DeclaringType.Name}");
                        testCases.Add(mi.DeclaringType.Name);
                    }
                }
            }
            catch
            {
                Error.ReportAndExit($"Failed to list test methods from assembly '{assembly.FullName}'");
            }
            return testCases;
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
                Console.WriteLine("... Emitting coverage report:");
                Reporter.EmitTestingCoverageReport(testReport);
            }

            if (_checkerConfiguration.DebugActivityCoverage)
            {
                Console.WriteLine("... Emitting debug coverage report:");
                Reporter.EmitTestingCoverageReport(testReport);
            }

            Console.WriteLine(testReport.GetText(_checkerConfiguration, "..."));
            
            var file = Path.GetFileNameWithoutExtension(testReport.CheckerConfiguration.AssemblyToBeAnalyzed);
            var directory = testReport.CheckerConfiguration.OutputDirectory;
            var pintPath = directory + file + "_pchecker_summary.txt";
            Console.WriteLine($"..... Writing {pintPath}");
            File.WriteAllText(pintPath, testReport.GetSummaryText(Profiler));
            
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