//-----------------------------------------------------------------------
// <copyright file="ThreadInfo.cs" company="Microsoft">
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

using Microsoft.PSharp.Scheduling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class implementing thread related information.
    /// </summary>
    public sealed class ThreadInfo
    {
        internal int Id;

        internal bool Enabled = true;
        internal bool Active = false;
        internal bool Started = false;
        internal bool Terminated = false;

        internal EventType eventType = EventType.THREAD_START;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tid">Thread ID</param>
        internal ThreadInfo(int tid)
        {
            this.Id = tid;
        }
    }
}
