using System;
using System.IO;
using System.Linq;
using PChecker;
using PChecker.ExhaustiveSearch;
using PChecker.SystematicTesting;
using PChecker.Instrumentation;
using PChecker.IO.Debugging;
using PChecker.Scheduling;
using Plang.Compiler;
using Plang.Options;

namespace Plang
{
    public static class CommandLine
    {

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

            // get the command
            if (args.Length == 0)
            {
                PrintCommandHelp();
                return;
            }
            
            
            switch (args[0].ToLower())
            {
                case "compile":
                    RunCompiler(args.Skip(1).ToArray());
                    break;
                case "check":
                    RunChecker(args.Skip(1).ToArray());
                    break;
                case   "help":
                case  "-h":
                case "--help":
                    PrintCommandHelp();
                    break;
                default:
                    CommandLineOutput.WriteError($"Expected (compile | check) as the command input but received `{args[0]}`");
                    PrintCommandHelp();
                    break;
            }
        }

        private static void PrintCommandHelp()
        {
            CommandLineOutput.WriteInfo("================================================================================");
            CommandLineOutput.WriteInfo("The P commandline tool supports two commands (modes): compile or check.\n");
            CommandLineOutput.WriteInfo("usage:> p command command-options");
            CommandLineOutput.WriteInfo("\t command :: compile | check           `compile` to run the P compiler and ");
            CommandLineOutput.WriteInfo("\t                                      `check` to run the P checker on the compiled code");
            CommandLineOutput.WriteInfo("\t command-options                      use `--help` or `-h` to learn more about the");
            CommandLineOutput.WriteInfo("\t                                      corresponding command options");
            CommandLineOutput.WriteInfo("\t -----------------------------------------------------------------------");
            CommandLineOutput.WriteInfo("\t p compile --help                     for P compiler help");
            CommandLineOutput.WriteInfo("\t p check --help                       for P checker help");
            CommandLineOutput.WriteInfo("================================================================================");
        }

        private static void RunChecker(string[] args)
        {
            // Parses the command line options to get the checkerConfiguration.
            var configuration = new PCheckerOptions().Parse(args);
            
            // if the replay option is passed then we ignore all the other options and replay the schedule
            if (configuration.SchedulingStrategy == "replay")
            {
                CommandLineOutput.WriteInfo($"Replay option is used, checker is ignoring all other parameters and using the {configuration.ScheduleFile} to replay the schedule");
                CommandLineOutput.WriteInfo($"... Replaying {configuration.ScheduleFile}");

                switch (configuration.Mode)
                {
                    case CheckerMode.BugFinding:
                    {
                        var engine = TestingEngine.Create(configuration);
                        engine.Run();
                        CommandLineOutput.WriteInfo(engine.GetReport());
                    }
                        break;
                    case CheckerMode.Verification:
                    case CheckerMode.Coverage:
                        ExhaustiveEngine.Create(configuration).Run();
                        break;
                    default:
                        Error.Report($"[PTool] Checker with {configuration.Mode} mode is currently unsupported.");
                        break;
                }
            }
            else
            {
                // This has to be here because all checker modes now require this to set up output directory.
                CodeCoverageInstrumentation.SetOutputDirectory(configuration, makeHistory: true);

                Console.WriteLine(".. Checking " + configuration.AssemblyToBeAnalyzed);

                // Creates and runs the testing process scheduler.
                switch (configuration.Mode)
                {
                    case CheckerMode.BugFinding:
                        TestingProcessScheduler.Create(configuration).Run();
                        break;
                    case CheckerMode.Verification:
                    case CheckerMode.Coverage:
                        ExhaustiveEngine.Create(configuration).Run();
                        break;
                    default:
                        Error.Report($"[PTool] Checker with {configuration.Mode} mode is currently unsupported.");
                        break;
                }
            
                Console.WriteLine(". Done");
            }
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
            }
        }

        /// <summary>
        /// Shutdowns any active monitors.
        /// </summary>
        private static void Shutdown()
        {
            CommandLineOutput.WriteInfo("[PTool]: Thanks for using P!");
        }
        
        public static void RunCompiler(string[] args)
        {
            var configuration = new PCompilerOptions().Parse(args);
            ICompiler compiler = new Compiler.Compiler();
            compiler.Compile(configuration);
        }
    }
}