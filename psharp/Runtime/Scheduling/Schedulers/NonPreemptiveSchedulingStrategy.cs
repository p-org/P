//-----------------------------------------------------------------------
// <copyright file="NonPreemptiveSchedulingStrategy.cs" company="Microsoft">
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
using System.Linq;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class representing a non-preemptive scheduling strategy.
    /// </summary>
    public sealed class NonPreemptiveSchedulingStrategy : ISchedulingStrategy
    {
        private int seed;
        private Random rand;
        private int numSchedPoints = 0;

        public NonPreemptiveSchedulingStrategy(int seed)
        {
            this.seed = seed;
            rand = new Random(seed);
        }

        public int GetNumSchedPoints()
        {
            return numSchedPoints;
        }

        public ThreadInfo ReachedSchedulingPoint(int currTid, List<ThreadInfo> threadList)
        {
            var orderedList = threadList
                .ShiftLeft(currTid)
                .Where((ti) => ti.Enabled)
                .ToList();

            if (orderedList.Count == 0)
            {
                return null;
            }

            ThreadInfo currThreadInfo = threadList[currTid];
            if (!(currThreadInfo.Enabled && currThreadInfo.eventType == EventType.TAKE))
            {
                numSchedPoints++;
            }

            return orderedList[0];
        }

        public bool GetRandomBool()
        {
            return rand.Next(2) != 0;
        }

        public int GetRandomInt(int ceiling)
        {
            return rand.Next(ceiling);
        }

        public bool Reset()
        {
            numSchedPoints = 0;
            return true;
        }

        public string GetDescription()
        {
            return "Non-preemptive (seed is " + seed + ")";
        }
    }
}
