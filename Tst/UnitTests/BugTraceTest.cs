using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using NUnit.Framework;
using PChecker;
using PChecker.Runtime.Events;
using PChecker.Runtime.Logging;
using PChecker.Runtime.StateMachines;
using PChecker.SystematicTesting;
using UnitTests.Core;
using UnitTests.Runners;
using TestAttribute = NUnit.Framework.TestAttribute;

namespace UnitTests
{
    /// <summary>
    /// The checker explores schedules without tracing them, and replays a failing schedule to build
    /// its text and JSON traces. Those traces must be the ones that tracing the failing schedule
    /// itself builds (as the checker did before), for every kind of bug. When the replay does not
    /// reproduce the bug, the traces must still report it, with a warning. Full exploration must not
    /// replay schedules, and custom logs must observe every schedule.
    /// </summary>
    [TestFixture]
    public class BugTraceTest
    {
        private AssemblyLoadContext context;
        private Assembly assembly;
        private DirectoryInfo scratchDir;

        [OneTimeSetUp]
        public void CompileProgram()
        {
            var testDir = new DirectoryInfo(Path.Combine(Constants.TestDirectory, "RegressionTests", "Integration",
                "Traces", "BugTraces"));
            scratchDir = Directory.CreateDirectory(Path.Combine(Constants.ScratchParentDirectory,
                nameof(BugTraceTest)));
            var generatedDir = Directory.CreateDirectory(Path.Combine(scratchDir.FullName, "PChecker"));
            foreach (var nativeFile in testDir.GetFiles("*.cs"))
            {
                nativeFile.CopyTo(Path.Combine(generatedDir.FullName, nativeFile.Name), true);
            }

            var runner = new PCheckerRunner(testDir.GetFiles("*.p"), testDir.GetFiles("*.cs"));
            Assert.That(runner.DoCompile(scratchDir), Is.EqualTo(0), "the test program must compile");

            context = new AssemblyLoadContext($"ALC_{Guid.NewGuid():N}", isCollectible: true);
            assembly = context.LoadFromAssemblyPath(Path.Combine(generatedDir.FullName, "net8.0", "BugTraces.dll"));
        }

        [OneTimeTearDown]
        public void Unload() => context?.Unload();

        private static CheckerConfiguration Configuration(string strategy = "random", uint seed = 1)
        {
            var configuration = CheckerConfiguration.Create()
                .WithTestingIterations(100)
                .WithMaxSchedulingSteps(100)
                .WithRandomGeneratorSeed(seed);
            configuration.SchedulingStrategy = strategy;
            configuration.StrategyBound = 3;
            return configuration;
        }

        private Action<ControlledRuntime> TestEntry(string testName)
        {
            var method = assembly.GetType($"PImplementation.{testName}")
                .GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            return (Action<ControlledRuntime>)Delegate.CreateDelegate(typeof(Action<ControlledRuntime>), method);
        }

        private int ForeignCounter
        {
            get => (int)assembly.GetType("PImplementation.GlobalFunctions").GetField("Counter").GetValue(null);
        }

        /// <summary>
        /// Runs the test, and returns the engine and the JSON trace it emits, if any.
        /// </summary>
        private (TestingEngine Engine, string JsonTrace) Check(string testName, CheckerConfiguration configuration)
        {
            lock (PCheckerRunner.CheckerLock)
            {
                var engine = TestingEngine.Create(configuration, TestEntry(testName));
                engine.Run();

                var outputDir = Directory.CreateDirectory(Path.Combine(scratchDir.FullName, "Traces",
                    Guid.NewGuid().ToString("N")));
                var originalOut = Console.Out;
                Console.SetOut(TextWriter.Null);
                try
                {
                    engine.TryEmitTraces(outputDir.FullName + Path.DirectorySeparatorChar, testName);
                }
                finally
                {
                    Console.SetOut(originalOut);
                }

                var jsonFile = outputDir.GetFiles("*.trace.json").SingleOrDefault();
                return (engine, jsonFile is null ? null : File.ReadAllText(jsonFile.FullName));
            }
        }

