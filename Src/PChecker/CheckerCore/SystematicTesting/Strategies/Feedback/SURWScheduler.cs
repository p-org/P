using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using PChecker.Random;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Strategies.Probabilistic;

namespace PChecker.SystematicTesting.Strategies.Feedback;

internal class SURWScheduler: IScheduler
{
    private readonly IRandomValueGenerator _randomValueGenerator;
    private Dictionary<string, int> _executionLength;

    public SURWScheduler(Dictionary<string, int> executionLength, IRandomValueGenerator random)
    {
        _executionLength = executionLength;
        _randomValueGenerator = random;
    }

    public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
    {
        next = null;
        var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
        if (enabledOperations.Count == 0)
        {
            return false;
        }
        foreach (var op in enabledOperations)
        {
        }

        return true;
    }

    public void Reset()
    {
        throw new System.NotImplementedException();
    }

    public bool PrepareForNextIteration()
    {
        throw new System.NotImplementedException();
    }

    public IScheduler Mutate()
    {
        throw new System.NotImplementedException();
    }

    public IScheduler New()
    {
        throw new System.NotImplementedException();
    }
}