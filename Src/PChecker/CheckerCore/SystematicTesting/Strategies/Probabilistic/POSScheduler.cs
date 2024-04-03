using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Feedback;
using PChecker.IO.Debugging;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Probabilistic;

internal class POSScheduler: PrioritizedScheduler
{
    internal readonly PriorizationProvider Provider;

    /// <summary>
    /// List of prioritized operations.
    /// </summary>
    private readonly List<AsyncOperation> PrioritizedOperations;

    public ConflictOpMonitor? ConflictOpMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="PCTStrategy"/> class.
    /// </summary>
    public POSScheduler(PriorizationProvider provider, ConflictOpMonitor? monitor)
    {
        Provider = provider;
        PrioritizedOperations = new List<AsyncOperation>();
        ConflictOpMonitor = monitor;
    }

    public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
    {
        next = null;
        var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
        if (enabledOperations.Count == 0)
        {
            return false;
        }

        var highestEnabledOp = GetPrioritizedOperation(enabledOperations, current);
        next = highestEnabledOp;
        if (next.Type == AsyncOperationType.Send)
        {
            ResetPriorities(next, enabledOperations);
        }
        return true;
    }

    void ResetPriorities(AsyncOperation next, IEnumerable<AsyncOperation> ops)
    {
        foreach (var op in ops)
        {
            if (op.Type == AsyncOperationType.Send)
            {
                if (op.LastSentReceiver == next.LastSentReceiver)
                {
                    PrioritizedOperations.Remove(op);
                }
            }
        }
        PrioritizedOperations.Remove(next);
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

        if (FindNonRacingOperation(ops, out var next))
        {
            return next;
        }

        var prioritizedSchedulable = GetHighestPriorityEnabledOperation(ops);

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

    private bool FindNonRacingOperation(IEnumerable<AsyncOperation> ops, out AsyncOperation next)
    {
        var nonRacingOps = ops.Where(op => op.Type != AsyncOperationType.Send);
        if (!nonRacingOps.Any() && ConflictOpMonitor != null)
        {
            nonRacingOps = ops.Where(op => !ConflictOpMonitor.IsConflictingOp(op));
        }

        if (!nonRacingOps.Any())
        {
            next = null;
            return false;
        }
        if (!nonRacingOps.Skip(1).Any())
        {
            next = nonRacingOps.First();
            return true;
        }
        next = GetHighestPriorityEnabledOperation(nonRacingOps);
        return true;
    }

    public void Reset()
    {
        PrioritizedOperations.Clear();
    }

    /// <inheritdoc/>
    public virtual bool PrepareForNextIteration()
    {
        PrioritizedOperations.Clear();
        return true;
    }
}