        /// <summary>
        /// Checks that the traces of the replayed failing schedule are those that tracing every
        /// schedule builds, and returns the text trace.
        /// </summary>
        private string AssertTraceOfFailingSchedule(string testName, CheckerConfiguration expectedConfiguration,
            CheckerConfiguration configuration)
        {
            expectedConfiguration.IsTracingEverySchedule = true;
            var (expected, expectedJson) = Check(testName, expectedConfiguration);
            var (actual, actualJson) = Check(testName, configuration);

            Assert.That(expected.TestReport.BugReports, Has.Count.EqualTo(1), "the test must find a bug");
            Assert.That(actual.TestReport.BugReports, Is.EqualTo(expected.TestReport.BugReports));
            Assert.That(actual.ReproducableTrace, Is.EqualTo(expected.ReproducableTrace));
            Assert.That(actual.ReadableTrace, Is.EqualTo(expected.ReadableTrace));
            Assert.That(actualJson, Is.EqualTo(expectedJson));

            var bug = actual.TestReport.BugReports.Single();
            Assert.That(actual.ReadableTrace, Does.Contain($"<ErrorLog> {bug}{Environment.NewLine}"));
            Assert.That(actual.ReadableTrace, Does.Not.Contain("WARNING"));
            Assert.That(GetJsonEntries(actualJson, "AssertionFailure"), Is.EqualTo(new[] { bug }));
            return actual.ReadableTrace;
        }

        private static string[] GetJsonEntries(string json, string type)
        {
            using var document = JsonDocument.Parse(json);
            var key = type == "AssertionFailure" ? "error" : "strategyDescription";
            return document.RootElement.EnumerateArray()
                .Where(entry => entry.GetProperty("type").GetString() == type)
                .Select(entry => entry.GetProperty("details").GetProperty(key).GetString())
                .ToArray();
        }

        [TestCase("random")]
        [TestCase("pct")]
        [TestCase("fairpct")]
        [TestCase("feedbackpct")]
        public void AssertionFailureTraceIsTheFailingSchedule(string strategy)
        {
            var trace = AssertTraceOfFailingSchedule("tcAssert", Configuration(strategy), Configuration(strategy));
            Assert.That(trace, Does.Contain("<ErrorLog> Assertion Failed: "));
            Assert.That(trace, Does.Contain("<PrintLog> 1 2" + Environment.NewLine),
                "a print argument must be formatted with its value before later arguments are evaluated");
        }

        [TestCase("random")]
        [TestCase("pct")]
        [TestCase("fairpct")]
        public void StepsBoundTraceIsTheFailingSchedule(string strategy)
        {
            CheckerConfiguration FailOnMaxSteps()
            {
                var configuration = Configuration(strategy);
                configuration.ConsiderDepthBoundHitAsBug = true;
                return configuration;
            }

            var trace = AssertTraceOfFailingSchedule("tcMaxSteps", FailOnMaxSteps(), FailOnMaxSteps());
            Assert.That(trace, Does.Match("<ErrorLog> Scheduling steps bound of [0-9]+ reached\\."));
        }

        [Test]
        public void HotStateAtTerminationTraceIsTheFailingSchedule()
        {
            var trace = AssertTraceOfFailingSchedule("tcHotAtEnd", Configuration(), Configuration());
            Assert.That(trace, Does.Contain(
                "<ErrorLog> PImplementation.Responded detected liveness bug in hot state 'Waiting' at the end of " +
                "program execution."));
        }

        [Test]
        public void LivenessTemperatureTraceIsTheFailingSchedule()
        {
            CheckerConfiguration WithTemperature() =>
                Configuration().WithMaxSchedulingSteps(200).WithLivenessTemperatureThreshold(50);

            var trace = AssertTraceOfFailingSchedule("tcTemperature", WithTemperature(), WithTemperature());
            Assert.That(trace, Does.Contain(
                "<ErrorLog> PImplementation.Progress detected potential liveness bug in hot state 'Busy'."));
        }

