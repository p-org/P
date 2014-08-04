//-----------------------------------------------------------------------
// <copyright file="RandomScheduler.cs" company="Microsoft">
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
    public sealed class RandomScheduler : IScheduler
    {
        /// <summary>
        /// Randomly generates values.
        /// </summary>
        internal Random Randomizer;

        /// <summary>
        /// Default constructor of the RandomScheduler class.
        /// </summary>
        public RandomScheduler()
        {
            this.Randomizer = new Random(DateTime.Now.Second);
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <param name="operation">Operation</param>
        void IScheduler.Register(Operation operation)
        {

        }

        /// <summary>
        /// Returns the next operation to be scheduled.
        /// </summary>
        /// <param name="operations">List<Operation></param>
        /// <returns>Operation</returns>
        Operation IScheduler.Next(List<Operation> operations)
        {
            int index = this.Randomizer.Next(0, operations.Count);
            Operation op = operations[index];
            return op;
        }
    }
}
