// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.IO.Debugging;
using PChecker.Random;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Probabilistic
{
    /// <summary>
    /// A priority-based probabilistic scheduling strategy.
    /// </summary>
    /// <remarks>
    /// This strategy is described in the following paper:
    /// https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/asplos277-pct.pdf
    /// </remarks>
    internal sealed class PCTStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        private readonly IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        private readonly int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        private int ScheduledSteps;

        /// <summary>
        /// Max number of priority switch points.
        /// </summary>
        private readonly int MaxPrioritySwitchPoints;

        /// <summary>
        /// Approximate length of the schedule across all schedules.
        /// </summary>
        private int ScheduleLength;

        /// <summary>
        /// List of prioritized operations.
        /// </summary>
        private readonly List<AsyncOperation> PrioritizedOperations;

        /// <summary>
        /// Set of priority change points.
        /// </summary>
        private readonly SortedSet<int> PriorityChangePoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="PCTStrategy"/> class.
        /// </summary>
        public PCTStrategy(int maxSteps, int maxPrioritySwitchPoints, IRandomValueGenerator random)
        {
            RandomValueGenerator = random;
            MaxScheduledSteps = maxSteps;
            ScheduledSteps = 0;
            ScheduleLength = 0;
            MaxPrioritySwitchPoints = maxPrioritySwitchPoints;
            PrioritizedOperations = new List<AsyncOperation>();
            PriorityChangePoints = new SortedSet<int>();
        }

        /// <inheritdoc/>
        public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
        {
            next = null;
            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOperations.Count == 0)
            {
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

        /// <inheritdoc/>
        public bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            next = false;
            if (RandomValueGenerator.Next(maxValue) == 0)
            {
                next = true;
            }

            ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        public bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            next = RandomValueGenerator.Next(maxValue);
            ScheduledSteps++;
            return true;
        }

        /// <inheritdoc/>
        public bool PrepareForNextIteration()
        {
            ScheduleLength = Math.Max(ScheduleLength, ScheduledSteps);
            ScheduledSteps = 0;

            PrioritizedOperations.Clear();
            PriorityChangePoints.Clear();

            var range = new List<int>();
            for (var idx = 0; idx < ScheduleLength; idx++)
            {
                range.Add(idx);
            }

            foreach (var point in Shuffle(range).Take(MaxPrioritySwitchPoints))
            {
                PriorityChangePoints.Add(point);
            }

            return true;
        }

        /// <inheritdoc/>
        public int GetScheduledSteps() => ScheduledSteps;

        /// <inheritdoc/>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (MaxScheduledSteps == 0)
            {
                return false;
            }

            return ScheduledSteps >= MaxScheduledSteps;
        }

        /// <inheritdoc/>
        public bool IsFair() => false;

        /// <inheritdoc/>
        public string GetDescription()
        {
            var text = $"pct[priority change points '{MaxPrioritySwitchPoints}' [" +
                       string.Join(", ", PriorityChangePoints.ToArray()) +
                       "], seed '" + RandomValueGenerator.Seed + "']";
            return text;
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
                var mIndex = RandomValueGenerator.Next(PrioritizedOperations.Count) + 1;
                PrioritizedOperations.Insert(mIndex, op);
                Debug.WriteLine("<PCTLog> Detected new operation '{0}' at index '{1}'.", op.Id, mIndex);
            }

            if (PriorityChangePoints.Contains(ScheduledSteps))
            {
                if (ops.Count == 1)
                {
                    MovePriorityChangePointForward();
                }
                else
                {
                    var priority = GetHighestPriorityEnabledOperation(ops);
                    PrioritizedOperations.Remove(priority);
                    PrioritizedOperations.Add(priority);
                    Debug.WriteLine("<PCTLog> Operation '{0}' changes to lowest priority.", priority);
                }
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

        /// <summary>
        /// Returns the highest-priority enabled operation.
        /// </summary>
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
        /// Shuffles the specified list using the Fisher-Yates algorithm.
        /// </summary>
        private IList<int> Shuffle(IList<int> list)
        {
            var result = new List<int>(list);
            for (var idx = result.Count - 1; idx >= 1; idx--)
            {
                var point = RandomValueGenerator.Next(ScheduleLength);
                var temp = result[idx];
                result[idx] = result[point];
                result[point] = temp;
            }

            return result;
        }

        /// <summary>
        /// Moves the current priority change point forward. This is a useful
        /// optimization when a priority change point is assigned in either a
        /// sequential execution or a nondeterministic choice.
        /// </summary>
        private void MovePriorityChangePointForward()
        {
            PriorityChangePoints.Remove(ScheduledSteps);
            var newPriorityChangePoint = ScheduledSteps + 1;
            while (PriorityChangePoints.Contains(newPriorityChangePoint))
            {
                newPriorityChangePoint++;
            }

            PriorityChangePoints.Add(newPriorityChangePoint);
            Debug.WriteLine("<PCTLog> Moving priority change to '{0}'.", newPriorityChangePoint);
        }

        /// <inheritdoc/>
        public void Reset()
        {
            ScheduleLength = 0;
            ScheduledSteps = 0;
            PrioritizedOperations.Clear();
            PriorityChangePoints.Clear();
        }
    }
}