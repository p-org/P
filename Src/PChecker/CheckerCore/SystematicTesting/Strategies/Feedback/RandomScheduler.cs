using System.Collections.Generic;
using System.Linq;
using PChecker.Generator.Object;
using PChecker.Random;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Probabilistic;

internal class RandomScheduler : IScheduler
{
    private readonly IRandomValueGenerator _randomValueGenerator;
    public RandomScheduler(IRandomValueGenerator randomValueGenerator)
    {
        _randomValueGenerator = randomValueGenerator;
    }

    public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
    {
        var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
        next = null;
        if (enabledOperations.Count == 0)
        {
            return false;
        }

        if (enabledOperations.Count == 1)
        {
            next = enabledOperations[0];
            return true;
        }
        var idx = _randomValueGenerator.Next(enabledOperations.Count);
        next = enabledOperations[idx];
        return true;
    }

    public void Reset()
    {
    }

    public bool PrepareForNextIteration()
    {
        return true;
    }

    IScheduler IScheduler.Mutate()
    {
        return new RandomScheduler(((ControlledRandom)_randomValueGenerator).Mutate());
    }

    IScheduler IScheduler.New()
    {
        return new RandomScheduler(((ControlledRandom)_randomValueGenerator).New());
    }
}