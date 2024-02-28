// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using PChecker.IO.Debugging;
using PChecker.SystematicTesting.Operations;

namespace PChecker.SystematicTesting.Strategies.Exhaustive
{
    /// <summary>
    /// A depth-first search scheduling strategy.
    /// </summary>
    internal class DFSStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        protected int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        protected int ScheduledSteps;

        /// <summary>
        /// Stack of scheduling choices.
        /// </summary>
        private readonly List<List<SChoice>> ScheduleStack;

        /// <summary>
        /// Stack of nondeterministic choices.
        /// </summary>
        private readonly List<List<NondetBooleanChoice>> BoolNondetStack;

        /// <summary>
        /// Stack of nondeterministic choices.
        /// </summary>
        private readonly List<List<NondetIntegerChoice>> IntNondetStack;

        /// <summary>
        /// Current schedule index.
        /// </summary>
        private int SchIndex;

        /// <summary>
        /// Current nondeterministic index.
        /// </summary>
        private int NondetIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="DFSStrategy"/> class.
        /// </summary>
        public DFSStrategy(int maxSteps)
        {
            MaxScheduledSteps = maxSteps;
            ScheduledSteps = 0;
            SchIndex = 0;
            NondetIndex = 0;
            ScheduleStack = new List<List<SChoice>>();
            BoolNondetStack = new List<List<NondetBooleanChoice>>();
            IntNondetStack = new List<List<NondetIntegerChoice>>();
        }

        /// <inheritdoc/>
        public bool GetNextOperation(AsyncOperation current, IEnumerable<AsyncOperation> ops, out AsyncOperation next)
        {
            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOperations.Count == 0)
            {
                next = null;
                return false;
            }

            SChoice nextChoice = null;
            List<SChoice> scs = null;

            if (SchIndex < ScheduleStack.Count)
            {
                scs = ScheduleStack[SchIndex];
            }
            else
            {
                scs = new List<SChoice>();
                foreach (var task in enabledOperations)
                {
                    scs.Add(new SChoice(task.Id));
                }

                ScheduleStack.Add(scs);
            }

