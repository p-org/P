//-----------------------------------------------------------------------
// <copyright file="Operation.cs" company="Microsoft">
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

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class representing a scheduling operation.
    /// </summary>
    public sealed class Operation
    {
        /// <summary>
        /// Monotonically increasing operation counter.
        /// </summary>
        private static int Counter = 1;

        /// <summary>
        /// Unique ID of the operation. If the ID is 0 then
        /// the operation is sealed and its processing order
        /// cannot be affected by the scheduler.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// Constructor of the Operation class. If the
        /// operation is sealed then it will have the
        /// fixed ID 0. Else it will have a monotonically
        /// increasing ID >= 1.
        /// </summary>
        /// <param name="isSealed">bool</param>
        public Operation(bool isSealed = false)
        {
            if (isSealed)
            {
                this.Id = 0;
            }
            else
            {
                this.Id = Operation.Counter++;
            }
        }
    }
}
