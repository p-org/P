using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using System.Text.Json;
using PChecker;
using PChecker.Feedback;
using PChecker.Random;
using PChecker.Runtime;
using PChecker.SystematicTesting;

namespace UnitTests
{
    /// <summary>
    /// Unit tests for scenario-coverage accounting in <see cref="TestReport"/>
    /// (the aggregation + reporting side of the `scenario` feature). The end-to-end
    /// compile-and-run path is covered by the RegressionTests/.../ScenarioCoverageBasic fixture.
    /// </summary>
    [TestFixture]
    [TestOf(typeof(TestReport))]
    public class ScenarioCoverageTest
    {
        private static TestReport NewReport()
        {
            return new TestReport(CheckerConfiguration.Create());
        }

        [NUnit.Framework.Test]
        public void RecordScenarioSatisfied_CountsTriggersAndUniqueTimelines()
        {
            var report = NewReport();
            report.RecordScenarioSatisfied("ReadAfterWrite", "<M, w, r>");
            report.RecordScenarioSatisfied("ReadAfterWrite", "<M, w, r>");   // same timeline
            report.RecordScenarioSatisfied("ReadAfterWrite", "<M, w, r, x>"); // distinct timeline

            Assert.AreEqual(3, report.ScenarioTriggerCounts["ReadAfterWrite"]);
            Assert.AreEqual(2, report.ScenarioSatisfyingTimelines["ReadAfterWrite"].Count);
        }

        [NUnit.Framework.Test]
        public void EnsureScenarioTracked_SurfacesUncoveredScenariosWithZero()
        {
            var report = NewReport();
            report.EnsureScenarioTracked("NeverCovered");

            Assert.AreEqual(0, report.ScenarioTriggerCounts["NeverCovered"]);
            Assert.AreEqual(0, report.ScenarioSatisfyingTimelines["NeverCovered"].Count);

            // EnsureScenarioTracked must not clobber an already-recorded scenario.
            report.RecordScenarioSatisfied("Covered", "<M, a, b>");
            report.EnsureScenarioTracked("Covered");
            Assert.AreEqual(1, report.ScenarioTriggerCounts["Covered"]);
            Assert.AreEqual(1, report.ScenarioSatisfyingTimelines["Covered"].Count);
        }

        [NUnit.Framework.Test]
        public void Merge_SumsCountsAndUnionsSatisfyingTimelines()
        {
            var a = NewReport();
            a.RecordScenarioSatisfied("S", "<t1>");
            a.EnsureScenarioTracked("Uncovered");

            var b = NewReport();
            b.RecordScenarioSatisfied("S", "<t1>"); // duplicate timeline across workers
            b.RecordScenarioSatisfied("S", "<t2>");

            a.Merge(b);

            Assert.AreEqual(3, a.ScenarioTriggerCounts["S"]);                 // 1 + 2
            Assert.AreEqual(2, a.ScenarioSatisfyingTimelines["S"].Count);     // {t1, t2}
            Assert.AreEqual(0, a.ScenarioTriggerCounts["Uncovered"]);         // preserved
        }

        [NUnit.Framework.Test]
        public void GetText_ReportsScenarioCoverageIncludingZeroCoverage()
        {
            var report = NewReport();
            report.EnsureScenarioTracked("NeverCovered");
            report.RecordScenarioSatisfied("ReadAfterWrite", "<M, w, r>");

            var text = report.GetText(CheckerConfiguration.Create());

            // Summary line leads with the covered count and flags gaps.
            StringAssert.Contains("Scenario coverage: 1/2 scenarios covered (1 gap)", text);
            // Covered scenario is marked and shows its trigger/timeline counts.
            StringAssert.Contains("[covered] ReadAfterWrite: triggered in 1 schedule, 1 unique satisfying timeline", text);
            // The uncovered scenario is called out as a gap, not buried in a "0 schedules" line.
            StringAssert.Contains("[  GAP  ] NeverCovered: not covered", text);
        }

        [NUnit.Framework.Test]
        public void GetText_SummaryCountsAndGroupsCoveredBeforeGaps()
        {
            var report = NewReport();
            report.RecordScenarioSatisfied("Alpha", "<t1>");
            report.RecordScenarioSatisfied("Beta", "<t2>");
            report.EnsureScenarioTracked("Zeta");   // a gap
            report.RecordScenarioProgress("Zeta", 1, 2);

            var text = report.GetText(CheckerConfiguration.Create());

            StringAssert.Contains("Scenario coverage: 2/3 scenarios covered (1 gap)", text);
            StringAssert.Contains("[covered] Alpha", text);
            StringAssert.Contains("[covered] Beta", text);
            StringAssert.Contains("[  GAP  ] Zeta: not covered (best partial progress: 1/2 states)", text);
            // Covered scenarios are listed before the gaps so gaps stand out at the end.
            Assert.Less(text.IndexOf("[covered] Beta"), text.IndexOf("[  GAP  ] Zeta"));
        }

