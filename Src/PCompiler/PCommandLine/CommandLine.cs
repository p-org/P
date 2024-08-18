using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PChecker;
using PChecker.IO.Debugging;
using PChecker.Scheduling;
using Plang.Compiler;
using Plang.Options;
using Plang.PInfer;
using Plang.Compiler.Backend;

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
                case "infer":
                    RunPInfer(args.Skip(1).ToArray());
                    break;
                case   "version":
                case  "-v":
                case  "-version":
                case "--version":
                    CommandLineOutput.WriteInfo($"P version {typeof(CommandLine).Assembly.GetName().Version}");
                    break;
                case   "help":
                case  "-h":
                case "--help":
                    PrintCommandHelp();
                    break;
                default:
                    CommandLineOutput.WriteError($"Expected (compile | check | infer) as the command input but received `{args[0]}`");
                    PrintCommandHelp();
                    break;
            }
        }

        private static void PrintCommandHelp()
        {
            CommandLineOutput.WriteInfo("================================================================================");
            CommandLineOutput.WriteInfo("The P commandline tool supports two commands (or modes): compile or check.\n");
            CommandLineOutput.WriteInfo("usage:> p command options");
            CommandLineOutput.WriteInfo("\t command : compile | check           'compile' to run the P compiler and ");
            CommandLineOutput.WriteInfo("\t                                     'check' to run the P checker on the compiled code");
            CommandLineOutput.WriteInfo("\t options:                             use `--help` or `-h` to learn more about the");
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
            Checker.Run(configuration);
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
                Error.Report($"[Internal Error]:\n {ex.Message}\n<Please report to the P team or create an issue on GitHub, Thanks!>");
                Error.Report("[PTool] unhandled exception: {0}: {1}\n Stack Trace: {2}", ex.GetType().ToString(), ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// Shutdowns any active monitors.
        /// </summary>
        private static void Shutdown()
        {
            CommandLineOutput.WriteInfo("~~ [PTool]: Thanks for using P! ~~");
        }

        public static void RunCompiler(string[] args)
        {
            var configuration = new PCompilerOptions().Parse(args);
            ICompiler compiler = new Compiler.Compiler();
            compiler.Compile(configuration);
        }

        #nullable enable
        private static object? ParseString(Type type, string v)
        {
            object? result = null;
            if (type == typeof(string))
            {
               result = v; 
            }
            else if (type == typeof(int))
            {
                if (int.TryParse(v, out var x))
                {
                    result = x;
                }
            }
            else if (type == typeof(int[]))
            {
                List<int>? x = [];
                foreach (var s in v.Split(" "))
                {
                    object? r = ParseString(typeof(int), s);
                    if (r != null)
                    {
                        x.Add((int) r);
                    }
                    else
                    {
                        x = null;
                        break;
                    }
                }
                result = x;
            }
            else if (type == typeof(string[]))
            {
                List<string>? x = [];
                foreach (var s in v.Split(" "))
                {
                    object? r = ParseString(typeof(string), s);
                    if (r != null)
                    {
                        x.Add((string) r);
                    }
                    else
                    {
                        x = null;
                        break;
                    }
                }
                result = x;
            }
            else if (type == typeof(bool))
            {
                if (v.ToLower() == "y")
                {
                    result = true;
                }
                else if (v.ToLower() == "n")
                {
                    result = false;
                }
                else
                {
                    result = null;
                }
            }
            return result;
        }
        #nullable disable

        private static object GetInputOrDefault(int step, string prompt, Type type, object defaultValue, Func<object, bool> validate, bool allowDefault = true)
        {
            object r = null;
            do {
                Console.Write($"[{step}] " + prompt + ": ");
                var line = Console.ReadLine();
                if (String.IsNullOrEmpty(line))
                {
                    if (allowDefault)
                    {
                        return defaultValue;
                    }
                }
                else
                {
                    r = ParseString(type, line);
                }
                if (r == null || !validate(r))
                {
                   CommandLineOutput.WriteWarning($"`{line}` is not a valid input. Please try again."); 
                }
            } while (r == null);
            return r;
        }

        public static bool DoPInferAction(PInferMode mode, string[] args)
        {
            if (!(mode == PInferMode.Compile || mode == PInferMode.Auto || mode == PInferMode.RunHint))
            {
                return false;
            }
            HashSet<string> pinferModeOptions = [
                "--interactive", "-i", "--compile", "--auto"
            ];
            CompilerConfiguration compileConfig = new PCompilerOptions(true).Parse(args.Where(x => {
                return !pinferModeOptions.Contains(x);
            }).ToArray());
            ICompiler compiler = new Compiler.Compiler();
            compileConfig.OutputLanguages = [CompilerOutput.PInfer];
            switch (mode)
            {
                case PInferMode.Compile: compileConfig.PInferAction = PInferAction.Compile; break;
                case PInferMode.RunHint: compileConfig.PInferAction = PInferAction.RunHint; break;
                case PInferMode.Auto:    compileConfig.PInferAction = PInferAction.Auto; break;
            }
            if ((mode == PInferMode.Compile || mode == PInferMode.RunHint) && compileConfig.HintName == null)
            {
                Error.ReportAndExit("[Error] `compile` and `run` requires a hint name");
            }
            if ((mode == PInferMode.Auto || mode == PInferMode.RunHint) && compileConfig.TraceFolder == null)
            {
                Error.ReportAndExit("[Error] `auto` and `run` requires an aggregated trace folder");
            }
            compiler.Compile(compileConfig);
            return true;
        }

        public static void RunPInfer(string[] args)
        {
            var configuration = new PInferOptions().Parse(args);
            if (configuration.Mode == PInferMode.Interactive)
            {
                // Interactive mode
                Console.WriteLine("============PInfer Interactive Miner Setup============");
                PInferOptions.WritePredicates(configuration);
                int step = 1;
                configuration.NumForallQuantifiers = (int) GetInputOrDefault(step++, "Number of preceding forall quantifiers (default: # of quantified events)", typeof(int), -1, x => ((int) x) >= 0);
                configuration.NumGuardPredicates = (int) GetInputOrDefault(step++, "Number of atomic predicates in the guard (default: 0)", typeof(int), 0, x => ((int) x) >= 0);
                int nFilters = 0;
                if (configuration.NumForallQuantifiers >= 0)
                {
                    nFilters = (int) GetInputOrDefault(step++, "Number of atomic predicates in the filter (default: 0)", typeof(int), 0, x => ((int) x) >= 0);
                }
                configuration.NumFilterPredicates = nFilters;
                // configuration.TracePaths = ((List<string>) GetInputOrDefault(step++, "Paths to trace files, separated by space (must provide at least 1 trace)", typeof(string[]), "", false)).ToArray();
                configuration.InvArity = (int) GetInputOrDefault(step++, "Arity of candidate properties (default: 2)", typeof(int), 2, x => ((int) x) >= 0);
                int pruningLevel = (int) GetInputOrDefault(step++, "Level of pruning [0-3] (default: 3, see `p infer -h` for more details)", typeof(int), 3, x => ((int) x) >= 0 && ((int) x) <= 3);
                configuration.PruningLevel = pruningLevel;
                bool hintGuards = (bool) GetInputOrDefault(step, "Include manual hints for guards? y/[n]", typeof(bool), false, x => true);
                int[] mustIncludeGuards = [];
                int[] mustIncludeFilters = [];
                List<int> defaultList = [];
                if (hintGuards) 
                {
                    // PInferOptions.WritePredicates(configuration);
                    mustIncludeGuards = ((List<int>) GetInputOrDefault(step, "Enter predicate IDs, separated by spaces", typeof(int[]), defaultList, xs => ((List<int>) xs).All(x => x >= 0))).ToArray();
                }
                configuration.MustIncludeGuard = mustIncludeGuards;
                step += 1;
                bool hintFilters = (bool) GetInputOrDefault(step, "Include manual hints for filters? y/[n]", typeof(bool), false, x => true);
                if (hintFilters)
                {
                    // PInferOptions.WritePredicates(configuration);
                    mustIncludeFilters = ((List<int>) GetInputOrDefault(step, "Enter predicate IDs, separated by spaces", typeof(int[]), defaultList, xs => ((List<int>) xs).All(x => x >= 0))).ToArray();
                }
                configuration.MustIncludeFilter = mustIncludeFilters;
                step += 1;
                configuration.SkipTrivialCombinations = (bool) GetInputOrDefault(step++, "Skip trivial guard-filter-terms combinations? [y]/n", typeof(bool), true, x => true);
            }
            // fall-through
            if (!DoPInferAction(configuration.Mode, args))
            {
                PInfer.PInferInvoke.invokeMain(configuration);
            }
        }
    }
}
