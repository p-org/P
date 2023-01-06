// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using PChecker.IO;
using PChecker.SystematicTesting;
using PChecker.Instrumentation;
using PChecker.Scheduling;
using PChecker.Testing;
using PChecker.Utilities;

namespace PChecker
{
    /// <summary>
    /// Entry point to the Coyote tool.
    /// </summary>
    internal class Runner
    {
        private static Configuration Configuration;

        private static TextWriter StdOut;
        private static TextWriter StdError;

        private static readonly object ConsoleLock = new object();

        private static void Main(string[] args)
        {
            // Save these so we can force output to happen even if TestingProcess has re-routed it.
            StdOut = Console.Out;
            StdError = Console.Error;

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Console.CancelKeyPress += OnProcessCanceled;

            // Parses the command line options to get the configuration.
            Configuration = new CommandLineOptions().Parse(args);

            switch (Configuration.ToolCommand.ToLower())
            {
                case "test":
                    RunTest();
                    break;
                case "replay":
                    ReplayTest();
                    break;
            }
        }

        private static void RunTest()
        {
            if (Configuration.RunAsParallelBugFindingTask)
            {
                TestingProcess testingProcess = TestingProcess.Create(Configuration);
                testingProcess.Run();
                return;
            }

            if (Configuration.ReportCodeCoverage || Configuration.ReportActivityCoverage)
            {
                // This has to be here because both forms of coverage require it.
                CodeCoverageInstrumentation.SetOutputDirectory(Configuration, makeHistory: true);
            }

            if (Configuration.ReportCodeCoverage)
            {
#if NETFRAMEWORK
                // Instruments the program under test for code coverage.
                CodeCoverageInstrumentation.Instrument(Configuration);

                // Starts monitoring for code coverage.
                CodeCoverageMonitor.Start(Configuration);
#endif
            }

            Console.WriteLine(". Testing " + Configuration.AssemblyToBeAnalyzed);
            if (!string.IsNullOrEmpty(Configuration.TestCaseName))
            {
                Console.WriteLine("... Method {0}", Configuration.TestCaseName);
            }

            // Creates and runs the testing process scheduler.
            TestingProcessScheduler.Create(Configuration).Run();

            Console.WriteLine(". Done");
        }

        private static void ReplayTest()
        {
            // Set some replay specific options.
            Configuration.SchedulingStrategy = "replay";
            Configuration.EnableColoredConsoleOutput = true;
            Configuration.DisableEnvironmentExit = false;

            Console.WriteLine($". Replaying {Configuration.ScheduleFile}");
            TestingEngine engine = TestingEngine.Create(Configuration);
            engine.Run();
            Console.WriteLine(engine.GetReport());
        }

        /// <summary>
        /// Callback invoked when the current process terminates.
        /// </summary>
        private static void OnProcessExit(object sender, EventArgs e) => Shutdown();

        /// <summary>
        /// Callback invoked when the current process is canceled.
        /// </summary>
        private static void OnProcessCanceled(object sender, EventArgs e)
        {
            if (!TestingProcessScheduler.IsProcessCanceled)
            {
                TestingProcessScheduler.IsProcessCanceled = true;
                Shutdown();
            }
        }

        /// <summary>
        /// Callback invoked when an unhandled exception occurs.
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            ReportUnhandledException((Exception)args.ExceptionObject);
            Environment.Exit(1);
        }

        private static void ReportUnhandledException(Exception ex)
        {
            Console.SetOut(StdOut);
            Console.SetError(StdError);

            PrintException(ex);
            for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
            {
                PrintException(inner);
            }
        }

        private static void PrintException(Exception ex)
        {
            lock (ConsoleLock)
            {
                if (ex is ExecutionCanceledException)
                {
                    Error.Report("[PChecker] unhandled exception: {0}: {1}", ex.GetType().ToString(),
                        "This can mean you have a code path that is not controlled by the runtime that threw an unhandled exception.");
                    StdOut.WriteLine(ex.StackTrace);
                }
                else
                {
                    Error.Report("[PChecker] unhandled exception: {0}: {1}", ex.GetType().ToString(), ex.Message);
                    StdOut.WriteLine(ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Shutdowns any active monitors.
        /// </summary>
        private static void Shutdown()
        {
            StdOut.WriteLine("[PChecker]: Shutdown ..");
#if NETFRAMEWORK
            if (Configuration != null && Configuration.ReportCodeCoverage && CodeCoverageMonitor.IsRunning)
            {
                Console.WriteLine(". Shutting down the code coverage monitor, this may take a few seconds...");

                // Stops monitoring for code coverage.
                CodeCoverageMonitor.Stop();
                CodeCoverageInstrumentation.Restore();
            }
#endif
        }
    }
}
