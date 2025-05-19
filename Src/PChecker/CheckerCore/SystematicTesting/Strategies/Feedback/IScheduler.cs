using System.Collections.Generic;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Probabilistic;

internal interface IScheduler
{
    public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next);
    public void Reset();
    public bool PrepareForNextIteration();
    public IScheduler Mutate();
    public IScheduler New();
}