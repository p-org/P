//-----------------------------------------------------------------------
// <copyright file="RandomSchedulingStrategy.cs" company="Microsoft">
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
    /// Class representing a random delay scheduler.
    /// </summary>
    public sealed class RandomSchedulingStrategy : ISchedulingStrategy
    {
        private int seed;
        private Random rand;
        private int numSchedPoints = 0;

        public RandomSchedulingStrategy(int seed)
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
            var enabledThreads = threadList.Where((tid) => tid.Enabled).ToList();
            if (enabledThreads.Count == 0)
            {
                return null;
            }

            // POR
            ThreadInfo currThreadInfo = threadList[currTid];
            if (currThreadInfo.Enabled && currThreadInfo.eventType == EventType.TAKE)
            {
                return currThreadInfo;
            }

            numSchedPoints++;

            int i = rand.Next(enabledThreads.Count);
            return enabledThreads.ElementAt(i);
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
            return "Random (seed is " + seed + ")";
        }
    }
}
