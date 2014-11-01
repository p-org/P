//-----------------------------------------------------------------------
// <copyright file="Event.cs" company="Microsoft">
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

using Microsoft.PSharp.Scheduling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing an event.
    /// </summary>
    public abstract class Event
    {
        /// <summary>
        /// Payload of the event.
        /// </summary>
        protected internal readonly Object Payload;

        /// <summary>
        /// Default constructor of the Event class.
        /// </summary>
        protected Event()
        {
            this.Payload = null;
        }

        /// <summary>
        /// Constructor of the Event class.
        /// </summary>
        /// <param name="payload">Payload of the event</param>
        protected Event(Object payload)
        {
            this.Payload = payload;
        }
    }
}
