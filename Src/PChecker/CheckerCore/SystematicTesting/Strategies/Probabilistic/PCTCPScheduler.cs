using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PChecker.Actors.Logging;
using PChecker.Feedback;
using PChecker.IO.Debugging;
using PChecker.SystematicTesting.Operations;
using PChecker.SystematicTesting.Strategies.Probabilistic.pctcp;

namespace PChecker.SystematicTesting.Strategies.Probabilistic;

internal class PCTCPScheduler : PrioritizedScheduler
{
    public readonly PriorizationProvider Provider;

    private int ScheduledSteps;
    public readonly int MaxPrioritySwitchPoints;
    public int ScheduleLength;
    private int _nextPriorityChangePoint;
    private int _numSwitchPointsLeft;
    private int _nextOperationId = 0;
    private Dictionary<String, int> _chainedOperations = new();
    private List<Chain> _chains = new();
    private Dictionary<int, Dictionary<string, int>> _vectorClockMap = new();
    private Dictionary<int, HashSet<int>> _predMap = new();
    private Dictionary<int, OperationWithId> _operationMap = new();
    private Dictionary<int, Chain> _chainMap = new();
    private Dictionary<int, int> _nextOperationMap = new();
    public VectorClockGenerator vcGenerator;

    public PCTCPScheduler(int maxPrioritySwitchPoints, int scheduleLength, PriorizationProvider provider)
    {
        Provider = provider;
        ScheduledSteps = 0;
        ScheduleLength = scheduleLength;
        MaxPrioritySwitchPoints = maxPrioritySwitchPoints;
        _numSwitchPointsLeft = maxPrioritySwitchPoints;

        double switchPointProbability = 0.1;
        if (ScheduleLength != 0)
        {
            switchPointProbability = 1.0 * _numSwitchPointsLeft / (ScheduleLength - ScheduledSteps + 1);
        }

        _nextPriorityChangePoint =
            Generator.Mutator.Utils.SampleGeometric(switchPointProbability, Provider.SwitchPointChoice());
    }

