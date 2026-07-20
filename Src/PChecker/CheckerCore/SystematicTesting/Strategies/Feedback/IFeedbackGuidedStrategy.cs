using System.Collections.Generic;
using System.IO;
using PChecker.Feedback;

namespace PChecker.SystematicTesting.Strategies.Feedback;

internal interface IFeedbackGuidedStrategy: ISchedulingStrategy
{
    // scenarioReached: distinct states reached this run per scenario; scenarioSatisfied: scenarios
    // that reached a cold (accepting) state this run. Used to compute the coverage-novelty
    // steering signal for KEPT generators only (see FeedbackGuidedStrategy.ObserveRunningResults).
    public void ObserveRunningResults(TimelineObserver timelineObserver,
        IReadOnlyDictionary<string, int> scenarioReached, IReadOnlyCollection<string> scenarioSatisfied);
    public int TotalSavedInputs();
    public void DumpStats(TextWriter writer);
}