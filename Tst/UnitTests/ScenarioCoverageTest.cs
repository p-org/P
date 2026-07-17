using System;
using System.IO;
using NUnit.Framework;
using PChecker;
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

            StringAssert.Contains("Scenario coverage:", text);
            StringAssert.Contains("ReadAfterWrite", text);
            StringAssert.Contains("NeverCovered", text);
            StringAssert.Contains("triggered in 1 schedule,", text);
            StringAssert.Contains("triggered in 0 schedules,", text);
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

            StringAssert.Contains("across 2 test case(s)", text);
            // S: 5+3 triggers, 2+1 unique timelines, covered in both.
            StringAssert.Contains("S: covered in 2/2 test cases, 8 total triggers, 3 unique satisfying timelines", text);
            // Gap: never satisfied anywhere; best partial progress is the max across test cases.
            StringAssert.Contains("Gap: covered in 0/2 test cases, 0 total triggers, 0 unique satisfying timelines (best partial progress: 2/4 states)", text);
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

                StringAssert.Contains("across 2 test case(s)", text);
                StringAssert.Contains("S: covered in 2/2 test cases, 2 total triggers, 2 unique satisfying timelines", text);
                StringAssert.Contains("Gap: covered in 0/1 test cases", text);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
