//-----------------------------------------------------------------------
// <copyright file="ScheduleStep.cs" company="Microsoft">
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
    /// Class representing a single path step.
    /// </summary>
    internal sealed class ScheduleStep
    {
        /// <summary>
        /// Sender machine.
        /// </summary>
        internal readonly string Sender;

        /// <summary>
        /// Receiver machine.
        /// </summary>
        internal readonly string Receiver;

        /// <summary>
        /// Sent event.
        /// </summary>
        internal readonly string Event;

        /// <summary>
        /// Constructor of the PathStep class.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="receiver">Receiver machine</param>
        /// <param name="e">Sent event</param>
        public ScheduleStep(string sender, string receiver, string e)
        {
            this.Sender = sender;
            this.Receiver = receiver;
            this.Event = e;
        }
    }
}