        [NUnit.Framework.Test]
        public void RecordScenarioProgress_KeepsBestAndReportsPartialForUncovered()
        {
            var report = NewReport();
            report.EnsureScenarioTracked("NeverCovered");
            report.RecordScenarioProgress("NeverCovered", 1, 3);
            report.RecordScenarioProgress("NeverCovered", 2, 3); // better
            report.RecordScenarioProgress("NeverCovered", 1, 3); // worse, ignored

            Assert.AreEqual(2, report.ScenarioMaxStatesReached["NeverCovered"]);
            Assert.AreEqual(3, report.ScenarioTotalStates["NeverCovered"]);

            var text = report.GetText(CheckerConfiguration.Create());
            StringAssert.Contains("best partial progress: 2/3 states", text);
        }

        [NUnit.Framework.Test]
        public void Merge_TakesMaxPartialProgress()
        {
            var a = NewReport();
            a.RecordScenarioProgress("S", 1, 4);
            var b = NewReport();
            b.RecordScenarioProgress("S", 3, 4);

            a.Merge(b);

            Assert.AreEqual(3, a.ScenarioMaxStatesReached["S"]);
            Assert.AreEqual(4, a.ScenarioTotalStates["S"]);
        }

        [NUnit.Framework.Test]
        public void ScenarioMerger_AggregatesAcrossTestCases()
        {
            var tc1 = new ScenarioCoverageArtifact
            {
                TestCase = "tc1",
                Scenarios = new()
                {
                    new ScenarioCoverageEntry { Name = "S", Satisfied = true, SatisfyingSchedules = 5, DistinctSatisfyingTimelines = 2, MaxStatesVisited = 3, MonitorStates = 3 },
                    new ScenarioCoverageEntry { Name = "Gap", Satisfied = false, SatisfyingSchedules = 0, DistinctSatisfyingTimelines = 0, MaxStatesVisited = 1, MonitorStates = 4 },
                }
            };
            var tc2 = new ScenarioCoverageArtifact
            {
                TestCase = "tc2",
                Scenarios = new()
                {
                    new ScenarioCoverageEntry { Name = "S", Satisfied = true, SatisfyingSchedules = 3, DistinctSatisfyingTimelines = 1, MaxStatesVisited = 3, MonitorStates = 3 },
                    new ScenarioCoverageEntry { Name = "Gap", Satisfied = false, SatisfyingSchedules = 0, DistinctSatisfyingTimelines = 0, MaxStatesVisited = 2, MonitorStates = 4 },
                }
            };

            var text = ScenarioCoverageMerger.Merge(new[] { tc1, tc2 });

            StringAssert.Contains("across 2 test cases", text);
            // Summary: 1 of 2 scenarios covered, 1 gap.
            StringAssert.Contains("1/2 scenarios covered in >=1 test case; 1 coverage gap.", text);
            // S: covered in both, lists which test cases; 5+3 triggers, 2+1 timelines.
            StringAssert.Contains("[covered] S: covered in 2/2 test cases (tc1, tc2); 8 total triggers, 3 satisfying timelines (summed per test case)", text);
            // Gap: never satisfied anywhere; best progress is the max across test cases.
            StringAssert.Contains("[  GAP  ] Gap: never covered in any of 2 test cases; best progress anywhere 2/4 states", text);
        }

