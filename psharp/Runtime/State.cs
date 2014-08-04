//-----------------------------------------------------------------------
// <copyright file="State.cs" company="Microsoft">
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
using Microsoft.PSharp.Scheduling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state of a state machine.
    /// </summary>
    public abstract class State
    {
        #region fields

        /// <summary>
        /// Handle to the machine that owns this state instance.
        /// </summary>
        protected internal Machine Machine;

        /// <summary>
        /// Handle to the latest received event type.
        /// If there was no event received yet the returned
        /// value is null.
        /// </summary>
        protected Type Message
        {
            get
            {
                return this.Machine.Message;
            }
        }

        /// <summary>
        /// Handle to the payload of the last received event.
        /// If the last received event does not have a payload,
        /// a null value is returned.
        /// </summary>
        protected Object Payload
        {
            get
            {
                return this.Machine.Payload;
            }
        }

        /// <summary>
        /// Dictionary containing all step state transitions.
        /// </summary>
        private StateTransitions StepTransitions;

        /// <summary>
        /// Dictionary containing all call state transitions.
        /// </summary>
        private StateTransitions CallTransitions;

        /// <summary>
        /// Dictionary containing all action bindings.
        /// </summary>
        private ActionBindings ActionBindings;

        /// <summary>
        /// Set of ignored event types.
        /// </summary>
        internal HashSet<Type> IgnoredEvents;

        /// <summary>
        /// Set of deferred event types.
        /// </summary>
        internal HashSet<Type> DeferredEvents;

        #endregion

        #region P# internal methods

        /// <summary>
        /// Initializes the state.
        /// </summary>
        /// <param name="sst">Step state transitions</param>
        /// <param name="cst">Call state transitions</param>
        /// <param name="ab">Action bindings</param>
        internal void InitializeState(StateTransitions st,
            StateTransitions ct, ActionBindings ab)
        {
            if (st == null) this.StepTransitions = new StateTransitions();
            else this.StepTransitions = st;

            if (ct == null) this.CallTransitions = new StateTransitions();
            else this.CallTransitions = ct;

            if (ab == null) this.ActionBindings = new ActionBindings();
            else this.ActionBindings = ab;

            this.IgnoredEvents = this.DefineIgnoredEvents();
            this.DeferredEvents = this.DefineDeferredEvents();

            this.DoErrorChecking();
        }

        /// <summary>
        /// Checks if the state contains a step state transition
        /// triggered from the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Boolean value</returns>
        internal bool ContainsStepTransition(Event e)
        {
            return this.StepTransitions.ContainsKey(e.GetType());
        }

        /// <summary>
        /// Checks if the state contains a call state transition
        /// triggered from the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Boolean value</returns>
        internal bool ContainsCallTransition(Event e)
        {
            return this.CallTransitions.ContainsKey(e.GetType());
        }

        /// <summary>
        /// Checks if the state contains an action binding
        /// triggered from the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Boolean value</returns>
        internal bool ContainsActionBinding(Event e)
        {
            return this.ActionBindings.ContainsKey(e.GetType());
        }

        /// <summary>
        /// Returns the type of the state that is the target of
        /// the step transition triggered by the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Type of the state</returns>
        internal Type GetStepTransition(Event e)
        {
            return this.StepTransitions[e.GetType()];
        }

        /// <summary>
        /// Returns the type of the state that is the target of
        /// the call transition triggered by the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Type of the state</returns>
        internal Type GetCallTransition(Event e)
        {
            return this.CallTransitions[e.GetType()];
        }

        /// <summary>
        /// Returns the action that is triggered by the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <returns>Action</returns>
        internal Action GetActionBinding(Event e)
        {
            return this.ActionBindings[e.GetType()];
        }

        #endregion

        #region P# API methods

        /// <summary>
        /// Method to be executed when entering the state.
        /// </summary>
        public virtual void OnEntry() { }

        /// <summary>
        /// Method to be executed when exiting the state.
        /// </summary>
        public virtual void OnExit() { }

        /// <summary>
        /// Defines all event types that are ignored by this state.
        /// </summary>
        /// <returns>Set of event types</returns>
        protected virtual HashSet<Type> DefineIgnoredEvents()
        {
            return new HashSet<Type>();
        }

        /// <summary>
        /// Defines all event types that are deferred by this state.
        /// </summary>
        /// <returns>Set of event types</returns>
        protected virtual HashSet<Type> DefineDeferredEvents()
        {
            return new HashSet<Type>();
        }

        /// <summary>
        /// Sends an asynchronous event to a machine. The P# runtime
        /// treats the send as a new operation.
        /// </summary>
        /// <param name="m">Machine</param>
        /// <param name="e">Event</param>
        protected void SendNew(Machine m, Event e)
        {
            Runtime.Send(this.Machine.GetType().Name, m, e);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="m">Machine</param>
        /// <param name="e">Event</param>
        protected void Send(Machine m, Event e)
        {
            Runtime.Send(this.Machine.GetType().Name, m, e);
        }

        /// <summary>
        /// Adds the given event to this machine's mailbox.
        /// </summary>
        protected void Raise(Event e)
        {
            Utilities.Verbose("Machine {0} raised event {1}.\n", this.Machine, e);
            e.Operation = new Operation(true);
            this.Machine.Inbox.Add(e);
        }

        /// <summary>
        /// Performs a call state transition to the given target state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        protected void Call(Type s)
        {
            this.Machine.Call(s);
        }

        /// <summary>
        /// Stop listening to events and delete the machine.
        /// </summary>
        protected void Delete()
        {
            this.Machine.Delete();
        }

        #endregion

        #region factory methods

        internal static class Factory
        {
            /// <summary>
            /// Create a new state.
            /// </summary>
            /// <param name="s">Type of state</param>
            /// <returns></returns>
            internal static State CreateState(Type s)
            {
                return Activator.CreateInstance(s) as State;
            }
        }

        #endregion

        #region error checking

        /// <summary>
        /// Check machine for errors.
        /// </summary>
        private void DoErrorChecking()
        {
            List<Type> events = new List<Type>();

            events.AddRange(this.StepTransitions.Keys);
            events.AddRange(this.CallTransitions.Keys);
            events.AddRange(this.ActionBindings.Keys);

            for (int i = 0; i < events.Count; i++)
            {
                for (int j = 0; j < events.Count; j++)
                {
                    if (i == j)
                        continue;
                    Runtime.Assert(events[i] != events[j], "The state {0} contains " +
                        "the event {1} that triggers more than one state transitions " +
                        "or action bindings.\n", this, events[i]);
                }
            }
        }

        #endregion
    }
}