        [Test]
        public void JsonTraceDescribesTheStrategyThatFoundTheBug()
        {
            var (engine, json) = Check("tcAssert", Configuration("random", 7));

            Assert.That(engine.TestReport.NumOfFoundBugs, Is.EqualTo(1));
            var description = GetJsonEntries(json, "StrategyDescription").Single();
            Assert.That(description, Does.StartWith("random[seed '"), "not the replay that traced the schedule");
        }

        [Test]
        public void TraceReportsTheBugWhenTheReplayDoesNotReproduceIt()
        {
            // The schedule that finds the bug sees an even counter, and its replay the next, odd, one.
            var (engine, json) = Check("tcCounter", Configuration());

            var bug = engine.TestReport.BugReports.Single();
            Assert.That(bug, Does.Match("counter hit [0-9]*[02468]$"));
            Assert.That(engine.ReadableTrace, Does.StartWith(
                "<StrategyLog> WARNING: Replaying the failing schedule to trace it did not reproduce the bug: the " +
                "replay found no bug."));
            Assert.That(engine.ReadableTrace, Does.Contain($"<ErrorLog> {bug}{Environment.NewLine}"));
            Assert.That(GetJsonEntries(json, "AssertionFailure"), Is.EqualTo(new[] { bug }));
            Assert.That(GetJsonEntries(json, "StrategyDescription").Single(), Does.StartWith("random[seed '"));
        }

        [Test]
        public void FullExplorationDoesNotReplaySchedules()
        {
            const int iterations = 10;
            var configuration = Configuration().WithTestingIterations(iterations);
            configuration.PerformFullExploration = true;

            var counterBefore = ForeignCounter;
            var (engine, _) = Check("tcCounter", configuration);

            // Every schedule calls the foreign function once, and every other one fails. A replay
            // would call it again, which would also change which later schedules fail.
            Assert.That(ForeignCounter - counterBefore, Is.EqualTo(iterations));
            Assert.That(engine.TestReport.NumOfFoundBugs, Is.EqualTo(iterations / 2));
            Assert.That(engine.ReproducableTrace, Is.Not.Empty, "the schedule of the first bug is kept");
        }

        private class CountingTextLog : PCheckerLogTextFormatter, IControlledRuntimeLog
        {
            internal static int Created;

            public new void OnCreateStateMachine(StateMachineId id, string creatorName, string creatorType) =>
                Created++;
        }

        private class CountingJsonLog : PCheckerLogJsonFormatter, IControlledRuntimeLog
        {
            internal static int Created;

            public new void OnCreateStateMachine(StateMachineId id, string creatorName, string creatorType) =>
                Created++;
        }

        private class Idle : StateMachine
        {
            [Start]
            private class Init : State
            {
            }
        }

        [Test]
        public void SubclassesOfTheTraceFormattersObserveEverySchedule()
        {
            const uint iterations = 20;
            TestReport report;
            lock (PCheckerRunner.CheckerLock)
            {
                CountingTextLog.Created = 0;
                CountingJsonLog.Created = 0;
                var engine = TestingEngine.Create(Configuration().WithTestingIterations(iterations), runtime =>
                {
                    // Like the default trace formatters that generated code registers.
                    runtime.RegisterLog(new PCheckerLogTextFormatter());
                    runtime.RegisterLog(new PCheckerLogJsonFormatter());
                    runtime.RegisterLog(new CountingTextLog());
                    runtime.RegisterLog(new CountingJsonLog());
                    runtime.CreateStateMachine(typeof(Idle));
                });
                engine.Run();
                report = engine.TestReport;
            }

            Assert.That(report.NumOfFoundBugs, Is.EqualTo(0));
            Assert.That(CountingTextLog.Created, Is.EqualTo(iterations));
            Assert.That(CountingJsonLog.Created, Is.EqualTo(iterations));
        }
    }
}
