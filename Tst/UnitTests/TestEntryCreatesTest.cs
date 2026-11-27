using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PChecker;
using PChecker.Runtime.Events;
using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;
using PChecker.SystematicTesting;
using UnitTests.Runners;

namespace UnitTests
{
    /// <summary>
    /// A C# test entry method runs synchronously, so it cannot be suspended at a scheduling point.
    /// It must still be able to create several state machines (as on master) without the checker
    /// reporting a bug of its own, under every seed and strategy.
    /// </summary>
    [TestFixture]
    public class TestEntryCreatesTest
    {
        private const int Senders = 3;
        private const uint Iterations = 50;

        private static int pingsReceived;

        private class Ping : Event
        {
            public Ping() : base()
            {
            }
        }

        private class Receiver : StateMachine
        {
            public Receiver()
            {
                receives.Add(nameof(Ping));
            }

            private void OnPing()
            {
                Interlocked.Increment(ref pingsReceived);
            }

            [Start]
            [OnEventDoAction(typeof(Ping), nameof(OnPing))]
            private class Init : State
            {
            }
        }

        /// <summary>
        /// Sends one ping to the machine given by the payload of its initial event.
        /// </summary>
        private class Sender : StateMachine
        {
            public Sender()
            {
                sends.Add(nameof(Ping));
            }

            private async Task Entry(Event e)
            {
                await SendEventAsync((PMachineValue)e.Payload, new Ping());
            }

            [Start]
            [OnEntry(nameof(Entry))]
            private class Init : State
            {
            }
        }

        /// <summary>
        /// Creates a receiver, then several senders that ping it.
        /// </summary>
        private static void TestEntry(ControlledRuntime runtime)
        {
            var receiver = runtime.CreateStateMachine(typeof(Receiver));
            var target = new PMachineValue(receiver, new List<string> { nameof(Ping) });
            for (var i = 0; i < Senders; i++)
            {
                runtime.CreateStateMachine(typeof(Sender), new Event(target));
            }
        }

        private static IEnumerable<TestCaseData> Configurations()
        {
            foreach (var seed in new uint[] { 0, 1, 7, 42, 1234 })
            {
                yield return new TestCaseData("random", seed).SetName($"TestEntryCreatesSeveralMachines_random_{seed}");
                yield return new TestCaseData("pct", seed).SetName($"TestEntryCreatesSeveralMachines_pct_{seed}");
            }
        }

        [TestCaseSource(nameof(Configurations))]
        public void TestEntryCreatesSeveralMachines(string strategy, uint seed)
        {
            var configuration = CheckerConfiguration.Create()
                .WithTestingIterations(Iterations)
                .WithMaxSchedulingSteps(1000)
                .WithRandomGeneratorSeed(seed);
            configuration = strategy == "pct"
                ? configuration.WithPCTStrategy(false, 3)
                : configuration.WithRandomStrategy();

            TestReport report;
            int received;
            lock (PCheckerRunner.CheckerLock)
            {
                pingsReceived = 0;
                var engine = TestingEngine.Create(configuration, (System.Action<ControlledRuntime>)TestEntry);
                engine.Run();
                report = engine.TestReport;
                received = pingsReceived;
            }

            Assert.That(report.BugReports, Is.Empty, "the checker reported a bug in a correct test");
            Assert.That(report.InternalErrors, Is.Empty);
            Assert.That(report.NumOfExploredUnfairSchedules + report.NumOfExploredFairSchedules,
                Is.EqualTo((int)Iterations));
            Assert.That(received, Is.EqualTo(Senders * (int)Iterations),
                "every machine created by the test entry must run in every schedule");
        }
    }
}
