using System.IO;
using PChecker.Feedback;

namespace PChecker.SystematicTesting.Strategies.Feedback;

internal interface IFeedbackGuidedStrategy: ISchedulingStrategy
{
    public void ObserveRunningResults(TimelineObserver timelineObserver, double scenarioCompliance);
    public int TotalSavedInputs();
    public void DumpStats(TextWriter writer);
}