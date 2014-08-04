//-----------------------------------------------------------------------
// <copyright file="MachineWrapper.cs" company="Microsoft">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a wrapper for
    /// a state machine.
    /// </summary>
    public abstract class MachineWrapper
    {
        private Machine Machine;
        protected Object Payload;
        private Object Lock;

        /// <summary>
        /// Default constructor of the MachineWrapper class.
        /// </summary>
        public MachineWrapper()
        {
            this.Machine = null;
            this.Payload = null;
            this.Lock = new Object();
        }

        /// <summary>
        /// Creates and wraps the machine of type T with
        /// an optional payload. The wrapper must not
        /// already contain a wrapped machine.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="payload"></param>
        public void CreateAndWrapMachineOfType<T>(Object payload = null)
        {
            Runtime.Assert(this.Machine == null, "A wrapped machine already exists.\n");
            this.Machine = Microsoft.PSharp.Machine.Factory.
                CreateMachine<T>(payload) as Machine;
            this.Machine.Wrapper = this;
        }

        /// <summary>
        /// Wraps the given existing machine. The wrapper must not
        /// already contain a wrapped machine.
        /// </summary>
        /// <param name="m">Machine</param>
        public void WrapMachine(Machine m)
        {
            Runtime.Assert(this.Machine == null, "A wrapped machine already exists.\n");
            Runtime.Assert(m != null, "The given machine cannot be null.\n");
            this.Machine = m;
            this.Machine.Wrapper = this;
        }

        /// <summary>
        /// Returns the wrapped machine.
        /// </summary>
        /// <returns>Machine</returns>
        public Machine GetMachine()
        {
            return this.Machine;
        }

        /// <summary>
        /// Sends an asynchronous event to the wrapped machine.
        /// </summary>
        /// <param name="sender">Sender machine/environment</param>
        /// <param name="e">Event</param>
        protected void Send(string sender, Event e)
        {
            Runtime.Send(sender, this.Machine, e);
        }

        /// <summary>
        /// Waits for the given action to be processed.
        /// </summary>
        protected void Wait(Action action)
        {
            Runtime.Assert(this.Machine != null, "The wrapped machine " +
                "has not be initialized yet.\n");

            lock (this.Lock)
            {
                action.Invoke();
                System.Threading.Monitor.Wait(this.Lock);
            }
        }

        /// <summary>
        /// Signals that the latest event was processed and
        /// return an optional payload.
        /// </summary>
        /// <param name="obj">Optional payload</param>
        public void Signal(Object payload = null)
        {
            lock (this.Lock)
            {
                this.Payload = payload;
                System.Threading.Monitor.Pulse(this.Lock);
            }
        }
    }
}
