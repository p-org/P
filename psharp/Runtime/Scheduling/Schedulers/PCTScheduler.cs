//-----------------------------------------------------------------------
// <copyright file="PCTScheduler.cs" company="Microsoft">
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
using System.Text;
using System.Threading.Tasks;

using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class representing a PCT scheduler.
    /// </summary>
    public sealed class PCTScheduler : IScheduler
    {
        /// <summary>
        /// Bound on the number of possible operations.
        /// </summary>
        private int OperationsBound;

        /// <summary>
        /// A map from operation IDs to priorities.
        /// </summary>
        private Dictionary<int, int> PriorityMap;

        /// <summary>
        /// A monotonically increasing priority counter.
        /// </summary>
        private int PriorityCounter;

        /// <summary>
        /// Constructor of the RoundRobinScheduler class.
        /// Accepts a bound that is equal to the total
        /// number of operations to be scheduled.
        /// </summary>
        /// <param name="numOfOperations">int</param>
        public PCTScheduler(int bound)
        {
            this.OperationsBound = bound;
            this.PriorityMap = new Dictionary<int, int>();
            this.PriorityCounter = (this.OperationsBound * 2) - 1;
        }

        /// <summary>
        /// Returns the next machine ID to be scheduled.
        /// </summary>
        /// <param name="nextId">Next machine ID</param>
        /// <param name="machineIDs">Machine IDs</param>
        /// <returns>Boolean value</returns>
        bool IScheduler.TryGetNext(out int nextId, List<int> machineIDs)
        {
            var maxPriorityKey = this.PriorityMap.Aggregate(
                    (l, r) => l.Value > r.Value ? l : r).Key;

            if (this.OperationsBound > 0 && Model.Havoc.Boolean)
            {
                this.PriorityMap[maxPriorityKey] = this.PriorityMap[maxPriorityKey]
                    - this.OperationsBound;
                maxPriorityKey = this.PriorityMap.Aggregate(
                    (l, r) => l.Value > r.Value ? l : r).Key;
            }

            nextId = maxPriorityKey;
            return true;
        }

        /// <summary>
        /// Returns true if the scheduler has finished.
        /// </summary>
        /// <returns>Boolean value</returns>
        bool IScheduler.HasFinished()
        {
            return false;
        }

        /// <summary>
        /// Resets the scheduler.
        /// </summary>
        void IScheduler.Reset()
        {

        }
    }
}
