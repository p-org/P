//-----------------------------------------------------------------------
// <copyright file="Machine.cs" company="Microsoft">
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Scheduling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state machine.
    /// </summary>
    public abstract class Machine
    {
        #region fields
        
        /// <summary>
        /// Set of all possible states.
        /// </summary>
        private HashSet<State> States;

        /// <summary>
        /// A stack of machine states. The state on the top of
        /// the stack represents the current state. Usually
        /// there is only one state in the stack, unless a call
        /// transition was performed which emulates a function
        /// call in regular programming languages.
        /// </summary>
        private Stack<State> StateStack;

        /// <summary>
        /// Handle to an optional wrapper for this machine.
        /// A wrapper can be used for environment to machine
        /// communication through function calls.
        /// </summary>
        protected internal MachineWrapper Wrapper;

        /// <summary>
        /// False if machine has stopped.
        /// </summary>
        private bool IsActive;

        /// <summary>
        /// Cancellation token source for the machine.
        /// </summary>
        private CancellationTokenSource CTS;

        /// <summary>
        /// Collection of all possible step state transitions.
        /// </summary>
        private Dictionary<Type, StateTransitions> StepTransitions;

        /// <summary>
        /// Collection of all possible call state transitions.
        /// </summary>
        private Dictionary<Type, StateTransitions> CallTransitions;

        /// <summary>
        /// Collection of all possible action bindings.
        /// </summary>
        private Dictionary<Type, ActionBindings> ActionBindings;

        /// <summary>
        /// Inbox of the state machine. Incoming events are
        /// queued here. Events are dequeued to be processed.
        /// A thread-safe blocking collection is used.
        /// </summary>
        internal BlockingCollection<Event> Inbox;

        /// <summary>
        /// The operation associated with the machine.
        /// </summary>
        internal Operation Operation;

        /// <summary>
        /// Handle to the latest received event type.
        /// If there was no event received yet the returned
        /// value is null.
        /// </summary>
        protected internal Type Message;

        /// <summary>
        /// Handle to the payload of the last received event.
        /// If the last received event does not have a payload,
        /// a null value is returned.
        /// </summary>
        protected internal Object Payload;
        
        #endregion

        #region machine constructors

        /// <summary>
        /// Constructor of the Machine class.
        /// </summary>
        protected Machine()
        {
            this.Inbox = new BlockingCollection<Event>();
            this.StateStack = new Stack<State>();
            this.Operation = null;
            this.Wrapper = null;
            this.IsActive = true;

            this.CTS = new CancellationTokenSource();

            this.StepTransitions = this.DefineStepTransitions();
            this.CallTransitions = this.DefineCallTransitions();
            this.ActionBindings = this.DefineActionBindings();

            this.InitializeStates();
            this.DoErrorChecking();
        }

        #endregion

        #region private machine methods

        /// <summary>
        /// Initializes the states of the machine.
        /// </summary>
        private void InitializeStates()
        {
            this.States = new HashSet<State>();
            HashSet<Type> stateTypes = new HashSet<Type>();
            
            Type machineType = this.GetType();
            Type initialState = null;

            while (machineType != typeof(Machine))
            {
                foreach (var s in machineType.GetNestedTypes(BindingFlags.Instance |
                    BindingFlags.NonPublic | BindingFlags.Public |
                    BindingFlags.DeclaredOnly))
                {
                    if (s.IsClass && s.IsSubclassOf(typeof(State)))
                    {
                        if (s.IsDefined(typeof(Initial), false))
                        {
                            Runtime.Assert(initialState == null, "Machine {0} can " +
                                "not have more than one initial states.\n", this);
                            initialState = s;
                        }

                        stateTypes.Add(s);
                    }
                }

                machineType = machineType.BaseType;
            }

            foreach (Type s in stateTypes)
            {
                Runtime.Assert(s.BaseType == typeof(State), "The state is " +
                        "not of the correct type.\n");
                State state = State.Factory.CreateState(s);

                StateTransitions st = null;
                StateTransitions ct = null;
                ActionBindings ab = null;

                this.StepTransitions.TryGetValue(s, out st);
                this.CallTransitions.TryGetValue(s, out ct);
                this.ActionBindings.TryGetValue(s, out ab);

                state.Machine = this;
                state.InitializeState(st, ct, ab);

                this.States.Add(state);
            }

            foreach (State s in this.States)
            {
                if (initialState == s.GetType())
                {
                    this.StateStack.Push(s);
                }
            }
        }

        /// <summary>
        /// Performs a step transition to the given state.
        /// This is an actual transition from the current
        /// state to the new one.
        /// </summary>
        /// <param name="s">Type of the state</param>
        private void Goto(Type s)
        {
            // Performs the on exit statements of the current state.
            this.StateStack.Peek().OnExit();

            // The machine transitions to the new state.
            this.StateStack.Pop();
            this.StateStack.Push(this.States.First(state => state.GetType() == s));

            // Performs the on entry statements of the new state.
            this.StateStack.Peek().OnEntry();

            Runtime.Assert(this.StateStack.Peek() != null, "The machine's current " +
                "state cannot be null.\n");
        }

        /// <summary>
        /// Performs a call transition to the given state.
        /// This transition is similar to a function call
        /// in regular programming languages.
        /// </summary>
        /// <param name="s">Type of the state</param>
        private void Push(Type s)
        {
            // The machine transitions to the new state.
            this.StateStack.Push(this.States.First(state => state.GetType() == s));

            // Performs the on entry statements of the new state.
            this.StateStack.Peek().OnEntry();

            Runtime.Assert(this.StateStack.Peek() != null, "The machine's current " +
                "state cannot be null.\n");
        }

        /// <summary>
        /// Performs an action.
        /// </summary>
        /// <param name="a"></param>
        private void Do(Action a)
        {
            a();
        }

        /// <summary>
        /// The machine starts listening for incoming events.
        /// </summary>
        private void StartListener()
        {
            while (this.IsActive)
            {
                // We are using a blocking collection so the attempt to
                // take an event will block if there is no available
                // event int he mailbox. The operation will unblock when
                // the next event arrives.
                Event nextEvent = this.Inbox.Take(this.CTS.Token);
                if (this.CTS.Token.IsCancellationRequested)
                    break;

                // If in bug-finding mode, only proceed if the operation
                // of the machine equals to the current operation of the
                // P# runtime.
                if (Runtime.Options.Mode == Runtime.Mode.BugFinding &&
                    nextEvent.Operation.Id > 0)
                {
                    this.Operation = nextEvent.Operation;
                    while (this.Operation.Id != Runtime.Operation.Id)
                    {
                        Thread.Sleep(10);
                    }
                }

                State currentState = this.StateStack.Peek();

                this.Message = nextEvent.GetType();
                this.Payload = nextEvent.Payload;

                if (nextEvent != null)
                {
                    // If next event to process is in the ignored set of the current
                    // state, then the event is removed from the inbox queue.
                    if (currentState.IgnoredEvents.Contains(nextEvent.GetType()))
                    {
                        continue;
                    }
                    // If next event to process is in the deferred set of the current
                    // state, then the event processing is deferred and the machine
                    // will attempt to process the next event in the inbox queue.
                    else if (currentState.DeferredEvents.Contains(nextEvent.GetType()))
                    {
                        this.Inbox.Add(nextEvent);
                        continue;
                    }
                    // If the event is neither in the ignored nor in the deferred set
                    // of the current state, then the machine can process it. The
                    // machine checks if the event triggers an action.
                    else if (currentState.ContainsActionBinding(nextEvent))
                    {
                        Action action = currentState.GetActionBinding(nextEvent);
                        this.Do(action);
                        continue;
                    }
                    // If the event is neither in the ignored nor in the deferred set
                    // of the current state, then the machine can process it. The
                    // machine checks if the event triggers a step state transition.
                    else if (currentState.ContainsStepTransition(nextEvent))
                    {
                        Type targetState = currentState.GetStepTransition(nextEvent);
                        Utilities.Verbose("{0}: {1} --- STEP ---> {2}\n",
                            this, currentState, targetState);
                        this.Goto(targetState);
                        continue;
                    }
                    // If the event is neither in the ignored nor in the deferred set
                    // of the current state, then the machine can process it. The
                    // machine checks if the event triggers a call state transition.
                    else if (currentState.ContainsCallTransition(nextEvent))
                    {
                        Type targetState = currentState.GetCallTransition(nextEvent);
                        Utilities.Verbose("{0}: {1} --- CALL ---> {2}\n",
                            this, currentState, targetState);
                        this.Push(targetState);
                        continue;
                    }
                    // The event cannot be handled by the current state. The machine
                    // attempts to pop the current state and handle the event with the
                    // underlying state in the stack. If the stack is empty it means
                    // that the event was erroneous sent and the runtime reports an
                    // error before terminating.
                    else
                    {
                        this.StateStack.Pop();
                        Runtime.Assert(this.StateStack.Count > 0, "Machine {0} " +
                            "received an event ({1}) that cannot be handled in " +
                            "state {2}.\n", this, nextEvent, currentState);
                    }
                }
            }
        }

        #endregion

        #region P# internal methods

        /// <summary>
        /// Starts the machine with an optional payload.
        /// </summary>
        /// /// <param name="payload">Optional payload</param>
        internal void Start(Object payload = null)
        {
            Task.Factory.StartNew((Object pl) =>
            {
                this.Payload = pl;
                this.StateStack.Peek().OnEntry();
                this.StartListener();
            }, payload);
        }

        /// <summary>
        /// Stop listening to events.
        /// </summary>
        internal void StopListener()
        {
            this.IsActive = false;
            this.CTS.Cancel();
        }

        #endregion

        #region P# API methods

        /// <summary>
        /// Defines all possible step state transitions for each state.
        /// It must return a dictionary where a key represents
        /// a state and a value represents the state's transitions.
        /// </summary>
        /// <returns>Dictionary<Type, StateTransitions></returns>
        protected virtual Dictionary<Type, StateTransitions> DefineStepTransitions()
        {
            return new Dictionary<Type, StateTransitions>();
        }

        /// <summary>
        /// Defines all possible call state transitions for each state.
        /// It must return a dictionary where a key represents
        /// a state and a value represents the state's transitions.
        /// </summary>
        /// <returns>Dictionary<Type, StateTransitions></returns>
        protected virtual Dictionary<Type, StateTransitions> DefineCallTransitions()
        {
            return new Dictionary<Type, StateTransitions>();
        }

        /// <summary>
        /// Defines all possible action bindings for each state.
        /// It must return a dictionary where a key represents
        /// a state and a value represents the state's action bindings.
        /// </summary>
        /// <returns>Dictionary<Type, ActionBindings></returns>
        protected virtual Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            return new Dictionary<Type, ActionBindings>();
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="m">Machine</param>
        /// <param name="e">Event</param>
        protected void Send(Machine m, Event e)
        {
            Runtime.Send(this.GetType().Name, m, e);
        }

        /// <summary>
        /// Adds the given event to this machine's mailbox.
        /// </summary>
        protected void Raise(Event e)
        {
            Utilities.Verbose("Machine {0} raised event {1}.\n", this, e);
            e.Operation = new Operation(true);
            this.Inbox.Add(e);
        }

        /// <summary>
        /// Performs a call state transition to the given target state.
        /// </summary>
        /// <param name="s">Type of the state</param>
        protected internal void Call(Type s)
        {
            this.Push(s);
        }

        /// <summary>
        /// Stop listening to events and delete the machine.
        /// </summary>
        protected internal void Delete()
        {
            this.StopListener();
            Runtime.Delete(this);
        }

        #endregion

        #region factory methods

        public static class Factory
        {
            /// <summary>
            /// Creates a new machine of type T with an optional payload.
            /// </summary>
            /// <param name="m">Type of machine</param>
            /// <param name="payload">Payload</param>
            /// <returns>Machine</returns>
            public static Machine CreateMachine(Type m, Object payload = null)
            {
                Runtime.Assert(m.IsSubclassOf(typeof(Machine)), "The provided " +
                    "type {0} is not a subclass of Machine.\n", m);
                Machine machine = Runtime.TryCreateNewMachineInstance(m);
                machine.Start(payload);
                return machine;
            }

            /// <summary>
            /// Creates a new machine of type T with an optional payload.
            /// </summary>
            /// <typeparam name="T">Type of machine</typeparam>
            /// <param name="payload">Payload</param>
            /// <returns>Machine</returns>
            public static T CreateMachine<T>(Object payload = null)
            {
                Runtime.Assert(typeof(T).IsSubclassOf(typeof(Machine)), "The provided " +
                    "type {0} is not a subclass of Machine.\n", typeof(T));
                T machine = Runtime.TryCreateNewMachineInstance<T>();
                (machine as Machine).Start(payload);
                return machine;
            }

            /// <summary>
            /// Creates a new monitor of type T with an optional payload.
            /// </summary>
            /// <param name="m">Type of monitor</param>
            /// <param name="payload">Payload</param>
            public static void CreateMonitor(Type m, Object payload = null)
            {
                Runtime.Assert(m.IsSubclassOf(typeof(Machine)), "The provided " +
                    "type {0} is not a subclass of Machine.\n", m);
                Runtime.Assert(m.IsDefined(typeof(Monitor), false), "The provided " +
                    "type {0} is not a monitor.\n", m);

                Machine machine = Runtime.TryCreateNewMonitorInstance(m);
                machine.Start(payload);
            }

            /// <summary>
            /// Creates a new monitor of type T with an optional payload.
            /// </summary>
            /// <typeparam name="T">Type of monitor</typeparam>
            /// <param name="payload">Payload</param>
            public static void CreateMonitor<T>(Object payload = null)
            {
                Runtime.Assert(typeof(T).IsSubclassOf(typeof(Machine)), "The provided " +
                    "type {0} is not a subclass of Machine.\n", typeof(T));
                Runtime.Assert(typeof(T).IsDefined(typeof(Monitor), false), "The provided " +
                    "type {0} is not a monitor.\n", typeof(T));

                T machine = Runtime.TryCreateNewMonitorInstance<T>();
                (machine as Machine).Start(payload);
            }
        }

        #endregion

        #region error checking

        /// <summary>
        /// Check machine for errors.
        /// </summary>
        private void DoErrorChecking()
        {
            Runtime.Assert(this.States.Count > 0, "Machine {0} must " +
                "have one or more states.\n", this);
            Runtime.Assert(this.StateStack.Peek() != null, "Machine {0} " +
                "must not have a null current state.\n", this);
        }

        #endregion
    }
}
