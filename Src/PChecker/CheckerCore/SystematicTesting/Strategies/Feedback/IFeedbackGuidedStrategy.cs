using PChecker.Feedback;

namespace PChecker.SystematicTesting.Strategies.Feedback;

internal interface IFeedbackGuidedStrategy: ISchedulingStrategy
{
    public void ObserveRunningResults(EventPatternObserver patternObserver, ControlledRuntime runtime);
    public int TotalSavedInputs();
}