using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.SystematicTesting.Operations;
using Debug = PChecker.IO.Debugging.Debug;

namespace PChecker.SystematicTesting.Strategies.Probabilistic;

internal class PCTScheduler: PrioritizedScheduler
{
    public readonly PriorizationProvider Provider;

    /// <summary>
    /// The number of scheduled steps.
    /// </summary>
    private int ScheduledSteps;

    /// <summary>
    /// Max number of priority switch points.
    /// </summary>
    public readonly int MaxPrioritySwitchPoints;

    /// <summary>
    /// Approximate length of the schedule across all iterations.
    /// </summary>
    public int ScheduleLength;

    /// <summary>
    /// List of prioritized operations.
    /// </summary>
    private readonly List<AsyncOperation> PrioritizedOperations;

    private int _nextPriorityChangePoint;
    private int _numSwitchPointsLeft;

    /// <summary>
    /// Initializes a new instance of the <see cref="PCTStrategy"/> class.
    /// </summary>
    public PCTScheduler(int maxPrioritySwitchPoints, int scheduleLength, PriorizationProvider provider)
    {
        Provider = provider;
        ScheduledSteps = 0;
        ScheduleLength = scheduleLength;
        MaxPrioritySwitchPoints = maxPrioritySwitchPoints;
        PrioritizedOperations = new List<AsyncOperation>();
        _numSwitchPointsLeft = maxPrioritySwitchPoints;

        double switchPointProbability = 0.1;
        if (ScheduleLength != 0)
        {
            switchPointProbability = 1.0 * _numSwitchPointsLeft / (ScheduleLength - ScheduledSteps + 1);
        }
        _nextPriorityChangePoint = Generator.Mutator.Utils.SampleGeometric(switchPointProbability, Provider.SwitchPointChoice());
    }

    public virtual bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
    {
        ScheduledSteps++;
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

        return true;
    }

    private void MovePriorityChangePointForward()
    {
        _nextPriorityChangePoint += 1;
        Debug.WriteLine("<PCTLog> Moving priority change to '{0}'.", _nextPriorityChangePoint);
    }
    private AsyncOperation GetHighestPriorityEnabledOperation(IEnumerable<AsyncOperation> choices)
    {
        AsyncOperation prioritizedOp = null;
        foreach (var entity in PrioritizedOperations)
        {
            if (choices.Any(m => m == entity))
            {
                prioritizedOp = entity;
                break;
            }
        }

        return prioritizedOp;
    }


    /// <summary>
    /// Returns the prioritized operation.
    /// </summary>
    private AsyncOperation GetPrioritizedOperation(List<AsyncOperation> ops, AsyncOperation current)
    {
        if (PrioritizedOperations.Count == 0)
        {
            PrioritizedOperations.Add(current);
        }

        foreach (var op in ops.Where(op => !PrioritizedOperations.Contains(op)))
        {
            var mIndex = Provider.AssignPriority(PrioritizedOperations.Count);
            PrioritizedOperations.Insert(mIndex, op);
            Debug.WriteLine("<PCTLog> Detected new operation '{0}' at index '{1}'.", op.Id, mIndex);
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
                PrioritizedOperations.Remove(prioritizedSchedulable);
                PrioritizedOperations.Add(prioritizedSchedulable);
                Debug.WriteLine("<PCTLog> Operation '{0}' changes to lowest priority.", prioritizedSchedulable);

                _numSwitchPointsLeft -= 1;
                // Update the next priority change point.
                if (_numSwitchPointsLeft > 0)
                {
                    double switchPointProbability = 0.1;
                    if (ScheduleLength != 0)
                    {
                        switchPointProbability = 1.0 * _numSwitchPointsLeft / (ScheduleLength - ScheduledSteps + 1);
                    }
                    _nextPriorityChangePoint = Generator.Mutator.Utils.SampleGeometric(switchPointProbability, Provider.SwitchPointChoice()) + ScheduledSteps;
                }

            }
        }

        if (Debug.IsEnabled)
        {
            Debug.WriteLine("<PCTLog> Prioritized schedulable '{0}'.", prioritizedSchedulable);
            Debug.Write("<PCTLog> Priority list: ");
            for (var idx = 0; idx < PrioritizedOperations.Count; idx++)
            {
                if (idx < PrioritizedOperations.Count - 1)
                {
                    Debug.Write("'{0}', ", PrioritizedOperations[idx]);
                }
                else
                {
                    Debug.WriteLine("'{0}'.", PrioritizedOperations[idx]);
                }
            }
        }

        return ops.First(op => op.Equals(prioritizedSchedulable));
    }

    public void Reset()
    {
        ScheduleLength = 0;
        ScheduledSteps = 0;
        PrioritizedOperations.Clear();
    }

    /// <inheritdoc/>
    public virtual bool PrepareForNextIteration()
    {
        ScheduleLength = Math.Max(ScheduleLength, ScheduledSteps);
        ScheduledSteps = 0;
        _numSwitchPointsLeft = MaxPrioritySwitchPoints;

        PrioritizedOperations.Clear();
        double switchPointProbability = 0.1;
        if (ScheduleLength != 0)
        {
            switchPointProbability = 1.0 * _numSwitchPointsLeft / (ScheduleLength - ScheduledSteps + 1);
        }
        _nextPriorityChangePoint = Generator.Mutator.Utils.SampleGeometric(switchPointProbability, Provider.SwitchPointChoice());
        return true;
    }
}
