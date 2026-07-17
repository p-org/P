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
    }
}