            nextChoice = scs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice is null)
            {
                next = null;
                return false;
            }

            if (SchIndex > 0)
            {
                var previousChoice = ScheduleStack[SchIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = enabledOperations.Find(task => task.Id == nextChoice.Id);
            nextChoice.IsDone = true;
            SchIndex++;

            if (next is null)
            {
                return false;
            }

            ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        public bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            NondetBooleanChoice nextChoice = null;
            List<NondetBooleanChoice> ncs = null;

            if (NondetIndex < BoolNondetStack.Count)
            {
                ncs = BoolNondetStack[NondetIndex];
            }
            else
            {
                ncs = new List<NondetBooleanChoice>
                {
                    new NondetBooleanChoice(false),
                    new NondetBooleanChoice(true)
                };

                BoolNondetStack.Add(ncs);
            }

            nextChoice = ncs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice is null)
            {
                next = false;
                return false;
            }

            if (NondetIndex > 0)
            {
                var previousChoice = BoolNondetStack[NondetIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Value;
            nextChoice.IsDone = true;
            NondetIndex++;

            ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        public bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            NondetIntegerChoice nextChoice = null;
            List<NondetIntegerChoice> ncs = null;

            if (NondetIndex < IntNondetStack.Count)
            {
                ncs = IntNondetStack[NondetIndex];
            }
            else
            {
                ncs = new List<NondetIntegerChoice>();
                for (var value = 0; value < maxValue; value++)
                {
                    ncs.Add(new NondetIntegerChoice(value));
                }

                IntNondetStack.Add(ncs);
            }

            nextChoice = ncs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice is null)
            {
                next = 0;
                return false;
            }

            if (NondetIndex > 0)
            {
                var previousChoice = IntNondetStack[NondetIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Value;
            nextChoice.IsDone = true;
            NondetIndex++;

            ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        public virtual bool PrepareForNextIteration()
        {
            if (ScheduleStack.All(scs => scs.All(val => val.IsDone)))
            {
                return false;
            }

            // PrintSchedule();
            ScheduledSteps = 0;

            SchIndex = 0;
            NondetIndex = 0;

            for (var idx = BoolNondetStack.Count - 1; idx > 0; idx--)
            {
                if (!BoolNondetStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = BoolNondetStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                BoolNondetStack.RemoveAt(idx);
            }

            for (var idx = IntNondetStack.Count - 1; idx > 0; idx--)
            {
                if (!IntNondetStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = IntNondetStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                IntNondetStack.RemoveAt(idx);
            }

            if (BoolNondetStack.Count > 0 &&
                BoolNondetStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                BoolNondetStack.Clear();
            }

            if (IntNondetStack.Count > 0 &&
                IntNondetStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                IntNondetStack.Clear();
            }

            if (BoolNondetStack.Count == 0 &&
                IntNondetStack.Count == 0)
            {
                for (var idx = ScheduleStack.Count - 1; idx > 0; idx--)
                {
                    if (!ScheduleStack[idx].All(val => val.IsDone))
                    {
                        break;
                    }

                    var previousChoice = ScheduleStack[idx - 1].First(val => !val.IsDone);
                    previousChoice.IsDone = true;

                    ScheduleStack.RemoveAt(idx);
                }
            }
            else
            {
                var previousChoice = ScheduleStack.Last().LastOrDefault(val => val.IsDone);
                if (previousChoice != null)
                {
                    previousChoice.IsDone = false;
                }
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
        public string GetDescription() => "dfs";

        /// <summary>
        /// Prints the schedule.
        /// </summary>
        private void PrintSchedule()
        {
            Debug.WriteLine("*******************");
            Debug.WriteLine("Schedule stack size: " + ScheduleStack.Count);
            for (var idx = 0; idx < ScheduleStack.Count; idx++)
            {
                Debug.WriteLine("Index: " + idx);
                foreach (var sc in ScheduleStack[idx])
                {
                    Debug.Write(sc.Id + " [" + sc.IsDone + "], ");
                }

                Debug.WriteLine(string.Empty);
            }

            Debug.WriteLine("*******************");
            Debug.WriteLine("Random bool stack size: " + BoolNondetStack.Count);
            for (var idx = 0; idx < BoolNondetStack.Count; idx++)
            {
                Debug.WriteLine("Index: " + idx);
                foreach (var nc in BoolNondetStack[idx])
                {
                    Debug.Write(nc.Value + " [" + nc.IsDone + "], ");
                }

                Debug.WriteLine(string.Empty);
            }

            Debug.WriteLine("*******************");
            Debug.WriteLine("Random int stack size: " + IntNondetStack.Count);
            for (var idx = 0; idx < IntNondetStack.Count; idx++)
            {
                Debug.WriteLine("Index: " + idx);
                foreach (var nc in IntNondetStack[idx])
                {
                    Debug.Write(nc.Value + " [" + nc.IsDone + "], ");
                }

                Debug.WriteLine(string.Empty);
            }

            Debug.WriteLine("*******************");
        }

        /// <inheritdoc/>
        public void Reset()
        {
            ScheduleStack.Clear();
            BoolNondetStack.Clear();
            IntNondetStack.Clear();
            SchIndex = 0;
            NondetIndex = 0;
            ScheduledSteps = 0;
        }

        /// <summary>
        /// A scheduling choice. Contains an id and a boolean that is
        /// true if the choice has been previously explored.
        /// </summary>
        private class SChoice
        {
            internal ulong Id;
            internal bool IsDone;

            /// <summary>
            /// Initializes a new instance of the <see cref="SChoice"/> class.
            /// </summary>
            internal SChoice(ulong id)
            {
                Id = id;
                IsDone = false;
            }
        }

        /// <summary>
        /// A nondeterministic choice. Contains a boolean value that
        /// corresponds to the choice and a boolean that is true if
        /// the choice has been previously explored.
        /// </summary>
        private class NondetBooleanChoice
        {
            internal bool Value;
            internal bool IsDone;

            /// <summary>
            /// Initializes a new instance of the <see cref="NondetBooleanChoice"/> class.
            /// </summary>
            internal NondetBooleanChoice(bool value)
            {
                Value = value;
                IsDone = false;
            }
        }

        /// <summary>
        /// A nondeterministic choice. Contains an integer value that
        /// corresponds to the choice and a boolean that is true if
        /// the choice has been previously explored.
        /// </summary>
        private class NondetIntegerChoice
        {
            internal int Value;
            internal bool IsDone;

            /// <summary>
            /// Initializes a new instance of the <see cref="NondetIntegerChoice"/> class.
            /// </summary>
            internal NondetIntegerChoice(int value)
            {
                Value = value;
                IsDone = false;
            }
        }
    }
}