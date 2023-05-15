// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PChecker
{
#pragma warning disable CA1724 // Type names should not match namespaces
    /// <summary>
    /// The Coyote project configurations.
    /// </summary>
    [DataContract]
    [Serializable]
    public class CheckerConfiguration
    {

        /// <summary>
        /// The output path.
        /// </summary>
        [DataMember]
        public string OutputFilePath;

        /// <summary>
        /// Timeout in seconds.
        /// </summary>
        [DataMember]
        public int Timeout;

        /// <summary>
        /// Memory limit in GB.
        /// </summary>
        [DataMember]
        public double MemoryLimit;

        /// <summary>
        /// The assembly to be analyzed for bugs.
        /// </summary>
        [DataMember]
        public string AssemblyToBeAnalyzed;

        /// <summary>
        /// Checking mode
        /// </summary>
        [DataMember]
        public CheckerMode Mode;

        /// <summary>
        /// Test case to be used.
        /// </summary>
        [DataMember]
        public string TestCaseName;

        /// <summary>
        /// The systematic testing strategy to use.
        /// </summary>
        [DataMember]
        public string SchedulingStrategy { get;  set; }

        /// <summary>
        /// Number of testing iterations.
        /// </summary>
        [DataMember]
        public int TestingIterations { get;  set; }

        /// <summary>
        /// Custom seed to be used by the random value generator. By default,
        /// this value is null indicating that no seed has been set.
        /// </summary>
        [DataMember]
        public uint? RandomGeneratorSeed { get;  set; }

        /// <summary>
        /// If true, the seed will increment in each
        /// testing iteration.
        /// </summary>
        [DataMember]
        public bool IncrementalSchedulingSeed;

        /// <summary>
        /// If true, the Coyote tester performs a full exploration,
        /// and does not stop when it finds a bug.
        /// </summary>
        [DataMember]
        public bool PerformFullExploration;

        /// <summary>
        /// The maximum scheduling steps to explore for fair schedulers.
        /// By default this is set to 100,000 steps.
        /// </summary>
        [DataMember]
        public int MaxFairSchedulingSteps { get; set; }

        /// <summary>
        /// The maximum scheduling steps to explore for unfair schedulers.
        /// By default this is set to 10,000 steps.
        /// </summary>
        [DataMember]
        public int MaxUnfairSchedulingSteps { get; set; }

        /// <summary>
        /// The maximum scheduling steps to explore
        /// for both fair and unfair schedulers.
        /// By default there is no bound.
        /// </summary>
        public int MaxSchedulingSteps
        {
            set
            {
                MaxUnfairSchedulingSteps = value;
                MaxFairSchedulingSteps = value;
            }
        }

        /// <summary>
        /// True if the user has explicitly set the
        /// fair scheduling steps bound.
        /// </summary>
        [DataMember]
        public bool UserExplicitlySetMaxFairSchedulingSteps;

        /// <summary>
        /// If true, then the Coyote tester will consider an execution
        /// that hits the depth bound as buggy.
        /// </summary>
        [DataMember]
        public bool ConsiderDepthBoundHitAsBug;

        /// <summary>
        /// A strategy-specific bound.
        /// </summary>
        [DataMember]
        public int StrategyBound { get; set; }

        /// <summary>
        /// If this option is enabled, liveness checking is enabled during bug-finding.
        /// </summary>
        [DataMember]
        public bool IsLivenessCheckingEnabled;

        /// <summary>
        /// The liveness temperature threshold. If it is 0
        /// then it is disabled.
        /// </summary>
        [DataMember]
        public int LivenessTemperatureThreshold { get; set; }

        /// <summary>
        /// If this option is enabled, the tester is hashing the program state.
        /// </summary>
        [DataMember] public bool IsProgramStateHashingEnabled;
        
        /// <summary>
        /// The schedule file to be replayed.
        /// </summary>
        public string ScheduleFile;

        /// <summary>
        /// The schedule trace to be replayed.
        /// </summary>
        public string ScheduleTrace;

        /// <summary>
        /// If true, then messages are logged.
        /// </summary>
        [DataMember]
        public bool IsVerbose { get; set; }

        /// <summary>
        /// Enables code coverage reporting of a Coyote program.
        /// </summary>
        [DataMember]
        public bool ReportCodeCoverage;

        /// <summary>
        /// Enables activity coverage reporting of a Coyote program.
        /// </summary>
        [DataMember]
        public bool ReportActivityCoverage { get; set; }

        /// <summary>
        /// Enables activity coverage debugging.
        /// </summary>
        public bool DebugActivityCoverage;

        /// <summary>
        /// Is DGML graph showing all test iterations or just one "bug" iteration.
        /// False means all, and True means only the iteration containing a bug.
        /// </summary>
        [DataMember]
        public bool IsDgmlBugGraph;

        /// <summary>
        /// If specified, requests a DGML graph of the iteration that contains a bug, if a bug is found.
        /// This is different from a coverage activity graph, as it will also show actor instances.
        /// </summary>
        [DataMember]
        public bool IsDgmlGraphEnabled { get; set; }

        /// <summary>
        /// Produce an XML formatted runtime log file.
        /// </summary>
        [DataMember]
        public bool IsXmlLogEnabled { get; set; }

        /// <summary>
        /// If specified, requests a custom runtime log to be used instead of the default.
        /// This is the AssemblyQualifiedName of the type to load.
        /// </summary>
        [DataMember]
        public string CustomActorRuntimeLogType;

        /// <summary>
        /// Enables debugging.
        /// </summary>
        [DataMember]
        public bool EnableDebugging;


        /// <summary>
        /// The testing scheduler unique endpoint.
        /// </summary>
        [DataMember]
        public string TestingSchedulerEndPoint;

        /// <summary>
        /// The unique testing process id.
        /// </summary>
        [DataMember]
        public uint TestingProcessId;

        /// <summary>
        /// Additional assembly specifications to instrument for code coverage, besides those in the
        /// dependency graph between <see cref="AssemblyToBeAnalyzed"/> and the Microsoft.Coyote DLLs.
        /// Key is filename, value is whether it is a list file (true) or a single file (false).
        /// </summary>
        public Dictionary<string, bool> AdditionalCodeCoverageAssemblies;

        /// <summary>
        /// Enables colored console output.
        /// </summary>
        public bool EnableColoredConsoleOutput;

        /// <summary>
        /// If true, then environment exit will be disabled.
        /// </summary>
        public bool DisableEnvironmentExit;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckerConfiguration"/> class.
        /// </summary>
        protected CheckerConfiguration()
        {
            OutputFilePath = "PCheckerOutput";

            Timeout = 0;
            MemoryLimit = 0;

            AssemblyToBeAnalyzed = string.Empty;
            Mode = CheckerMode.BugFinding;
            TestCaseName = string.Empty;

            SchedulingStrategy = "random";
            TestingIterations = 1;
            RandomGeneratorSeed = null;
            IncrementalSchedulingSeed = false;
            PerformFullExploration = false;
            MaxFairSchedulingSteps = 100000; // 10 times the unfair steps
            MaxUnfairSchedulingSteps = 10000;
            UserExplicitlySetMaxFairSchedulingSteps = false;
            TestingSchedulerEndPoint = "CoyoteTestScheduler.4723bb92-c413-4ecb-8e8a-22eb2ba22234";
            TestingProcessId = 0;
            ConsiderDepthBoundHitAsBug = false;
            StrategyBound = 0;

            IsLivenessCheckingEnabled = true;
            LivenessTemperatureThreshold = 0;

            IsProgramStateHashingEnabled = false;

            ScheduleFile = string.Empty;
            ScheduleTrace = string.Empty;

            ReportCodeCoverage = false;
            ReportActivityCoverage = true;
            DebugActivityCoverage = false;

            IsVerbose = false;
            EnableDebugging = false;

            AdditionalCodeCoverageAssemblies = new Dictionary<string, bool>();

            EnableColoredConsoleOutput = false;
            DisableEnvironmentExit = true;
        }

        /// <summary>
        /// Creates a new checkerConfiguration with default values.
        /// </summary>
        public static CheckerConfiguration Create()
        {
            return new CheckerConfiguration();
        }

        /// <summary>
        /// Updates the checkerConfiguration to use the random scheduling strategy during systematic testing.
        /// </summary>
        public CheckerConfiguration WithRandomStrategy()
        {
            SchedulingStrategy = "random";
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration to use the probabilistic scheduling strategy during systematic testing.
        /// You can specify a value controlling the probability of each scheduling decision. This value is
        /// specified as the integer N in the equation 0.5 to the power of N. So for N=1, the probability is
        /// 0.5, for N=2 the probability is 0.25, N=3 you get 0.125, etc. By default, this value is 3.
        /// </summary>
        /// <param name="probabilityLevel">The probability level.</param>
        public CheckerConfiguration WithProbabilisticStrategy(uint probabilityLevel = 3)
        {
            SchedulingStrategy = "fairpct";
            StrategyBound = (int)probabilityLevel;
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration to use the PCT scheduling strategy during systematic testing.
        /// You can specify the number of priority switch points, which by default are 10.
        /// </summary>
        /// <param name="isFair">If true, use the fair version of PCT.</param>
        /// <param name="numPrioritySwitchPoints">The nunmber of priority switch points.</param>
        public CheckerConfiguration WithPCTStrategy(bool isFair, uint numPrioritySwitchPoints = 10)
        {
            SchedulingStrategy = isFair ? "fairpct" : "pct";
            StrategyBound = (int)numPrioritySwitchPoints;
            return this;
        }
        
        /// <summary>
        /// Updates the configuration to use the reinforcement learning (RL) scheduling strategy
        /// during systematic testing.
        /// </summary>
        public CheckerConfiguration WithRLStrategy()
        {
            this.SchedulingStrategy = "rl";
            this.IsProgramStateHashingEnabled = true;
            return this;
        }
        
        /// <summary>
        /// Updates the checkerConfiguration to use the dfs scheduling strategy during systematic testing.
        /// </summary>
        public CheckerConfiguration WithDFSStrategy()
        {
            SchedulingStrategy = "dfs";
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration to use the replay scheduling strategy during systematic testing.
        /// This strategy replays the specified schedule trace to reproduce the same execution.
        /// </summary>
        /// <param name="scheduleTrace">The schedule trace to be replayed.</param>
        public CheckerConfiguration WithReplayStrategy(string scheduleTrace)
        {
            SchedulingStrategy = "replay";
            ScheduleTrace = scheduleTrace;
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration with the specified number of iterations to run during systematic testing.
        /// </summary>
        /// <param name="iterations">The number of iterations to run.</param>
        public CheckerConfiguration WithTestingIterations(uint iterations)
        {
            TestingIterations = (int)iterations;
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration with the specified number of scheduling steps to explore per iteration
        /// (for both fair and unfair schedulers) during systematic testing.
        /// </summary>
        /// <param name="maxSteps">The scheduling steps to explore per iteration.</param>
        public CheckerConfiguration WithMaxSchedulingSteps(uint maxSteps)
        {
            MaxSchedulingSteps = (int)maxSteps;
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration with the specified number of fair scheduling steps to explore
        /// per iteration during systematic testing.
        /// </summary>
        /// <param name="maxFairSteps">The scheduling steps to explore per iteration.</param>
        public CheckerConfiguration WithMaxFairSchedulingSteps(uint maxFairSteps)
        {
            MaxFairSchedulingSteps = (int)maxFairSteps;
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration with the specified number of unfair scheduling steps to explore
        /// per iteration during systematic testing.
        /// </summary>
        /// <param name="maxUnfairSteps">The scheduling steps to explore per iteration.</param>
        public CheckerConfiguration WithMaxUnfairSchedulingSteps(uint maxUnfairSteps)
        {
            MaxUnfairSchedulingSteps = (int)maxUnfairSteps;
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration with the specified liveness temperature threshold during systematic testing.
        /// If this value is 0 it disables liveness checking.
        /// </summary>
        /// <param name="threshold">The liveness temperature threshold.</param>
        public CheckerConfiguration WithLivenessTemperatureThreshold(uint threshold)
        {
            LivenessTemperatureThreshold = (int)threshold;
            return this;
        }

        /// <summary>
        /// Updates the seed used by the random value generator during systematic testing.
        /// </summary>
        /// <param name="seed">The seed used by the random value generator.</param>
        public CheckerConfiguration WithRandomGeneratorSeed(uint seed)
        {
            RandomGeneratorSeed = seed;
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration with verbose output enabled or disabled.
        /// </summary>
        /// <param name="isVerbose">If true, then messages are logged.</param>
        public CheckerConfiguration WithVerbosityEnabled(bool isVerbose = true)
        {
            IsVerbose = isVerbose;
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration with activity coverage enabled or disabled.
        /// </summary>
        /// <param name="isEnabled">If true, then enables activity coverage.</param>
        public CheckerConfiguration WithActivityCoverageEnabled(bool isEnabled = true)
        {
            ReportActivityCoverage = isEnabled;
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration with DGML graph generation enabled or disabled.
        /// </summary>
        /// <param name="isEnabled">If true, then enables DGML graph generation.</param>
        public CheckerConfiguration WithDgmlGraphEnabled(bool isEnabled = true)
        {
            IsDgmlGraphEnabled = isEnabled;
            return this;
        }

        /// <summary>
        /// Updates the checkerConfiguration with XML log generation enabled or disabled.
        /// </summary>
        /// <param name="isEnabled">If true, then enables XML log generation.</param>
        public CheckerConfiguration WithXmlLogEnabled(bool isEnabled = true)
        {
            IsXmlLogEnabled = isEnabled;
            return this;
        }
    }
#pragma warning restore CA1724 // Type names should not match namespaces
}
