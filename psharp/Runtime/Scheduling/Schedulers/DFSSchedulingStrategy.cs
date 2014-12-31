//-----------------------------------------------------------------------
// <copyright file="DFSSchedulingStrategy.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class representing a depth-first search scheduling strategy.
    /// </summary>
    public sealed class DFSSchedulingStrategy : ISchedulingStrategy
    {
        private List<List<TidEntry>> stack;
        private int index;
        private int boundCount;
        private int maxBoundCount = -1;
        private bool delayBounding = false;
        private Random deterministicRandom;
        private int deterministicRandSeed;
        private int numSchedPoints;

        public DFSSchedulingStrategy(int deterministicRandSeed)
        {
            stack = new List<List<TidEntry>>();
            stack.Add(new List<TidEntry>() { new TidEntry(0, EventType.THREAD_START) });
            index = 0;
            boundCount = 0;
            this.deterministicRandSeed = deterministicRandSeed;
            deterministicRandom = new Random(deterministicRandSeed);
            numSchedPoints = 0;
        }

        public int GetNumSchedPoints()
        {
            return numSchedPoints;
        }

        public DFSSchedulingStrategy DoDelayBounding()
        {
            delayBounding = true;
            return this;
        }

        public DFSSchedulingStrategy SetBound(int bound)
        {
            maxBoundCount = bound;
            return this;
        }

        public string GetDescription()
        {
            return "DFS" + (maxBoundCount >= 0 ? (delayBounding ? " DB of " : " PB of ") +
                maxBoundCount : "") + " seed=" + deterministicRandSeed;
        }

        public int MaxStackIndex()
        {
            return stack.Count - 1;
        }

        private void PrunePOR(int currTid, List<TidEntry> orderedList)
        {
            if (orderedList.Count > 0 && orderedList[0].eventType == EventType.TAKE)
            {
                for (int i = 1; i < orderedList.Count; ++i)
                {
                    orderedList[i].done = true;
                }
            }
        }

        private void PruneBounded(int currTid, List<TidEntry> orderedList)
        {
            if (maxBoundCount >= 0)
            {
                Debug.Assert(boundCount <= maxBoundCount);
                if (delayBounding)
                {
                    // Delay bounding
                    for (int i = 1; i < orderedList.Count; ++i)
                    {
                        if (boundCount + i > maxBoundCount)
                        {
                            orderedList[i].done = true;
                        }
                    }
                }
                else
                {
                    // Preemption bounding
                    if (orderedList.Count > 0 && currTid == orderedList[0].tid)
                    {
                        for (int i = 1; i < orderedList.Count; ++i)
                        {
                            if (boundCount + 1 > maxBoundCount)
                            {
                                orderedList[i].done = true;
                            }
                        }
                    }
                }
            }
        }

        public bool GetRandomBool()
        {
            return deterministicRandom.Next(2) != 0;
        }

        public int GetRandomInt(int ceiling)
        {
            return deterministicRandom.Next(ceiling);
        }

        private void UpdateBoundCount(int currTid, int nextTid, List<TidEntry> orderedList, int nextThreadIndexInOrderedList)
        {
            if (delayBounding)
            {
                boundCount += nextThreadIndexInOrderedList;
            }
            else
            {
                // preemption bounding
                if (nextTid != currTid && currTid == orderedList[0].tid)
                {
                    boundCount++;
                }
            }
        }

        public ThreadInfo ReachedSchedulingPoint(int currTid, List<ThreadInfo> threadList)
        {
            Debug.Assert(index <= MaxStackIndex());

            ThreadInfo currThreadInfo = threadList[currTid];
            if (!(currThreadInfo.Enabled && currThreadInfo.eventType == EventType.TAKE))
            {
                numSchedPoints++;
            }

            int nextThreadIndexInOrderedList = -1;

            // create list of TidEntry
            var orderedList = threadList
                .ShiftLeft(currTid)
                .Where((ti) => ti.Enabled)
                .Select((ti) => new TidEntry(ti.Id, ti.eventType))
                .ToList();

            PruneBounded(currTid, orderedList);
            PrunePOR(currTid, orderedList);

            if (index == MaxStackIndex())
            {
                var tidEntry = stack.Last().First((entry) => !entry.done);
                Debug.Assert(currTid == tidEntry.tid && !tidEntry.done);

                // push onto stack
                stack.Add(orderedList);
                index++;

                // pick next thread (the first thread)
                if (orderedList.Count > 0)
                {
                    Debug.Assert(!orderedList.First().done);
                    nextThreadIndexInOrderedList = 0;
                }
            }
            else
            {
                Debug.Assert(index < MaxStackIndex());
                var topOfStack = stack.ElementAt(index);
                var currEntry = topOfStack.First((entry) => !entry.done);

                // we executed the first entry that was not done.
                Debug.Assert(currEntry.tid == currTid);

                // check that enabled threads match
                index++;
                topOfStack = stack.ElementAt(index);

                if (topOfStack.Count != orderedList.Count)
                {
                    throw new NondeterminismException();
                }

                for (int i = 0; i < topOfStack.Count; ++i)
                {
                    var a = topOfStack.ElementAt(i);
                    var b = orderedList.ElementAt(i);
                    if (a.tid != b.tid || a.eventType != b.eventType)
                    {
                        throw new NondeterminismException();
                    }
                }

                // next thread is the first thread that is not done
                nextThreadIndexInOrderedList = topOfStack.FindIndex((entry) => !entry.done);
            }

            if (nextThreadIndexInOrderedList == -1)
            {
                return null;
            }

            int nextTid = stack[index][nextThreadIndexInOrderedList].tid;

            UpdateBoundCount(currTid, nextTid, orderedList, nextThreadIndexInOrderedList);

            return threadList[nextTid];
        }

        public bool Reset()
        {
            // setup stack for next execution
            index = 0;
            numSchedPoints = 0;
            boundCount = 0;
            deterministicRandom = new Random(deterministicRandSeed);

            // pop stack while last entry contains 0 "not-done"s
            while (stack.Last().Count((entry) => !entry.done) == 0)
            {
                stack.RemoveAt(stack.Count - 1);
                if (stack.Count == 0)
                {
                    // nothing left to pop.
                    return false;
                }

                var firstNotDone = stack.Last().FirstOrDefault((entry) => !entry.done);
                if (firstNotDone != null)
                {
                    firstNotDone.done = true;
                }
            }

            Debug.Assert(stack.Last().Count((entry) => !entry.done) > 0);

            return true;
        }
    }

    public static class ShiftList
    {
        public static List<T> ShiftLeft<T>(this List<T> list, int shiftBy)
        {
            if (shiftBy == 0 || shiftBy == list.Count)
            {
                return new List<T>(list);
            }

            if (list.Count <= shiftBy)
            {
                throw new IndexOutOfRangeException();
            }

            var result = list.GetRange(shiftBy, list.Count - shiftBy);
            result.AddRange(list.GetRange(0, shiftBy));
            return result;
        }
    }

    class TidEntry
    {
        public int tid;
        public EventType eventType;
        public bool done;

        public TidEntry(int tid, EventType eventType)
        {
            this.tid = tid;
            this.eventType = eventType;
            this.done = false;
        }
    }
}