        [NUnit.Framework.Test]
        public void MergeDirectory_ReadsArtifactsRecursivelyFromSubdirectories()
        {
            var root = Path.Combine(Path.GetTempPath(), "scencov_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "run1"));
            Directory.CreateDirectory(Path.Combine(root, "run2"));
            try
            {
                var r1 = NewReport();
                r1.RecordScenarioSatisfied("S", "<t1>");
                ScenarioCoverageMerger.Write(r1, "tc1", Path.Combine(root, "run1", "a" + ScenarioCoverageMerger.FileSuffix));

                var r2 = NewReport();
                r2.RecordScenarioSatisfied("S", "<t2>");
                r2.EnsureScenarioTracked("Gap");
                r2.RecordScenarioProgress("Gap", 1, 3);
                ScenarioCoverageMerger.Write(r2, "tc2", Path.Combine(root, "run2", "b" + ScenarioCoverageMerger.FileSuffix));

                var text = ScenarioCoverageMerger.MergeDirectory(root);

                StringAssert.Contains("across 2 test cases", text);
                // S covered in both; assert counts + that both test cases are listed (file
                // enumeration order is filesystem-dependent, so don't pin the order).
                StringAssert.Contains("[covered] S: covered in 2/2 test cases", text);
                StringAssert.Contains("2 total triggers, 2 satisfying timelines (summed per test case)", text);
                StringAssert.Contains("tc1", text);
                StringAssert.Contains("tc2", text);
                // Gap seen in only one test case, never covered.
                StringAssert.Contains("[  GAP  ] Gap: never covered in any of 1 test case", text);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        // ── Coverage-novelty steering signal (ScenarioSteering.NoveltyCompliance) ──

        private static double Novelty(
            Dictionary<string, int> reached, IReadOnlyCollection<string> satisfied,
            Dictionary<string, int> suiteBest, HashSet<string> suiteSatisfied)
            => ScenarioSteering.NoveltyCompliance(reached, satisfied, suiteBest, suiteSatisfied);

        [NUnit.Framework.Test]
        public void Novelty_NoScenarios_IsZero()
        {
            var best = new Dictionary<string, int>();
            var sat = new HashSet<string>();
            Assert.AreEqual(0.0, Novelty(new(), Array.Empty<string>(), best, sat));
        }

        [NUnit.Framework.Test]
        public void Novelty_FirstSatisfactionScoresOnce_ThenZero()
        {
            var best = new Dictionary<string, int>();
            var sat = new HashSet<string>();

            // First run to satisfy "S" is novel.
            Assert.AreEqual(1.0, Novelty(new() { ["S"] = 3 }, new[] { "S" }, best, sat));
            Assert.IsTrue(sat.Contains("S"));
            // A later run that satisfies the same "S" again earns nothing.
            Assert.AreEqual(0.0, Novelty(new() { ["S"] = 3 }, new[] { "S" }, best, sat));
        }

        [NUnit.Framework.Test]
        public void Novelty_AdvancingPartialProgressScores_PlateauDoesNot()
        {
            var best = new Dictionary<string, int>();
            var sat = new HashSet<string>();

            Assert.AreEqual(1.0, Novelty(new() { ["Rare"] = 1 }, Array.Empty<string>(), best, sat)); // 0 -> 1
            Assert.AreEqual(1.0, Novelty(new() { ["Rare"] = 2 }, Array.Empty<string>(), best, sat)); // 1 -> 2
            Assert.AreEqual(0.0, Novelty(new() { ["Rare"] = 2 }, Array.Empty<string>(), best, sat)); // 2 -> 2 (no gain)
            Assert.AreEqual(0.0, Novelty(new() { ["Rare"] = 1 }, Array.Empty<string>(), best, sat)); // regress: no gain
            Assert.AreEqual(2, best["Rare"]);
        }

        [NUnit.Framework.Test]
        public void Novelty_ImpossibleScenarioBoostsOnceThenSaturatesToZero()
        {
            // The bug this fix addresses: a scenario stuck at a constant partial progress
            // must NOT keep contributing a constant signal. It scores once (reaching its
            // ceiling) then never again — so the signal cannot saturate the priority.
            var best = new Dictionary<string, int>();
            var sat = new HashSet<string>();

            Assert.AreEqual(1.0, Novelty(new() { ["Impossible"] = 2 }, Array.Empty<string>(), best, sat));
            for (var i = 0; i < 5; i++)
            {
                Assert.AreEqual(0.0, Novelty(new() { ["Impossible"] = 2 }, Array.Empty<string>(), best, sat));
            }
        }

        [NUnit.Framework.Test]
        public void Novelty_ProgressOnAlreadySatisfiedScenarioIsIgnored()
        {
            var best = new Dictionary<string, int>();
            var sat = new HashSet<string> { "S" };   // already satisfied earlier in the suite

            // Even a brand-new furthest state for S earns nothing once S is covered.
            Assert.AreEqual(0.0, Novelty(new() { ["S"] = 9 }, Array.Empty<string>(), best, sat));
            Assert.IsFalse(best.ContainsKey("S"));
        }

        // ── Output-folder robustness: per-test-case folders + latest-only merge ──

        [NUnit.Framework.Test]
        public void SetOutputDirectory_GroupsOutputByTestCase()
        {
            var root = Path.Combine(Path.GetTempPath(), "outdir_" + Guid.NewGuid().ToString("N"));
            try
            {
                var cfg = CheckerConfiguration.Create();
                cfg.OutputPath = root;
                cfg.AssemblyToBeAnalyzed = "/x/MyModel.dll";
                cfg.TestCaseName = "tcFoo";
                cfg.SetOutputDirectory();
                // With a test case, the layout is <root>/tcFoo/<Mode>/.
                StringAssert.Contains(Path.Combine("tcFoo", cfg.Mode.ToString()), cfg.OutputDirectory);

                var anon = CheckerConfiguration.Create();
                anon.OutputPath = root;
                anon.AssemblyToBeAnalyzed = "/x/MyModel.dll";
                anon.TestCaseName = "";  // no test case -> layout unchanged (no extra segment)
                anon.SetOutputDirectory();
                StringAssert.DoesNotContain(Path.Combine("tcFoo", anon.Mode.ToString()), anon.OutputDirectory);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [NUnit.Framework.Test]
        public void MergeDirectory_DedupesReRunsOfSameTestCaseKeepingLatest()
        {
            var root = Path.Combine(Path.GetTempPath(), "scencov_dedup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "BugFinding"));   // latest run
            Directory.CreateDirectory(Path.Combine(root, "BugFinding0"));  // rotated history
            try
            {
                // Older run of test case "tc": S covered with 1 distinct timeline.
                var older = NewReport();
                older.RecordScenarioSatisfied("S", "<t1>");
                var olderPath = Path.Combine(root, "BugFinding0", "A" + ScenarioCoverageMerger.FileSuffix);
                ScenarioCoverageMerger.Write(older, "tc", olderPath);
                File.SetLastWriteTimeUtc(olderPath, DateTime.UtcNow.AddMinutes(-10));

                // Latest run of the SAME test case "tc": S covered with 3 distinct timelines.
                var latest = NewReport();
                latest.RecordScenarioSatisfied("S", "<t1>");
                latest.RecordScenarioSatisfied("S", "<t2>");
                latest.RecordScenarioSatisfied("S", "<t3>");
                var latestPath = Path.Combine(root, "BugFinding", "A" + ScenarioCoverageMerger.FileSuffix);
                ScenarioCoverageMerger.Write(latest, "tc", latestPath);
                File.SetLastWriteTimeUtc(latestPath, DateTime.UtcNow);

                var text = ScenarioCoverageMerger.MergeDirectory(root);

                // Deduped to ONE test case (the latest run) — not double-counted as two.
                StringAssert.Contains("across 1 test case", text);
                StringAssert.Contains("covered in 1/1 test cases", text);
                StringAssert.Contains("3 satisfying timelines (summed per test case)", text);  // latest's count, not the older 1
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [NUnit.Framework.Test]
        public void Artifact_UsesClearVersionedSchema()
        {
            var report = NewReport();
            report.RecordScenarioSatisfied("Covered", "<t1>");
            report.EnsureScenarioTracked("Gap");
            report.RecordScenarioProgress("Gap", 1, 3);

            var dir = Path.Combine(Path.GetTempPath(), "scencov_fmt_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var path = Path.Combine(dir, "A" + ScenarioCoverageMerger.FileSuffix);
                ScenarioCoverageMerger.Write(report, "tcX", path);
                var json = File.ReadAllText(path);

                // Versioned, self-describing, camelCase; satisfaction is explicit.
                StringAssert.Contains("\"version\": 1", json);
                StringAssert.Contains("\"testCase\": \"tcX\"", json);
                StringAssert.Contains("\"satisfied\": true", json);   // Covered
                StringAssert.Contains("\"satisfied\": false", json);  // Gap
                StringAssert.Contains("\"satisfyingSchedules\": 1", json);
                StringAssert.Contains("\"distinctSatisfyingTimelines\": 1", json);
                StringAssert.Contains("\"maxStatesVisited\"", json);
                StringAssert.Contains("\"monitorStates\": 3", json);  // Gap's total states

                // And it round-trips back through the merger.
                StringAssert.Contains("Covered", ScenarioCoverageMerger.MergeDirectory(dir));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        // ── Satisfaction detection: a cold START state must not be trivially "covered" ──

        [NUnit.Framework.Test]
        public void IsSatisfyingEntry_ColdStateSatisfies_ExceptTheStartEntry()
        {
            // A cold (accepting) state entry satisfies the scenario...
            Assert.IsTrue(ScenarioSteering.IsSatisfyingEntry(isInHotState: false, isStartEntry: false));
            // ...UNLESS it is the monitor's first (start) state entry: a `cold start state`
            // fires during RegisterMonitor before any behavior, and must NOT count as covered.
            Assert.IsFalse(ScenarioSteering.IsSatisfyingEntry(isInHotState: false, isStartEntry: true));
            // Hot states never satisfy; unmarked/warm (null) states never satisfy.
            Assert.IsFalse(ScenarioSteering.IsSatisfyingEntry(isInHotState: true, isStartEntry: false));
            Assert.IsFalse(ScenarioSteering.IsSatisfyingEntry(isInHotState: true, isStartEntry: true));
            Assert.IsFalse(ScenarioSteering.IsSatisfyingEntry(isInHotState: null, isStartEntry: false));
            Assert.IsFalse(ScenarioSteering.IsSatisfyingEntry(isInHotState: null, isStartEntry: true));
        }

        [NUnit.Framework.Test]
        public void MergeDirectory_SkipsArtifactsFromANewerSchemaVersion()
        {
            var root = Path.Combine(Path.GetTempPath(), "scencov_ver_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                // A current (v1) artifact.
                var ok = NewReport();
                ok.RecordScenarioSatisfied("S", "<t1>");
                ScenarioCoverageMerger.Write(ok, "tcCurrent", Path.Combine(root, "cur" + ScenarioCoverageMerger.FileSuffix));

                // A future-schema artifact (version bumped): fields may mean something else, so it
                // must be SKIPPED, not silently miscounted as v1.
                File.WriteAllText(Path.Combine(root, "future" + ScenarioCoverageMerger.FileSuffix),
                    "{\"version\": 999, \"testCase\": \"tcFuture\", \"scenarios\": [" +
                    "{\"name\": \"S\", \"satisfied\": true, \"satisfyingSchedules\": 7, " +
                    "\"distinctSatisfyingTimelines\": 3, \"maxStatesVisited\": 3, \"monitorStates\": 3}]}");

                var text = ScenarioCoverageMerger.MergeDirectory(root);

                StringAssert.Contains("across 1 test case", text);   // only the v1 artifact counted
                StringAssert.Contains("tcCurrent", text);
                StringAssert.DoesNotContain("tcFuture", text);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        // ── Observer: drives ScenarioComplianceObserver directly (the end-to-end seam the
        // pure IsSatisfyingEntry test cannot reach). Touches PModule static state, so kept
        // NonParallelizable and always cleaned up. ──

        private class DummyScenarioMonitor { }

        [NUnit.Framework.Test]
        [NonParallelizable]
        public void Observer_ColdStartEntryDoesNotSatisfy_ButReEntryDoes()
        {
            PModule.coverageMonitors.Clear();
            PModule.scenarioStateCounts.Clear();
            try
            {
                PModule.coverageMonitors.Add(typeof(DummyScenarioMonitor));
                PModule.scenarioStateCounts[typeof(DummyScenarioMonitor)] = 2;
                var mt = typeof(DummyScenarioMonitor).FullName;

                var obs = new ScenarioComplianceObserver();
                // First entry is the monitor's START state, logged during RegisterMonitor. Even
                // if it is cold, it must NOT satisfy (no behavior observed yet).
                obs.OnMonitorStateTransition(mt, "Accept", isEntry: true, isInHotState: false);
                Assert.AreEqual(0, obs.SatisfiedScenarios.Count, "cold start entry must not satisfy");
                // A later cold-state entry (reached through observed behavior) DOES satisfy.
                obs.OnMonitorStateTransition(mt, "Accept", isEntry: true, isInHotState: false);
                Assert.AreEqual(1, obs.SatisfiedScenarios.Count, "re-entry into a cold state satisfies");
            }
            finally
            {
                PModule.coverageMonitors.Clear();
                PModule.scenarioStateCounts.Clear();
            }
        }

        [NUnit.Framework.Test]
        [NonParallelizable]
        public void Observer_RefreshPicksUpMonitorsPopulatedAfterConstruction_AndNormalPathSatisfies()
        {
            PModule.coverageMonitors.Clear();
            PModule.scenarioStateCounts.Clear();
            try
            {
                // Constructed while PModule is EMPTY (as it is in a real run: the generated
                // InitializeMonitorMap populates PModule AFTER the observer exists).
                var obs = new ScenarioComplianceObserver();
                Assert.IsFalse(obs.HasScenarios);

                PModule.coverageMonitors.Add(typeof(DummyScenarioMonitor));
                PModule.scenarioStateCounts[typeof(DummyScenarioMonitor)] = 2;
                var mt = typeof(DummyScenarioMonitor).FullName;

                // Normal scenario: hot start entry (does not satisfy) then a cold accept via behavior.
                obs.OnMonitorStateTransition(mt, "Init", isEntry: true, isInHotState: true);
                obs.OnMonitorStateTransition(mt, "Accept", isEntry: true, isInHotState: false);

                Assert.IsTrue(obs.HasScenarios, "Refresh must pick up monitors registered after construction");
                Assert.IsNotEmpty(obs.AllScenarioNames);
                Assert.AreEqual(1, obs.SatisfiedScenarios.Count, "hot-start -> cold-accept must satisfy");
            }
            finally
            {
                PModule.coverageMonitors.Clear();
                PModule.scenarioStateCounts.Clear();
            }
        }

        // ── Merge/report robustness: graceful degradation + zero-scenario contracts ──

        [NUnit.Framework.Test]
        public void MergeDirectory_SkipsCorruptJson_AndMergesTheValidArtifact()
        {
            var root = Path.Combine(Path.GetTempPath(), "scencov_bad_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var ok = NewReport();
                ok.RecordScenarioSatisfied("S", "<t1>");
                ScenarioCoverageMerger.Write(ok, "tcGood", Path.Combine(root, "good" + ScenarioCoverageMerger.FileSuffix));

                // A truncated/corrupt artifact must be skipped, not crash the merge.
                File.WriteAllText(Path.Combine(root, "bad" + ScenarioCoverageMerger.FileSuffix), "{ this is not valid json ");

                var text = ScenarioCoverageMerger.MergeDirectory(root);

                StringAssert.Contains("across 1 test case", text);
                StringAssert.Contains("tcGood", text);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [NUnit.Framework.Test]
        public void MergeDirectory_EmptyDirectory_ReportsZeroTestCasesWithoutCrashing()
        {
            var root = Path.Combine(Path.GetTempPath(), "scencov_empty_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var text = ScenarioCoverageMerger.MergeDirectory(root);
                StringAssert.Contains("across 0 test cases", text);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [NUnit.Framework.Test]
        public void ZeroScenarios_ReportOmitsBlock_AndWriteIsNoOp()
        {
            var report = NewReport();   // no scenarios recorded

            // The per-run report must not print a scenario-coverage block when there are none.
            StringAssert.DoesNotContain("Scenario coverage", report.GetText(CheckerConfiguration.Create()));

            // And Write must not create an artifact file.
            var dir = Path.Combine(Path.GetTempPath(), "scencov_none_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var path = Path.Combine(dir, "none" + ScenarioCoverageMerger.FileSuffix);
                ScenarioCoverageMerger.Write(report, "tcEmpty", path);
                Assert.IsFalse(File.Exists(path), "no scenarios -> no artifact written");
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        // ── Determinism: same seed -> identical artifact (RNG reseed x accounting x serialization) ──

        [NUnit.Framework.Test]
        public void SameSeed_ProducesIdenticalScenarioArtifact_DifferentSeedDiffers()
        {
            string RunOnce(uint seed)
            {
                var cfg = CheckerConfiguration.Create();
                cfg.RandomGeneratorSeed = seed;
                var rng = new RandomValueGenerator(cfg);   // reseeds deterministically from the seed
                var report = new TestReport(cfg);
                report.EnsureScenarioTracked("Alpha");
                report.EnsureScenarioTracked("Beta");
                for (var i = 0; i < 50; i++)
                {
                    if (rng.Next(2) == 0)
                    {
                        report.RecordScenarioSatisfied("Alpha", "<t" + rng.Next(5) + ">");
                    }
                    else
                    {
                        report.RecordScenarioProgress("Beta", rng.Next(3), 3);
                    }
                }
                return JsonSerializer.Serialize(
                    ScenarioCoverageMerger.FromReport(report, "tcDet"),
                    new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }

            Assert.AreEqual(RunOnce(12345), RunOnce(12345), "same seed must produce a byte-identical artifact");
            Assert.AreNotEqual(RunOnce(12345), RunOnce(99999), "different seeds should produce different coverage");
        }
    }
}
