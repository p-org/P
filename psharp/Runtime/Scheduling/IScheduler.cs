//-----------------------------------------------------------------------
// <copyright file="IScheduler.cs" company="Microsoft">
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
    /// Interface of a generic state machine scheduler.
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// This is called by the P# runtime scheduler to find
        /// the next machine ID to schedule. It accepts the list
        /// of all enabled machine ID in the program.
        /// </summary>
        /// <param name="machineIDs">List<int></param>
        /// <returns>machineID</returns>
        int Next(List<int> machineIDs);
    }
}
