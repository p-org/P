using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PChecker;
using PChecker.Feedback;
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
                    new ScenarioCoverageEntry { Name = "S", Triggered = 5, UniqueTimelines = 2, MaxStatesReached = 3, TotalStates = 3 },
                    new ScenarioCoverageEntry { Name = "Gap", Triggered = 0, UniqueTimelines = 0, MaxStatesReached = 1, TotalStates = 4 },
                }
            };
            var tc2 = new ScenarioCoverageArtifact
            {
                TestCase = "tc2",
                Scenarios = new()
                {
                    new ScenarioCoverageEntry { Name = "S", Triggered = 3, UniqueTimelines = 1, MaxStatesReached = 3, TotalStates = 3 },
                    new ScenarioCoverageEntry { Name = "Gap", Triggered = 0, UniqueTimelines = 0, MaxStatesReached = 2, TotalStates = 4 },
                }
            };

            var text = ScenarioCoverageMerger.Merge(new[] { tc1, tc2 });

            StringAssert.Contains("across 2 test cases", text);
            // Summary: 1 of 2 scenarios covered, 1 gap.
            StringAssert.Contains("1/2 scenarios covered in >=1 test case; 1 coverage gap.", text);
            // S: covered in both, lists which test cases; 5+3 triggers, 2+1 timelines.
            StringAssert.Contains("[covered] S: covered in 2/2 test cases (tc1, tc2); 8 total triggers, 3 unique satisfying timelines", text);
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
                StringAssert.Contains("2 total triggers, 2 unique satisfying timelines", text);
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
                StringAssert.Contains("3 unique satisfying timelines", text);  // latest's count, not the older 1
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
