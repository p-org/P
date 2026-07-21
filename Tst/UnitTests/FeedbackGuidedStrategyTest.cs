using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PChecker;
using PChecker.Feedback;
using PChecker.Generator.Object;
using PChecker.Runtime;
using PChecker.Runtime.Events;
using PChecker.Runtime.StateMachines;
using PChecker.SystematicTesting.Strategies.Feedback;
using PChecker.SystematicTesting.Strategies.Probabilistic;

namespace UnitTests
{
    /// <summary>
    /// Integration tests for <see cref="FeedbackGuidedStrategy.ObserveRunningResults"/> — the
    /// scenario-steering seam that the pure <c>ScenarioSteering.NoveltyCompliance</c> tests cannot
    /// reach. Two properties:
    ///  (i)  a timeline-redundant (diversity ≤ 0) schedule is DISCARDED before its scenario progress
    ///       is folded into the suite state, so it can't rob a later kept schedule of the novelty
    ///       boost (guards the `if (diversity <= 0) return;` ordering before NoveltyCompliance);
    ///  (ii) suite-state is per-strategy-instance, not shared static (guards the instance fields
    ///       _scenarioSuiteBestReached / _scenarioSuiteSatisfied).
    /// Both are asserted by COMPARING two strategies (no brittle absolute priority values).
    /// </summary>
    [TestFixture]
    public class FeedbackGuidedStrategyTest
    {
        private static FeedbackGuidedStrategy NewStrategy()
        {
            var cfg = CheckerConfiguration.Create();
            var gen = new ControlledRandom(new System.Random(0));
            var sched = new RandomScheduler(gen);
            return new FeedbackGuidedStrategy(cfg, gen, sched);
        }

        // A TimelineObserver populated by delivering `events` to a receiver named `receiver`.
        // Identical (receiver, events) yield the identical abstract timeline (the exact-duplicate
        // novelty gate); a different receiver/events yields a distinct one.
        private static TimelineObserver Obs(string receiver, params Event[] events)
        {
            var id = new StateMachineId(typeof(FeedbackGuidedStrategyTest), receiver, null, useNameForHashing: true);
            var obs = new TimelineObserver();
            foreach (var e in events)
            {
                obs.OnDequeueEvent(id, "", e, null, new VectorTime(id));
            }
            return obs;
        }

        private static IReadOnlyDictionary<string, int> Reached(string s, int n) => new Dictionary<string, int> { [s] = n };
        private static IReadOnlyCollection<string> Sat(params string[] s) => s;
        private static readonly IReadOnlyDictionary<string, int> NoReached = new Dictionary<string, int>();
        private static readonly IReadOnlyCollection<string> NoSat = Array.Empty<string>();

        // Sorted saved-generator priorities (reflection: _savedGenerators is private; GeneratorRecord
        // and its Priority are public).
        private static List<double> Priorities(FeedbackGuidedStrategy s)
        {
            var field = typeof(FeedbackGuidedStrategy).GetField("_savedGenerators",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var list = (IEnumerable<FeedbackGuidedStrategy.GeneratorRecord>)field.GetValue(s);
            return list.Select(r => r.Priority).OrderBy(x => x).ToList();
        }

        private static void AssertPrioritiesEqual(List<double> a, List<double> b)
        {
            Assert.AreEqual(a.Count, b.Count, "different number of saved generators");
            for (var i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i], b[i], 1e-9, $"priority #{i} differs");
            }
        }

        private class EvA : Event { }
        private class EvB : Event { }

        [NUnit.Framework.Test]
        public void DiscardedScheduleDoesNotConsumeNovelty()
        {
            // Strategy A: a non-scenario first schedule, then a distinct schedule that first-satisfies S.
            var a = NewStrategy();
            a.ObserveRunningResults(Obs("M", new EvA(), new EvB()), NoReached, NoSat);
            a.ObserveRunningResults(Obs("N", new EvA(), new EvB()), Reached("S", 3), Sat("S"));

            // Strategy B: identical, but with an extra TIMELINE-DUPLICATE schedule (of the first) that
            // ALSO carries S-progress, inserted before the distinct one. It must be discarded and must
            // not consume S's novelty — so B ends identical to A.
            var b = NewStrategy();
            b.ObserveRunningResults(Obs("M", new EvA(), new EvB()), NoReached, NoSat);
            b.ObserveRunningResults(Obs("M", new EvA(), new EvB()), Reached("S", 3), Sat("S")); // duplicate → discarded
            b.ObserveRunningResults(Obs("N", new EvA(), new EvB()), Reached("S", 3), Sat("S"));

            // The duplicate was dropped (both saved exactly two generators, not three)...
            Assert.AreEqual(2, a.TotalSavedInputs());
            Assert.AreEqual(2, b.TotalSavedInputs(), "the timeline-duplicate schedule must be discarded");
            // ...and it consumed no novelty: the distinct S-satisfying schedule earned the same boost
            // in both, so the saved priorities are identical. (If the discard had folded S into the
            // suite state, B's last schedule would have earned compliance 0 and a lower priority.)
            AssertPrioritiesEqual(Priorities(a), Priorities(b));
        }

        [NUnit.Framework.Test]
        public void SuiteStateIsPerInstance()
        {
            // Two independent strategies each first-satisfy S on their first (identical) schedule.
            // Each observation is the instance's first, so diversity is 1.0 and — if suite-state is
            // per-instance — compliance is 1.0, giving priority 1.0*(1+1)=2.0 in BOTH. If suite-state
            // were a shared static, the second instance would see S already satisfied → compliance 0
            // → priority 1.0, and the two would differ.
            var s1 = NewStrategy();
            s1.ObserveRunningResults(Obs("M", new EvA(), new EvB()), Reached("S", 3), Sat("S"));
            var s2 = NewStrategy();
            s2.ObserveRunningResults(Obs("M", new EvA(), new EvB()), Reached("S", 3), Sat("S"));

            var p1 = Priorities(s1);
            var p2 = Priorities(s2);
            AssertPrioritiesEqual(p1, p2);
            Assert.AreEqual(1, p1.Count);
            Assert.AreEqual(2.0, p1[0], 1e-9, "first-satisfying schedule should earn the novelty boost (priority 2.0)");
        }
    }
}
