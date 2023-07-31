using System.Collections.Generic;

namespace PChecker.SystematicTesting.Strategies.Feedback;

internal interface IFeedbackGuidedStrategy: ISchedulingStrategy
{
    public void ObserveRunningResults(ControlledRuntime runtime);
    public int TotalSavedInputs();
    public int CurrentInputIndex();
}