    public virtual bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops,
        out AsyncOperation next)
    {
        next = null;
        var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
        if (enabledOperations.Count == 0)
        {
            if (_nextPriorityChangePoint == ScheduledSteps)
            {
                MovePriorityChangePointForward();
            }

            return false;
        }

        var highestEnabledOp = GetPrioritizedOperation(enabledOperations, current);
        if (next is null)
        {
            next = highestEnabledOp;
        }

        ScheduledSteps++;
        return true;
    }

    private void OnNewOperation(AsyncOperation operation)
    {
        Dictionary<String, int> vc;
        if (vcGenerator.ContextVcMap.ContainsKey(operation.Name))
        {
            vc = vcGenerator
                .ContextVcMap[operation.Name]
                .ToDictionary(entry => entry.Key, entry => entry.Value);
        }
        else
        {
            vc = new();
        }

        OperationWithId op;
        if (operation.Type == AsyncOperationType.Send)
        {
            op = new OperationWithId(operation.Name, operation.LastSentReceiver, operation
                .LastEvent!.Loc, _nextOperationId++);
        }
        else
        {
            op = new OperationWithId(operation.Name, "", 0, _nextOperationId++);
        }

        _vectorClockMap[op.Id] = vc;
        _chainedOperations[operation.Name] = op.Id;
        _operationMap[op.Id] = op;
        _predMap[op.Id] = new();

        for (int i = 0; i < op.Id - 1; i++)
        {
            if (IsLT(_vectorClockMap[i], _vectorClockMap[op.Id]))
            {
                _predMap[op.Id].Add(i);
            }
        }

        if (!PlaceInChains(op))
        {
            Chain newChain = new();
            newChain.Ops.Add(op);
            _chainMap[op.Id] = newChain;
            if (_chains.Count == 0)
            {
                _chains.Add(newChain);
            }
            else
            {
                var index = Provider.AssignPriority(_chains.Count);
                _chains.Insert(index, newChain);
            }
        }
    }

    private bool PlaceInChains(OperationWithId op)
    {
        // var currentQ = _chains.
        for (int i = 0; i < _chains.Count; i++)
        {
            var chain = _chains[i];
            if (chain.Ops.Count > 0)
            {
                var tail = chain.Ops.Last();
                if (IsLT(_vectorClockMap[tail.Id], _vectorClockMap[op.Id]))
                {
                    chain.Ops.Add(op);
                    _nextOperationMap[tail.Id] = op.Id;
                    _chainMap[op.Id] = chain;
                    return true;
                }
            }
        }

        return false;
    }

    bool IsLT(Dictionary<string, int> vc1, Dictionary<string, int> vc2)
    {
        bool hasLess = false;
        foreach (var key in vc1.Keys.Union(vc2.Keys))
        {
            var op1 = vc1.GetValueOrDefault(key, 0);
            var op2 = vc2.GetValueOrDefault(key, 0);
            if (op1 >= op2)
            {
                return false;
            }

            if (op1 < op2)
            {
                hasLess = true;
            }
        }

        return hasLess;
    }

    private void MovePriorityChangePointForward()
    {
        _nextPriorityChangePoint += 1;
        Debug.WriteLine("<PCTLog> Moving priority change to '{0}'.", _nextPriorityChangePoint);
    }

    private OperationWithId GetHighestPriorityEnabledOperation(IEnumerable<AsyncOperation> choices)
    {
        OperationWithId highestPriorityOp = null;
        int currentPriority = Int32.MaxValue;
        int currentChainIndex = Int32.MaxValue;
        foreach (var op in choices)
        {
            var id = _chainedOperations[op.Name];
            var chain = _chainMap[id];
            var priotiy = _chains.IndexOf(chain);
            if (priotiy < currentPriority)
            {
                highestPriorityOp = _operationMap[id];
                currentPriority = priotiy;
                currentChainIndex = chain.Ops.IndexOf(highestPriorityOp);
            }

            if (priotiy == currentPriority)
            {
                var index = chain.Ops.IndexOf(_operationMap[id]);
                if (index < currentChainIndex)
                {
                    highestPriorityOp = _operationMap[id];
                    currentChainIndex = index;
                }
            }
        }

        return highestPriorityOp;
    }

    private (int, int, Dictionary<int, (int, int)>) FindReducingSequence()
    {
        var queue = new Queue<int>();
        foreach (var chain in _chains)
        {
            if (chain.Ops.Count > 0)
            {
                queue.Enqueue(chain.Ops.First().Id);
            }
        }

        var pairs = new Dictionary<int, (int, int)>();

        while (queue.Count > 0)
        {
            var opId = queue.Dequeue();
            foreach (var chain in _chains)
            {
                if (chain == _chainMap[opId]) continue;
                if (chain.Ops.Count <= 0) continue;
                if (IsLT(_vectorClockMap[chain.Ops.Last().Id], _vectorClockMap[opId]))
                {
                    return (chain.Ops.Last().Id, opId, pairs);
                }
            }

            var temp = _predMap[opId].Where(it => _nextOperationMap.ContainsKey(it)
                                                  && !pairs.ContainsKey(_nextOperationMap[it]))
                .ToList();
            foreach (var predOp in temp)
            {
                queue.Enqueue(_nextOperationMap[predOp]);
                pairs[_nextOperationMap[predOp]] = (predOp, opId);
            }
        }

        return (-1, -1, pairs);
    }

    private void ReduceChains()
    {
        var (pred, op, pairs) = FindReducingSequence();

        if (pred == -1) return;

        do
        {
            var predChain = _chainMap[pred];
            var opChain = _chainMap[op];
            var ids = opChain.SliceSuccessors(op);
            predChain.Ops.AddRange(ids);
            opChain.Ops.RemoveAll(it => ids.Contains(it));
            foreach (var id in ids)
            {
                _chainMap[id.Id] = predChain;
            }

            _nextOperationMap[pred] = op;

            if (opChain.Ops.Count > 0)
            {
                _nextOperationMap.Remove(opChain.Ops.Last().Id);
            }

            if (!pairs.ContainsKey(op))
            {
                _chains.Remove(opChain);
                break;
            }

            pred = pairs[op].Item1;
            op = pairs[op].Item2;
        } while (true);
    }

    /// <summary>
    /// Returns the prioritized operation.
    /// </summary>
    private AsyncOperation GetPrioritizedOperation(List<AsyncOperation> ops, AsyncOperation current)
    {
        bool newOpAdded = false;
        foreach (var op in ops.Where(op => !_chainedOperations.ContainsKey(op.Name)))
        {
            OnNewOperation(op);
            newOpAdded = true;
        }

        if (newOpAdded)
        {
            ReduceChains();
        }


        var prioritizedSchedulable = GetHighestPriorityEnabledOperation(ops);
        if (_nextPriorityChangePoint == ScheduledSteps)
        {
            if (ops.Count == 1)
            {
                MovePriorityChangePointForward();
            }
            else
            {
                var chain = _chainMap[prioritizedSchedulable.Id];
                _chains.Remove(chain);
                _chains.Add(chain);

                _numSwitchPointsLeft -= 1;
                // Update the next priority change point.
                if (_numSwitchPointsLeft > 0)
                {
                    double switchPointProbability = 0.1;
                    if (ScheduleLength != 0)
                    {
                        switchPointProbability = 1.0 * _numSwitchPointsLeft / (ScheduleLength - ScheduledSteps + 1);
                    }

                    _nextPriorityChangePoint =
                        Generator.Mutator.Utils.SampleGeometric(switchPointProbability, Provider.SwitchPointChoice()) +
                        ScheduledSteps;
                }
            }
        }

        AsyncOperation scheduledOperation = null;
        if (prioritizedSchedulable != null)
        {
            scheduledOperation = ops.First(it => it.Name == prioritizedSchedulable.Sender);
            _chainedOperations.Remove(scheduledOperation.Name);
            Debug.WriteLine("<PCTCT> scheduled operation: " + scheduledOperation.Name);
        }

        return scheduledOperation;
    }

    public void Reset()
    {
        ScheduleLength = 0;
        ScheduledSteps = 0;
        _chainedOperations.Clear();
        _vectorClockMap.Clear();
        _chains.Clear();
        _nextOperationMap.Clear();
        _nextOperationId = 0;
    }

    /// <inheritdoc/>
    public virtual bool PrepareForNextIteration()
    {
        ScheduleLength = Math.Max(ScheduleLength, ScheduledSteps);
        ScheduledSteps = 0;
        _nextOperationId = 0;
        _numSwitchPointsLeft = MaxPrioritySwitchPoints;
        _chainedOperations.Clear();
        _vectorClockMap.Clear();
        _chains.Clear();
        _nextOperationMap.Clear();
        _chainMap.Clear();

        double switchPointProbability = 0.1;
        if (ScheduleLength != 0)
        {
            switchPointProbability = 1.0 * _numSwitchPointsLeft / (ScheduleLength - ScheduledSteps + 1);
        }

        _nextPriorityChangePoint =
            Generator.Mutator.Utils.SampleGeometric(switchPointProbability, Provider.SwitchPointChoice());
        return true;
    }
}