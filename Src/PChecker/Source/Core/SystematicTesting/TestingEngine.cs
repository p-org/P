// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Coverage;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting.Strategies;
using CoyoteTasks = Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Testing engine that can run a controlled concurrency test using
    /// a specified configuration.
    /// </summary>
#if !DEBUG
    [DebuggerStepThrough]
#endif
    public sealed class TestingEngine
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The method to test.
        /// </summary>
        private readonly TestMethodInfo TestMethodInfo;

        /// <summary>
        /// Set of callbacks to invoke at the end
        /// of each iteration.
        /// </summary>
        private readonly ISet<Action<int>> PerIterationCallbacks;

        /// <summary>
        /// The program exploration strategy.
        /// </summary>
        internal readonly ISchedulingStrategy Strategy;

        /// <summary>
        /// Random value generator used by the scheduling strategies.
        /// </summary>
        private readonly IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The error reporter.
        /// </summary>
        private readonly ErrorReporter ErrorReporter;

        /// <summary>
        /// The installed logger.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/core/logging" >Logging</see> for more information.
        /// </remarks>
        private TextWriter Logger;

        /// <summary>
        /// The profiler.
        /// </summary>
        private readonly Profiler Profiler;

        /// <summary>
        /// The testing task cancellation token source.
        /// </summary>
        private readonly CancellationTokenSource CancellationTokenSource;

        /// <summary>
        /// Data structure containing information
        /// gathered during testing.
        /// </summary>
        public TestReport TestReport { get; set; }

        /// <summary>
        /// A graph of the actors, state machines and events of a single test iteration.
        /// </summary>
        private Graph Graph;

        /// <summary>
        /// Contains a single iteration of XML log output in the case where the IsXmlLogEnabled
        /// configuration is specified.
        /// </summary>
        private StringBuilder XmlLog;

        /// <summary>
        /// The readable trace, if any.
        /// </summary>
        public string ReadableTrace { get; private set; }

        /// <summary>
        /// The reproducable trace, if any.
        /// </summary>
        public string ReproducableTrace { get; private set; }

        /// <summary>
        /// Checks if the systematic testing engine is running in replay mode.
        /// </summary>
        private bool IsReplayModeEnabled => this.Strategy is ReplayStrategy;

        /// <summary>
        /// A guard for printing info.
        /// </summary>
        private int PrintGuard;

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration) =>
            Create(configuration, LoadAssembly(configuration.AssemblyToBeAnalyzed));

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Assembly assembly)
        {
            TestMethodInfo testMethodInfo = null;
            try
            {
                testMethodInfo = TestMethodInfo.GetFromAssembly(assembly, configuration.TestMethodName);
            }
            catch
            {
                Error.ReportAndExit($"Failed to get test method '{configuration.TestMethodName}' from assembly '{assembly.FullName}'");
            }

            return new TestingEngine(configuration, testMethodInfo);
        }

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Action test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Action<ICoyoteRuntime> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Action<IActorRuntime> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Func<CoyoteTasks.Task> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Func<ICoyoteRuntime, CoyoteTasks.Task> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Creates a new systematic testing engine.
        /// </summary>
        public static TestingEngine Create(Configuration configuration, Func<IActorRuntime, CoyoteTasks.Task> test) =>
            new TestingEngine(configuration, test);

        /// <summary>
        /// Initializes a new instance of the <see cref="TestingEngine"/> class.
        /// </summary>
        internal TestingEngine(Configuration configuration, Delegate test)
            : this(configuration, new TestMethodInfo(test))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestingEngine"/> class.
        /// </summary>
        private TestingEngine(Configuration configuration, TestMethodInfo testMethodInfo)
        {
            this.Configuration = configuration;
            this.TestMethodInfo = testMethodInfo;

            this.Logger = new ConsoleLogger();
            this.ErrorReporter = new ErrorReporter(configuration, this.Logger);
            this.Profiler = new Profiler();

            this.PerIterationCallbacks = new HashSet<Action<int>>();

            // Initializes scheduling strategy specific components.
            this.RandomValueGenerator = new RandomValueGenerator(configuration);

            this.TestReport = new TestReport(configuration);
            this.ReadableTrace = string.Empty;
            this.ReproducableTrace = string.Empty;

            this.CancellationTokenSource = new CancellationTokenSource();
            this.PrintGuard = 1;

            if (configuration.SchedulingStrategy is "replay")
            {
                var scheduleDump = this.GetScheduleForReplay(out bool isFair);
                ScheduleTrace schedule = new ScheduleTrace(scheduleDump);
                this.Strategy = new ReplayStrategy(configuration, schedule, isFair);
            }
            else if (configuration.SchedulingStrategy is "random")
            {
                this.Strategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, this.RandomValueGenerator);
            }
            else if (configuration.SchedulingStrategy is "pct")
            {
                this.Strategy = new PCTStrategy(configuration.MaxUnfairSchedulingSteps, configuration.StrategyBound,
                    this.RandomValueGenerator);
            }
            else if (configuration.SchedulingStrategy is "fairpct")
            {
                var prefixLength = configuration.SafetyPrefixBound == 0 ?
                    configuration.MaxUnfairSchedulingSteps : configuration.SafetyPrefixBound;
                var prefixStrategy = new PCTStrategy(prefixLength, configuration.StrategyBound, this.RandomValueGenerator);
                var suffixStrategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, this.RandomValueGenerator);
                this.Strategy = new ComboStrategy(prefixStrategy, suffixStrategy);
            }
            else if (configuration.SchedulingStrategy is "probabilistic")
            {
                this.Strategy = new ProbabilisticRandomStrategy(configuration.MaxFairSchedulingSteps,
                    configuration.StrategyBound, this.RandomValueGenerator);
            }
            else if (configuration.SchedulingStrategy is "dfs")
            {
                this.Strategy = new DFSStrategy(configuration.MaxUnfairSchedulingSteps);
            }
            else if (configuration.SchedulingStrategy is "portfolio")
            {
                Error.ReportAndExit("Portfolio testing strategy is only " +
                    "available in parallel testing.");
            }

            if (configuration.SchedulingStrategy != "replay" &&
                configuration.ScheduleFile.Length > 0)
            {
                var scheduleDump = this.GetScheduleForReplay(out bool isFair);
                ScheduleTrace schedule = new ScheduleTrace(scheduleDump);
                this.Strategy = new ReplayStrategy(configuration, schedule, isFair, this.Strategy);
            }
        }

        /// <summary>
        /// Runs the testing engine.
        /// </summary>
        public void Run()
        {
            try
            {
                Task task = this.CreateTestingTask();
                if (this.Configuration.Timeout > 0)
                {
                    this.CancellationTokenSource.CancelAfter(
                        this.Configuration.Timeout * 1000);
                }

                this.Profiler.StartMeasuringExecutionTime();
                if (!this.CancellationTokenSource.IsCancellationRequested)
                {
                    task.Start();
                    task.Wait(this.CancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                if (this.CancellationTokenSource.IsCancellationRequested)
                {
                    this.Logger.WriteLine($"... Task {this.Configuration.TestingProcessId} timed out.");
                }
            }
            catch (AggregateException aex)
            {
                aex.Handle((ex) =>
                {
                    IO.Debug.WriteLine(ex.Message);
                    IO.Debug.WriteLine(ex.StackTrace);
                    return true;
                });

                if (aex.InnerException is FileNotFoundException)
                {
                    Error.ReportAndExit($"{aex.InnerException.Message}");
                }

                Error.ReportAndExit("Exception thrown during testing outside the context of an actor, " +
                    "possibly in a test method. Please use /debug /v:2 to print more information.");
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine($"... Task {this.Configuration.TestingProcessId} failed due to an internal error: {ex}");
                this.TestReport.InternalErrors.Add(ex.ToString());
            }
            finally
            {
                this.Profiler.StopMeasuringExecutionTime();
            }
        }

        /// <summary>
        /// Creates a new testing task.
        /// </summary>
        private Task CreateTestingTask()
        {
            string options = string.Empty;
            if (this.Configuration.SchedulingStrategy is "random" ||
                this.Configuration.SchedulingStrategy is "pct" ||
                this.Configuration.SchedulingStrategy is "fairpct" ||
                this.Configuration.SchedulingStrategy is "probabilistic")
            {
                options = $" (seed:{this.RandomValueGenerator.Seed})";
            }

            this.Logger.WriteLine($"... Task {this.Configuration.TestingProcessId} is " +
                $"using '{this.Configuration.SchedulingStrategy}' strategy{options}.");

            return new Task(() =>
            {
                if (this.Configuration.AttachDebugger)
                {
                    Debugger.Launch();
                }

                try
                {
                    // Invokes the user-specified initialization method.
                    this.TestMethodInfo.InitializeAllIterations();

                    int maxIterations = this.IsReplayModeEnabled ? 1 : this.Configuration.TestingIterations;
                    for (int i = 0; i < maxIterations; i++)
                    {
                        if (this.CancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        // Runs a new testing iteration.
                        this.RunNextIteration(i);

                        if (this.IsReplayModeEnabled || (!this.Configuration.PerformFullExploration &&
                            this.TestReport.NumOfFoundBugs > 0) || !this.Strategy.PrepareForNextIteration())
                        {
                            break;
                        }

                        if (this.RandomValueGenerator != null && this.Configuration.IncrementalSchedulingSeed)
                        {
                            // Increments the seed in the random number generator (if one is used), to
                            // capture the seed used by the scheduling strategy in the next iteration.
                            this.RandomValueGenerator.Seed += 1;
                        }

                        // Increases iterations if there is a specified timeout
                        // and the default iteration given.
                        if (this.Configuration.TestingIterations == 1 &&
                            this.Configuration.Timeout > 0)
                        {
                            maxIterations++;
                        }
                    }

                    // Invokes the user-specified test disposal method.
                    this.TestMethodInfo.DisposeAllIterations();
                }
                catch (Exception ex)
                {
                    Exception innerException = ex;
                    while (innerException is TargetInvocationException)
                    {
                        innerException = innerException.InnerException;
                    }

                    if (innerException is AggregateException)
                    {
                        innerException = innerException.InnerException;
                    }

                    if (!(innerException is TaskCanceledException))
                    {
                        ExceptionDispatchInfo.Capture(innerException).Throw();
                    }
                }
            }, this.CancellationTokenSource.Token);
        }

        /// <summary>
        /// Runs the next testing iteration.
        /// </summary>
        private void RunNextIteration(int iteration)
        {
            if (!this.IsReplayModeEnabled && this.ShouldPrintIteration(iteration + 1))
            {
                this.Logger.WriteLine($"..... Iteration #{iteration + 1}");

                // Flush when logging to console.
                if (this.Logger is ConsoleLogger)
                {
                    Console.Out.Flush();
                }
            }

            // Runtime used to serialize and test the program in this iteration.
            ControlledRuntime runtime = null;

            // Logger used to intercept the program output if no custom logger
            // is installed and if verbosity is turned off.
            InMemoryLogger runtimeLogger = null;

            // Gets a handle to the standard output and error streams.
            var stdOut = Console.Out;
            var stdErr = Console.Error;

            try
            {
                // Creates a new instance of the controlled runtime.
                runtime = new ControlledRuntime(this.Configuration, this.Strategy, this.RandomValueGenerator);

                // If verbosity is turned off, then intercept the program log, and also redirect
                // the standard output and error streams to a nul logger.
                if (!this.Configuration.IsVerbose)
                {
                    runtimeLogger = new InMemoryLogger();
                    runtime.SetLogger(runtimeLogger);

                    var writer = TextWriter.Null;
                    Console.SetOut(writer);
                    Console.SetError(writer);
                }

                this.InitializeCustomLogging(runtime);

                // Runs the test and waits for it to terminate.
                runtime.RunTest(this.TestMethodInfo.Method, this.TestMethodInfo.Name);
                runtime.WaitAsync().Wait();

                // Invokes the user-specified iteration disposal method.
                this.TestMethodInfo.DisposeCurrentIteration();

                // Invoke the per iteration callbacks, if any.
                foreach (var callback in this.PerIterationCallbacks)
                {
                    callback(iteration);
                }

                // Checks that no monitor is in a hot state at termination. Only
                // checked if no safety property violations have been found.
                if (!runtime.Scheduler.BugFound)
                {
                    runtime.CheckNoMonitorInHotStateAtTermination();
                }

                if (runtime.Scheduler.BugFound)
                {
                    this.ErrorReporter.WriteErrorLine(runtime.Scheduler.BugReport);
                }

                runtime.LogWriter.LogCompletion();

                this.GatherTestingStatistics(runtime);

                if (!this.IsReplayModeEnabled && this.TestReport.NumOfFoundBugs > 0)
                {
                    if (runtimeLogger != null)
                    {
                        this.ReadableTrace = runtimeLogger.ToString();
                        this.ReadableTrace += this.TestReport.GetText(this.Configuration, "<StrategyLog>");
                    }

                    this.ConstructReproducableTrace(runtime);
                }
            }
            finally
            {
                if (!this.Configuration.IsVerbose)
                {
                    // Restores the standard output and error streams.
                    Console.SetOut(stdOut);
                    Console.SetError(stdErr);
                }

                if (!this.IsReplayModeEnabled && this.Configuration.PerformFullExploration && runtime.Scheduler.BugFound)
                {
                    this.Logger.WriteLine($"..... Iteration #{iteration + 1} " +
                        $"triggered bug #{this.TestReport.NumOfFoundBugs} " +
                        $"[task-{this.Configuration.TestingProcessId}]");
                }

                // Cleans up the runtime before the next iteration starts.
                runtimeLogger?.Dispose();
                runtime?.Dispose();
            }
        }

        /// <summary>
        /// Stops the testing engine.
        /// </summary>
        public void Stop()
        {
            this.CancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        public string GetReport()
        {
            if (this.IsReplayModeEnabled)
            {
                StringBuilder report = new StringBuilder();
                report.AppendFormat("... Reproduced {0} bug{1}{2}.", this.TestReport.NumOfFoundBugs,
                    this.TestReport.NumOfFoundBugs == 1 ? string.Empty : "s",
                    this.Configuration.AttachDebugger ? string.Empty : " (use --break to attach the debugger)");
                report.AppendLine();
                report.Append($"... Elapsed {this.Profiler.Results()} sec.");
                return report.ToString();
            }

            return this.TestReport.GetText(this.Configuration, "...");
        }

        /// <summary>
        /// Tries to emit the testing traces, if any.
        /// </summary>
        public IEnumerable<string> TryEmitTraces(string directory, string file)
        {
            int index = 0;
            // Find the next available file index.
            Regex match = new Regex("^(.*)_([0-9]+)_([0-9]+)");
            foreach (var path in Directory.GetFiles(directory))
            {
                string name = Path.GetFileName(path);
                if (name.StartsWith(file))
                {
                    var result = match.Match(name);
                    if (result.Success)
                    {
                        string value = result.Groups[3].Value;
                        if (int.TryParse(value, out int i))
                        {
                            index = Math.Max(index, i + 1);
                        }
                    }
                }
            }

            if (!this.Configuration.PerformFullExploration)
            {
                // Emits the human readable trace, if it exists.
                if (!string.IsNullOrEmpty(this.ReadableTrace))
                {
                    string readableTracePath = directory + file + "_" + index + ".txt";

                    this.Logger.WriteLine($"..... Writing {readableTracePath}");
                    File.WriteAllText(readableTracePath, this.ReadableTrace);
                    yield return readableTracePath;
                }
            }

            if (this.Configuration.IsXmlLogEnabled)
            {
                string xmlPath = directory + file + "_" + index + ".trace.xml";
                this.Logger.WriteLine($"..... Writing {xmlPath}");
                File.WriteAllText(xmlPath, this.XmlLog.ToString());
                yield return xmlPath;
            }

            if (this.Graph != null)
            {
                string graphPath = directory + file + "_" + index + ".dgml";
                this.Graph.SaveDgml(graphPath, true);
                this.Logger.WriteLine($"..... Writing {graphPath}");
                yield return graphPath;
            }

            if (!this.Configuration.PerformFullExploration)
            {
                // Emits the reproducable trace, if it exists.
                if (!string.IsNullOrEmpty(this.ReproducableTrace))
                {
                    string reproTracePath = directory + file + "_" + index + ".schedule";

                    this.Logger.WriteLine($"..... Writing {reproTracePath}");
                    File.WriteAllText(reproTracePath, this.ReproducableTrace);
                    yield return reproTracePath;
                }
            }

            this.Logger.WriteLine($"... Elapsed {this.Profiler.Results()} sec.");
        }

        /// <summary>
        /// Registers a callback to invoke at the end of each iteration. The callback takes as
        /// a parameter an integer representing the current iteration.
        /// </summary>
        public void RegisterPerIterationCallBack(Action<int> callback)
        {
            this.PerIterationCallbacks.Add(callback);
        }

        /// <summary>
        /// Take care of handling the <see cref="Configuration"/> settings for <see cref="Configuration.CustomActorRuntimeLogType"/>,
        /// <see cref="Configuration.IsDgmlGraphEnabled"/>, and <see cref="Configuration.ReportActivityCoverage"/> by setting up the
        /// LogWriters on the given <see cref="ControlledRuntime"/> object.
        /// </summary>
        private void InitializeCustomLogging(ControlledRuntime runtime)
        {
            if (!string.IsNullOrEmpty(this.Configuration.CustomActorRuntimeLogType))
            {
                var log = this.Activate<IActorRuntimeLog>(this.Configuration.CustomActorRuntimeLogType);
                if (log != null)
                {
                    runtime.RegisterLog(log);
                }
            }

            if (this.Configuration.IsDgmlGraphEnabled || this.Configuration.ReportActivityCoverage)
            {
                // Registers an activity coverage graph builder.
                runtime.RegisterLog(new ActorRuntimeLogGraphBuilder(false)
                {
                    CollapseMachineInstances = this.Configuration.ReportActivityCoverage
                });
            }

            if (this.Configuration.ReportActivityCoverage)
            {
                // Need this additional logger to get the event coverage report correct
                runtime.RegisterLog(new ActorRuntimeLogEventCoverage());
            }

            if (this.Configuration.IsXmlLogEnabled)
            {
                this.XmlLog = new StringBuilder();
                runtime.RegisterLog(new ActorRuntimeLogXmlFormatter(XmlWriter.Create(this.XmlLog,
                    new XmlWriterSettings() { Indent = true, IndentChars = "  ", OmitXmlDeclaration = true })));
            }
        }

        private T Activate<T>(string assemblyQualifiedName)
            where T : class
        {
            // Parses the result of Type.AssemblyQualifiedName.
            // e.g.: ConsoleApp1.Program, ConsoleApp1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
            try
            {
                string[] parts = assemblyQualifiedName.Split(',');
                if (parts.Length > 1)
                {
                    string typeName = parts[0];
                    string assemblyName = parts[1];
                    Assembly a = null;
                    if (File.Exists(assemblyName))
                    {
                        a = Assembly.LoadFrom(assemblyName);
                    }
                    else
                    {
                        a = Assembly.Load(assemblyName);
                    }

                    if (a != null)
                    {
                        object o = a.CreateInstance(typeName);
                        return o as T;
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine(ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Loads and returns the specified assembly.
        /// </summary>
        private static Assembly LoadAssembly(string assemblyFile)
        {
            Assembly assembly = null;

            try
            {
                assembly = Assembly.LoadFrom(assemblyFile);
            }
            catch (FileNotFoundException ex)
            {
                Error.ReportAndExit(ex.Message);
            }

#if NETFRAMEWORK
            // Load config file and absorb its settings.
            try
            {
                var configFile = System.Configuration.ConfigurationManager.OpenExeConfiguration(assemblyFile);
                var settings = configFile.AppSettings.Settings;
                foreach (var key in settings.AllKeys)
                {
                    if (System.Configuration.ConfigurationManager.AppSettings.Get(key) is null)
                    {
                        System.Configuration.ConfigurationManager.AppSettings.Set(key, settings[key].Value);
                    }
                    else
                    {
                        System.Configuration.ConfigurationManager.AppSettings.Add(key, settings[key].Value);
                    }
                }
            }
            catch (System.Configuration.ConfigurationErrorsException ex)
            {
                Error.Report(ex.Message);
            }
#endif

            return assembly;
        }

        /// <summary>
        /// Gathers the exploration strategy statistics from the specified runtimne.
        /// </summary>
        private void GatherTestingStatistics(ControlledRuntime runtime)
        {
            TestReport report = runtime.Scheduler.GetReport();
            if (this.Configuration.ReportActivityCoverage)
            {
                report.CoverageInfo.CoverageGraph = this.Graph;
            }

            var coverageInfo = runtime.GetCoverageInfo();
            report.CoverageInfo.Merge(coverageInfo);
            this.TestReport.Merge(report);

            // Also save the graph snapshot of the last iteration, if there is one.
            this.Graph = coverageInfo.CoverageGraph;
        }

        /// <summary>
        /// Constructs a reproducable trace.
        /// </summary>
        private void ConstructReproducableTrace(ControlledRuntime runtime)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (this.Strategy.IsFair())
            {
                stringBuilder.Append("--fair-scheduling").Append(Environment.NewLine);
            }

            if (this.Configuration.IsLivenessCheckingEnabled)
            {
                stringBuilder.Append("--liveness-temperature-threshold:" +
                    this.Configuration.LivenessTemperatureThreshold).
                    Append(Environment.NewLine);
            }

            if (!string.IsNullOrEmpty(this.Configuration.TestMethodName))
            {
                stringBuilder.Append("--test-method:" +
                    this.Configuration.TestMethodName).
                    Append(Environment.NewLine);
            }

            for (int idx = 0; idx < runtime.Scheduler.ScheduleTrace.Count; idx++)
            {
                ScheduleStep step = runtime.Scheduler.ScheduleTrace[idx];
                if (step.Type == ScheduleStepType.SchedulingChoice)
                {
                    stringBuilder.Append($"({step.ScheduledOperationId})");
                }
                else if (step.BooleanChoice != null)
                {
                    stringBuilder.Append(step.BooleanChoice.Value);
                }
                else
                {
                    stringBuilder.Append(step.IntegerChoice.Value);
                }

                if (idx < runtime.Scheduler.ScheduleTrace.Count - 1)
                {
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            this.ReproducableTrace = stringBuilder.ToString();
        }

        /// <summary>
        /// Returns the schedule to replay.
        /// </summary>
        private string[] GetScheduleForReplay(out bool isFair)
        {
            string[] scheduleDump;
            if (this.Configuration.ScheduleTrace.Length > 0)
            {
                scheduleDump = this.Configuration.ScheduleTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            }
            else
            {
                scheduleDump = File.ReadAllLines(this.Configuration.ScheduleFile);
            }

            isFair = false;
            foreach (var line in scheduleDump)
            {
                if (!line.StartsWith("--"))
                {
                    break;
                }

                if (line.Equals("--fair-scheduling"))
                {
                    isFair = true;
                }
                else if (line.StartsWith("--liveness-temperature-threshold:"))
                {
                    this.Configuration.LivenessTemperatureThreshold =
                        int.Parse(line.Substring("--liveness-temperature-threshold:".Length));
                }
                else if (line.StartsWith("--test-method:"))
                {
                    this.Configuration.TestMethodName =
                        line.Substring("--test-method:".Length);
                }
            }

            return scheduleDump;
        }

        /// <summary>
        /// Returns true if the engine should print the current iteration.
        /// </summary>
        private bool ShouldPrintIteration(int iteration)
        {
            if (iteration > this.PrintGuard * 10)
            {
                var count = iteration.ToString().Length - 1;
                var guard = "1" + (count > 0 ? string.Concat(Enumerable.Repeat("0", count)) : string.Empty);
                this.PrintGuard = int.Parse(guard);
            }

            return iteration % this.PrintGuard == 0;
        }

        /// <summary>
        /// Installs the specified <see cref="TextWriter"/>.
        /// </summary>
        public void SetLogger(TextWriter logger)
        {
            this.Logger.Dispose();

            if (logger is null)
            {
                this.Logger = TextWriter.Null;
            }
            else
            {
                this.Logger = logger;
            }

            this.ErrorReporter.Logger = logger;
        }
    }
}
