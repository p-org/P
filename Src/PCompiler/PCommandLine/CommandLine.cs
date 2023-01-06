using System;
using System.IO;
using Microsoft.CSharp.RuntimeBinder;
using PChecker;
using PChecker.IO;
using PChecker.PChecker.Compiler;
using PChecker.SystematicTesting;
using PChecker.Instrumentation;
using PChecker.Scheduling;
using PChecker.Testing;
using Plang.Compiler;

namespace Plang
{
    public static class CommandLine
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
                case "compile":
                    RunCompiler();
                    break;
                case "check":
                    RunChecker();
                    break;
                case "replay":
                    ReplayTest();
                    break;
            }
        }

        private static void RunChecker()
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
                Error.Report($"<Internal Error>:\n {ex.Message}\n<Please report to the P team or create an issue on GitHub, Thanks!>");
                Error.Report("[PTool] unhandled exception: {0}: {1}", ex.GetType().ToString(), ex.Message);
                StdOut.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Shutdowns any active monitors.
        /// </summary>
        private static void Shutdown()
        {
            StdOut.WriteLine("[PTool]: Shutdown ..");
        }
        
        public static void RunCompiler()
        {
            CompilationJob job = null;
            ICompiler compiler = new Compiler.Compiler();
            compiler.Compile(job);
        }
    }
}