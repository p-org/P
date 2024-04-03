using PChecker.Feedback;
using PChecker.Random;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Probabilistic;

internal class POSStrategy: POSScheduler, ISchedulingStrategy
{
    /// <summary>
    /// Random value generator.
    /// </summary>
    private readonly IRandomValueGenerator RandomValueGenerator;

    public POSStrategy(int maxSteps, ConflictOpMonitor? monitor, IRandomValueGenerator random)
        : base(new RandomPriorizationProvider(random), monitor)
    {
    }

    public bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
    {
        throw new System.NotImplementedException();
    }

    public bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
    {
        throw new System.NotImplementedException();
    }

    public int GetScheduledSteps()
    {
        throw new System.NotImplementedException();
    }

    public bool HasReachedMaxSchedulingSteps()
    {
        throw new System.NotImplementedException();
    }

    public bool IsFair()
    {
        throw new System.NotImplementedException();
    }

    public string GetDescription()
    {
        throw new System.NotImplementedException();
    }
}