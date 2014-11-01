//-----------------------------------------------------------------------
// <copyright file="RoundRobinScheduler.cs" company="Microsoft">
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
    /// Class representing a random delay scheduler.
    /// </summary>
    public sealed class RoundRobinScheduler : IScheduler
    {
        /// <summary>
        /// Index of latest operation.
        /// </summary>
        private int Index;

        /// <summary>
        /// Default constructor of the RoundRobinScheduler class.
        /// </summary>
        public RoundRobinScheduler()
        {
            this.Index = 0;
        }

        /// <summary>
        /// Returns the next machine ID to be scheduled.
        /// </summary>
        /// <param name="machineIDs">List<int></param>
        /// <returns>machineID</returns>
        int IScheduler.Next(List<int> machineIDs)
        {
            this.Index++;
            if (this.Index >= machineIDs.Count)
                this.Index = 0;
            return machineIDs[this.Index];
        }
    }
}
