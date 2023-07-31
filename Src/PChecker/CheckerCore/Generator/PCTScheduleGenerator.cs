using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Generator.Mutator;
using PChecker.Generator.Object;
using PChecker.IO.Debugging;
using PChecker.SystematicTesting.Operations;

namespace PChecker.Generator;

internal sealed class PctScheduleGenerator: IScheduleGenerator<PctScheduleGenerator>
{
    public System.Random Random;
    public RandomChoices<int> PriorityChoices;
    public RandomChoices<double> SwitchPointChoices;
    private readonly List<AsyncOperation> _prioritizedOperations = new();
    private int _nextPriorityChangePoint;
    private int _scheduledSteps = 0;
    public int MaxScheduleLength;
    private int _numSwitchPointsLeft;
    public int NumSwitchPoints;

    public PctScheduleGenerator(System.Random random, RandomChoices<int>? priorityChoices, RandomChoices<double>? switchPointChoices, int numSwitchPoints, int maxScheduleLength)
    {
        Random = random;
        PriorityChoices = priorityChoices != null ? new RandomChoices<int>(priorityChoices) : new RandomChoices<int>(random);
        SwitchPointChoices = switchPointChoices != null ? new RandomChoices<double>(switchPointChoices) :
            new RandomChoices<double>(random);

        NumSwitchPoints = numSwitchPoints;
        MaxScheduleLength = maxScheduleLength;

        _numSwitchPointsLeft = numSwitchPoints;
        double switchPointProbability = 0.1;
        if (MaxScheduleLength != 0)
        {
            switchPointProbability = 1.0 * _numSwitchPointsLeft / (MaxScheduleLength - _scheduledSteps + 1);
        }

        _nextPriorityChangePoint = Utils.SampleGeometric(switchPointProbability, SwitchPointChoices.Next());

    }

    public PctScheduleGenerator(CheckerConfiguration checkerConfiguration):
        this(new System.Random((int?)checkerConfiguration.RandomGeneratorSeed ?? Guid.NewGuid().GetHashCode()), null, null, checkerConfiguration.StrategyBound,  0)
    {
    }

    public PctScheduleGenerator Mutate()
    {
        return new PCTScheduleMutator().Mutate(this);
    }

    public PctScheduleGenerator Copy()
    {
        return new PctScheduleGenerator(Random, PriorityChoices, SwitchPointChoices, NumSwitchPoints, MaxScheduleLength);
    }

    public AsyncOperation? NextRandomOperation(List<AsyncOperation> enabledOperations, AsyncOperation current)
    {
        _scheduledSteps += 1;
        if (enabledOperations.Count == 0)
        {
            if (_nextPriorityChangePoint == _scheduledSteps)
            {
                MovePriorityChangePointForward();
            }
            return null;
        }

        return GetPrioritizedOperation(enabledOperations, current);
    }


    private AsyncOperation GetPrioritizedOperation(List<AsyncOperation> ops, AsyncOperation current)
    {
        if (_prioritizedOperations.Count == 0)
        {
            _prioritizedOperations.Add(current);
        }

        foreach (var op in ops.Where(op => !_prioritizedOperations.Contains(op)))
        {
            var mIndex = PriorityChoices.Next() % _prioritizedOperations.Count + 1;
            _prioritizedOperations.Insert(mIndex, op);
            Debug.WriteLine("<PCTLog> Detected new operation '{0}' at index '{1}'.", op.Id, mIndex);
        }

        if (_nextPriorityChangePoint == _scheduledSteps)
        {
            if (ops.Count == 1)
            {
                MovePriorityChangePointForward();
            }
            else
            {
                var priority = GetHighestPriorityEnabledOperation(ops);
                _prioritizedOperations.Remove(priority);
                _prioritizedOperations.Add(priority);
                Debug.WriteLine("<PCTLog> Operation '{0}' changes to lowest priority.", priority);

                _numSwitchPointsLeft -= 1;
                // Update the next priority change point.
                if (_numSwitchPointsLeft > 0)
                {
                    double switchPointProbability = 0.1;
                    if (MaxScheduleLength != 0)
                    {
                        switchPointProbability = 1.0 * _numSwitchPointsLeft / (MaxScheduleLength - _scheduledSteps + 1);
                    }
                    _nextPriorityChangePoint = Utils.SampleGeometric(switchPointProbability, SwitchPointChoices.Next()) + _scheduledSteps;
                }
            }
        }

        var prioritizedSchedulable = GetHighestPriorityEnabledOperation(ops);
        if (Debug.IsEnabled)
        {
            Debug.WriteLine("<PCTLog> Prioritized schedulable '{0}'.", prioritizedSchedulable);
            Debug.Write("<PCTLog> Priority list: ");
            for (var idx = 0; idx < _prioritizedOperations.Count; idx++)
            {
                if (idx < _prioritizedOperations.Count - 1)
                {
                    Debug.Write("'{0}', ", _prioritizedOperations[idx]);
                }
                else
                {
                    Debug.WriteLine("'{0}'.", _prioritizedOperations[idx]);
                }
            }
        }

        return prioritizedSchedulable;
    }

    private AsyncOperation GetHighestPriorityEnabledOperation(IEnumerable<AsyncOperation> choices)
    {
        AsyncOperation prioritizedOp = null;
        foreach (var entity in _prioritizedOperations)
        {
            if (choices.Any(m => m == entity))
            {
                prioritizedOp = entity;
                break;
            }
        }

        return prioritizedOp;
    }

    private void MovePriorityChangePointForward()
    {
        _nextPriorityChangePoint += 1;
        Debug.WriteLine("<PCTLog> Moving priority change to '{0}'.", _nextPriorityChangePoint);
    }

    public void PrepareForNextInput()
    {
        MaxScheduleLength = Math.Max(_scheduledSteps, MaxScheduleLength);
        _prioritizedOperations.Clear();
    }
}