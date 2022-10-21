// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Utilities
{
    internal sealed class CommandLineOptions
    {
        /// <summary>
        /// The command line parser to use.
        /// </summary>
        private readonly CommandLineArgumentParser Parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineOptions"/> class.
        /// </summary>
        internal CommandLineOptions()
        {
            this.Parser = new CommandLineArgumentParser("Coyote",
                "The Coyote tool enables you to systematically test a specified Coyote test, generate " +
                "a reproducible bug-trace if a bug is found, and replay a bug-trace using the VS debugger.");

            var basicOptions = this.Parser.GetOrCreateGroup("Basic", "Basic options");
            var commandArg = basicOptions.AddPositionalArgument("command", "The operation perform (test, replay)");
            commandArg.AllowedValues = new List<string>(new string[] { "test", "replay" });
            basicOptions.AddPositionalArgument("path", "Path to the Coyote program to test");
            basicOptions.AddArgument("method", "m", "Suffix of the test method to execute");

            var basicGroup = this.Parser.GetOrCreateGroup("Basic", "Basic options");
            basicGroup.AddArgument("timeout", "t", "Timeout in seconds (disabled by default)", typeof(uint));
            basicGroup.AddArgument("outdir", "o", "Dump output to directory x (absolute path or relative to current directory");
            basicGroup.AddArgument("verbose", "v", "Enable verbose log output during testing", typeof(bool));
            basicGroup.AddArgument("debug", "d", "Enable debugging", typeof(bool)).IsHidden = true;
            basicGroup.AddArgument("break", "b", "Attaches the debugger and also adds a breakpoint when an assertion fails (disabled during parallel testing)", typeof(bool));
            basicGroup.AddArgument("version", null, "Show tool version", typeof(bool));

            var testingGroup = this.Parser.GetOrCreateGroup("testingGroup", "Systematic testing options");
            testingGroup.DependsOn = new CommandLineArgumentDependency() { Name = "command", Value = "test" };
            testingGroup.AddArgument("iterations", "i", "Number of schedules to explore for bugs", typeof(uint));
            testingGroup.AddArgument("max-steps", "ms", @"Max scheduling steps to be explored during systematic testing (by default 10,000 unfair and 100,000 fair steps).
You can provide one or two unsigned integer values", typeof(uint)).IsMultiValue = true;
            testingGroup.AddArgument("timeout-delay", null, "Controls the frequency of timeouts by built-in timers (not a unit of time)", typeof(uint));
            testingGroup.AddArgument("fail-on-maxsteps", null, "Consider it a bug if the test hits the specified max-steps", typeof(bool));
            testingGroup.AddArgument("liveness-temperature-threshold", null, "Specify the liveness temperature threshold is the liveness temperature value that triggers a liveness bug", typeof(uint));
            testingGroup.AddArgument("parallel", "p", "Number of parallel testing processes (the default '0' runs the test in-process)", typeof(uint));
            testingGroup.AddArgument("sch-random", null, "Choose the random scheduling strategy (this is the default)", typeof(bool));
            testingGroup.AddArgument("sch-probabilistic", "sp", "Choose the probabilistic scheduling strategy with given probability for each scheduling decision where the probability is " +
                "specified as the integer N in the equation 0.5 to the power of N.  So for N=1, the probability is 0.5, for N=2 the probability is 0.25, N=3 you get 0.125, etc.", typeof(uint));
            testingGroup.AddArgument("sch-pct", null, "Choose the PCT scheduling strategy with given maximum number of priority switch points", typeof(uint));
            testingGroup.AddArgument("sch-fairpct", null, "Choose the fair PCT scheduling strategy with given maximum number of priority switch points", typeof(uint));
            testingGroup.AddArgument("sch-portfolio", null, "Choose the portfolio scheduling strategy", typeof(bool));

            var replayOptions = this.Parser.GetOrCreateGroup("replayOptions", "Replay and debug options");
            replayOptions.DependsOn = new CommandLineArgumentDependency() { Name = "command", Value = "replay" };
            replayOptions.AddPositionalArgument("schedule", "Schedule file to replay");

            var coverageGroup = this.Parser.GetOrCreateGroup("coverageGroup", "Code and activity coverage options");
            var coverageArg = coverageGroup.AddArgument("coverage", "c", @"Generate code coverage statistics (via VS instrumentation) with zero or more values equal to:
 code: Generate code coverage statistics (via VS instrumentation)
 activity: Generate activity (state machine, event, etc.) coverage statistics
 activity-debug: Print activity coverage statistics with debug info", typeof(string));
            coverageArg.AllowedValues = new List<string>(new string[] { string.Empty, "code", "activity", "activity-debug" });
            coverageArg.IsMultiValue = true;
            coverageGroup.AddArgument("instrument", "instr", "Additional file spec(s) to instrument for code coverage (wildcards supported)", typeof(string));
            coverageGroup.AddArgument("instrument-list", "instr-list", "File containing the paths to additional file(s) to instrument for code " +
                "coverage, one per line, wildcards supported, lines starting with '//' are skipped", typeof(string));

            var advancedGroup = this.Parser.GetOrCreateGroup("advancedGroup", "Advanced options");
            advancedGroup.AddArgument("explore", null, "Keep testing until the bound (e.g. iteration or time) is reached", typeof(bool));
            advancedGroup.AddArgument("seed", null, "Specify the random value generator seed", typeof(uint));
            advancedGroup.AddArgument("wait-for-testing-processes", null, "Wait for testing processes to start (default is to launch them)", typeof(bool));
            advancedGroup.AddArgument("testing-scheduler-ipaddress", null, "Specify server ip address and optional port (default: 127.0.0.1:0))", typeof(string));
            advancedGroup.AddArgument("testing-scheduler-endpoint", null, "Specify a name for the server (default: CoyoteTestScheduler)", typeof(string));
            advancedGroup.AddArgument("graph-bug", null, "Output a DGML graph of the iteration that found a bug", typeof(bool));
            advancedGroup.AddArgument("graph", null, "Output a DGML graph of all test iterations whether a bug was found or not", typeof(bool));
            advancedGroup.AddArgument("xml-trace", null, "Specify a filename for XML runtime log output to be written to", typeof(bool));
            advancedGroup.AddArgument("actor-runtime-log", null, "Specify an additional custom logger using fully qualified name: 'fullclass,assembly'", typeof(string));

            // Hidden options (for debugging or experimentation only).
            var hiddenGroup = this.Parser.GetOrCreateGroup("hiddenGroup", "Hidden Options");
            hiddenGroup.IsHidden = true;
            hiddenGroup.AddArgument("prefix", null, "Safety prefix bound", typeof(int)); // why is this needed, seems to just be an override for MaxUnfairSchedulingSteps?
            hiddenGroup.AddArgument("run-as-parallel-testing-task", null, null, typeof(bool));
            hiddenGroup.AddArgument("testing-process-id", null, "The id of the controlling TestingProcessScheduler", typeof(uint));
            // hiddenGroup.AddArgument("sch-dfs", null, "Choose the DFS scheduling strategy", typeof(bool)); // currently broken, re-enable when it's fixed
            hiddenGroup.AddArgument("parallel-debug", "pd", "Used with --parallel to put up a debugger prompt on each child process", typeof(bool));
        }

        /// <summary>
        /// Parses the command line options and returns a configuration.
        /// </summary>
        /// <returns>The Configuration object populated with the parsed command line options.</returns>
        internal Configuration Parse(string[] args)
        {
            var configuration = Configuration.Create();

            try
            {
                var result = this.Parser.ParseArguments(args);
                foreach (var arg in result)
                {
                    UpdateConfigurationWithParsedArgument(configuration, arg);
                }

                SanitizeConfiguration(configuration);
            }
            catch (CommandLineException ex)
            {
                if ((from arg in ex.Result where arg.LongName == "version" select arg).Any())
                {
                    WriteVersion();
                    Environment.Exit(1);
                }
                else
                {
                    this.Parser.PrintHelp(Console.Out);
                    Error.ReportAndExit(ex.Message);
                }
            }
            catch (Exception ex)
            {
                this.Parser.PrintHelp(Console.Out);
                Error.ReportAndExit(ex.Message);
            }

            return configuration;
        }

        /// <summary>
        /// Updates the configuration with the specified parsed argument.
        /// </summary>
        private static void UpdateConfigurationWithParsedArgument(Configuration configuration, CommandLineArgument option)
        {
            switch (option.LongName)
            {
                case "command":
                    configuration.ToolCommand = (string)option.Value;
                    break;
                case "outdir":
                    configuration.OutputFilePath = (string)option.Value;
                    break;
                case "verbose":
                    configuration.IsVerbose = true;
                    break;
                case "debug":
                    configuration.EnableDebugging = true;
                    Debug.IsEnabled = true;
                    break;
                case "timeout":
                    configuration.Timeout = (int)(uint)option.Value;
                    break;
                case "path":
                    configuration.AssemblyToBeAnalyzed = (string)option.Value;
                    break;
                case "method":
                    configuration.TestMethodName = (string)option.Value;
                    break;
                case "seed":
                    configuration.RandomGeneratorSeed = (uint)option.Value;
                    break;
                case "sch-random":
                case "sch-dfs":
                case "sch-portfolio":
                    configuration.SchedulingStrategy = option.LongName.Substring(4);
                    break;
                case "sch-probabilistic":
                case "sch-pct":
                case "sch-fairpct":
                    configuration.SchedulingStrategy = option.LongName.Substring(4);
                    configuration.StrategyBound = (int)(uint)option.Value;
                    break;
                case "schedule":
                    {
                        string filename = (string)option.Value;
                        string extension = System.IO.Path.GetExtension(filename);
                        if (!extension.Equals(".schedule"))
                        {
                            Error.ReportAndExit("Please give a valid schedule file " +
                                "'--replay x', where 'x' has extension '.schedule'.");
                        }

                        configuration.ScheduleFile = filename;
                    }

                    break;
                case "version":
                    WriteVersion();
                    Environment.Exit(1);
                    break;
                case "break":
                    configuration.AttachDebugger = true;
                    break;
                case "iterations":
                    configuration.TestingIterations = (int)(uint)option.Value;
                    break;
                case "parallel":
                    configuration.ParallelBugFindingTasks = (uint)option.Value;
                    break;
                case "parallel-debug":
                    configuration.ParallelDebug = true;
                    break;
                case "wait-for-testing-processes":
                    configuration.WaitForTestingProcesses = true;
                    break;
                case "testing-scheduler-ipaddress":
                    {
                        var ipAddress = (string)option.Value;
                        int port = 0;
                        if (ipAddress.Contains(":"))
                        {
                            string[] parts = ipAddress.Split(':');
                            if (parts.Length != 2 || !int.TryParse(parts[1], out port))
                            {
                                Error.ReportAndExit("Please give a valid port number for --testing-scheduler-ipaddress option");
                            }

                            ipAddress = parts[0];
                        }

                        if (!IPAddress.TryParse(ipAddress, out _))
                        {
                            Error.ReportAndExit("Please give a valid ip address for --testing-scheduler-ipaddress option");
                        }

                        configuration.TestingSchedulerIpAddress = ipAddress + ":" + port;
                    }

                    break;
                case "run-as-parallel-testing-task":
                    configuration.RunAsParallelBugFindingTask = true;
                    break;
                case "testing-scheduler-endpoint":
                    configuration.TestingSchedulerEndPoint = (string)option.Value;
                    break;
                case "testing-process-id":
                    configuration.TestingProcessId = (uint)option.Value;
                    break;
                case "graph":
                    configuration.IsDgmlGraphEnabled = true;
                    configuration.IsDgmlBugGraph = false;
                    break;
                case "graph-bug":
                    configuration.IsDgmlGraphEnabled = true;
                    configuration.IsDgmlBugGraph = true;
                    break;
                case "xml-trace":
                    configuration.IsXmlLogEnabled = true;
                    break;
                case "actor-runtime-log":
                    configuration.CustomActorRuntimeLogType = (string)option.Value;
                    break;
                case "explore":
                    configuration.PerformFullExploration = true;
                    break;
                case "coverage":
                    if (option.Value == null)
                    {
                        configuration.ReportCodeCoverage = true;
                        configuration.ReportActivityCoverage = true;
                    }
                    else
                    {
                        foreach (var item in (string[])option.Value)
                        {
                            switch (item)
                            {
                                case "code":
                                    configuration.ReportCodeCoverage = true;
                                    break;
                                case "activity":
                                    configuration.ReportActivityCoverage = true;
                                    break;
                                case "activity-debug":
                                    configuration.ReportActivityCoverage = true;
                                    configuration.DebugActivityCoverage = true;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    break;
                case "instrument":
                case "instrument-list":
                    configuration.AdditionalCodeCoverageAssemblies[(string)option.Value] = false;
                    break;
                case "timeout-delay":
                    configuration.TimeoutDelay = (uint)option.Value;
                    break;
                case "max-steps":
                    {
                        uint[] values = (uint[])option.Value;
                        if (values.Length > 2)
                        {
                            Error.ReportAndExit("Invalid number of options supplied via '--max-steps'.");
                        }

                        uint i = values[0];
                        uint j;
                        if (values.Length == 2)
                        {
                            j = values[1];
                            configuration.UserExplicitlySetMaxFairSchedulingSteps = true;
                        }
                        else
                        {
                            j = 10 * i;
                        }

                        configuration.MaxUnfairSchedulingSteps = (int)i;
                        configuration.MaxFairSchedulingSteps = (int)j;
                    }

                    break;
                case "fail-on-maxsteps":
                    configuration.ConsiderDepthBoundHitAsBug = true;
                    break;
                case "prefix":
                    configuration.SafetyPrefixBound = (int)option.Value;
                    break;
                case "liveness-temperature-threshold":
                    configuration.LivenessTemperatureThreshold = (int)(uint)option.Value;
                    break;
                default:
                    throw new Exception(string.Format("Unhandled parsed argument: '{0}'", option.LongName));
            }
        }

        private static void WriteVersion()
        {
            Console.WriteLine("Version: {0}", typeof(CommandLineOptions).Assembly.GetName().Version);
        }

        /// <summary>
        /// Checks the configuration for errors and performs post-processing updates.
        /// </summary>
        private static void SanitizeConfiguration(Configuration configuration)
        {
            if (configuration.LivenessTemperatureThreshold == 0 &&
                configuration.MaxFairSchedulingSteps > 0)
            {
                configuration.LivenessTemperatureThreshold = configuration.MaxFairSchedulingSteps / 2;
            }

            if (string.IsNullOrEmpty(configuration.AssemblyToBeAnalyzed) &&
                string.Compare(configuration.ToolCommand, "test", StringComparison.OrdinalIgnoreCase) == 0)
            {
                Error.ReportAndExit("Please give a valid path to a Coyote program's dll using 'test x'.");
            }

            if (configuration.SchedulingStrategy != "portfolio" &&
                configuration.SchedulingStrategy != "random" &&
                configuration.SchedulingStrategy != "pct" &&
                configuration.SchedulingStrategy != "fairpct" &&
                configuration.SchedulingStrategy != "probabilistic" &&
                configuration.SchedulingStrategy != "dfs")
            {
                Error.ReportAndExit("Please provide a scheduling strategy (see --sch* options)");
            }

            if (configuration.MaxFairSchedulingSteps < configuration.MaxUnfairSchedulingSteps)
            {
                Error.ReportAndExit("For the option '-max-steps N[,M]', please make sure that M >= N.");
            }

            if (configuration.SafetyPrefixBound > 0 &&
                configuration.SafetyPrefixBound >= configuration.MaxUnfairSchedulingSteps)
            {
                Error.ReportAndExit("Please give a safety prefix bound that is less than the " +
                    "max scheduling steps bound.");
            }

#if NETCOREAPP
            if (configuration.ReportCodeCoverage)
            {
                Error.ReportAndExit("We do not yet support code coverage reports when using the .NET Core runtime.");
            }
#endif
        }
    }
}
