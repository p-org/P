using System;
using System.IO;
using PChecker.Feedback;

namespace PChecker.SystematicTesting.Strategies.Feedback;

internal interface IFeedbackGuidedStrategy: ISchedulingStrategy
{
    public void ObserveRunningResults(EventPatternObserver patternObserver, TimelineObserver timelineObserver);
    public int TotalSavedInputs();
    public void DumpStats(TextWriter writer);
}