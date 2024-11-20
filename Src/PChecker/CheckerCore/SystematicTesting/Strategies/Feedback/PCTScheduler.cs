// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using PChecker.Generator.Object;
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
    internal class PCTScheduler : IScheduler
    {
        
        
        /// <summary>
        /// Random value generator.
        /// </summary>
        private readonly IRandomValueGenerator _randomValueGenerator;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        private int ScheduledSteps;

        /// <summary>
        /// Max number of priority switch points.
        /// </summary>
        internal readonly int MaxPrioritySwitchPoints;

        /// <summary>
        /// Approximate length of the schedule across all schedules.
        /// </summary>
        internal int ScheduleLength;

        /// <summary>
        /// List of prioritized operations.
        /// </summary>
        private readonly List<AsyncOperation> PrioritizedOperations = new();
        
        /// <summary>
        /// Set of priority change points.
        /// </summary>
        private readonly SortedSet<int> PriorityChangePoints = new();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PCTStrategy"/> class.
        /// </summary>
        public PCTScheduler(int maxPrioritySwitchPoints, int scheduleLength, IRandomValueGenerator random)
        {
            _randomValueGenerator = random;
            ScheduledSteps = 0;
            ScheduleLength = scheduleLength;
            MaxPrioritySwitchPoints = maxPrioritySwitchPoints;
            if (ScheduleLength != 0)
            {
                PreparePriorityChangePoints(ScheduleLength);
            }
            
        }

        public virtual bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops,
            out AsyncOperation next)
        {
            ScheduledSteps++;
            next = null;
            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOperations.Count == 0)
            {
                if (PriorityChangePoints.Contains(ScheduledSteps))
                {
                    MovePriorityChangePointForward();
                }

                return false;
            }

            var highestEnabledOp = GetPrioritizedOperation(enabledOperations, current);
            next = highestEnabledOp;

            return true;
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
                var mIndex = _randomValueGenerator.Next(PrioritizedOperations.Count) + 1;
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

            PrioritizedOperations.Clear();
            PriorityChangePoints.Clear();
            return true;
        }

        private void PreparePriorityChangePoints(int step)
        {
            List<int> listOfInts = new List<int>();
            for (int i = 0; i < step; i++)
            {
                listOfInts.Add(i);
            }

            for (int i = 0; i < MaxPrioritySwitchPoints; i++)
            {
                int index = _randomValueGenerator.Next(listOfInts.Count);
                PriorityChangePoints.Add(listOfInts[index]);
                listOfInts.RemoveAt(index);
            }
        }

        public IScheduler Mutate()
        {
            return new PCTScheduler(MaxPrioritySwitchPoints, ScheduleLength, ((ControlledRandom) _randomValueGenerator).Mutate());
        }

        public IScheduler New()
        {
            return new PCTScheduler(MaxPrioritySwitchPoints, ScheduleLength, ((ControlledRandom) _randomValueGenerator).New());
        }
    }
}