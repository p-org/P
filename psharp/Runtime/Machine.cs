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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a state machine.
    /// </summary>
    public abstract class Machine
    {
        #region static fields

        /// <summary>
        /// Monotonically increasing machine ID counter.
        /// </summary>
        private static int IdCounter = 0;

        #endregion

        #region fields

        /// <summary>
        /// Unique machine ID.
        /// </summary>
        internal readonly int Id;

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
        private Dictionary<Type, StepStateTransitions> StepTransitions;

        /// <summary>
        /// Collection of all possible call state transitions.
        /// </summary>
        private Dictionary<Type, CallStateTransitions> CallTransitions;

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
        /// Inbox of the state machine (used during bug-finding mode).
        /// Incoming events are queued here. Events are dequeued to be
        /// processed. A thread-safe blocking collection is used.
        /// </summary>
        internal SystematicBlockingQueue<Event> ScheduledInbox;

        /// <summary>
        /// The raised event if one exists. This has higher priority
        /// over any other events in the inbox queue.
        /// </summary>
        internal Event RaisedEvent;

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
            this.Id = Machine.IdCounter++;

            if (Runtime.Options.Mode == Runtime.Mode.Execution)
                this.Inbox = new BlockingCollection<Event>();
            else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
                this.ScheduledInbox = new SystematicBlockingQueue<Event>();

            this.RaisedEvent = null;
            this.StateStack = new Stack<State>();
            this.Wrapper = null;
            this.IsActive = true;

            this.CTS = new CancellationTokenSource();

            this.StepTransitions = this.DefineStepStateTransitions();
            this.CallTransitions = this.DefineCallStateTransitions();
            this.ActionBindings = this.DefineActionBindings();

            this.InitializeStates();
            this.AssertStateValidity();
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
                            Runtime.Assert(initialState == null, "Machine '{0}' can not have " +
                                "more than one initial states.\n", this.GetType().Name);
                            initialState = s;
                        }

                        stateTypes.Add(s);
                    }
                }

                machineType = machineType.BaseType;
            }

            foreach (Type s in stateTypes)
            {
                Runtime.Assert(s.BaseType == typeof(State), "State '{0}' is " +
                        "not of the correct type.\n", s.Name);
                State state = State.Factory.CreateState(s);

                StepStateTransitions sst = null;
                CallStateTransitions cst = null;
                ActionBindings ab = null;

                this.StepTransitions.TryGetValue(s, out sst);
                this.CallTransitions.TryGetValue(s, out cst);
                this.ActionBindings.TryGetValue(s, out ab);

                state.Machine = this;
                state.InitializeState(sst, cst, ab);

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
        /// <param name="onExit">Lambda to override OnExit</param>
        private void Goto(Type s, Action onExit)
        {
            State nextState = this.GetTransitionStateFromType(s);
            // The machine performs the on exit statements of the current state.
            this.ExecuteCurrentStateOnExit(onExit);
            // The machine transitions to the new state.
            this.StateStack.Pop();
            this.StateStack.Push(nextState);
            // The machine performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry();
            Runtime.Assert(this.StateStack.Peek() != null, "Machine '{0}' cannot not " +
                "have a null current state.\n", this.GetType().Name);
        }

        /// <summary>
        /// Performs a call transition to the given state.
        /// This transition is similar to a function call
        /// in regular programming languages.
        /// </summary>
        /// <param name="s">Type of the state</param>
        private void Push(Type s)
        {
            State nextState = this.GetTransitionStateFromType(s);
            // The machine transitions to the new state.
            this.StateStack.Push(nextState);
            // The machine performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry();
            Runtime.Assert(this.StateStack.Peek() != null, "Machine '{0}' cannot not " +
                "have a null current state.\n", this.GetType().Name);
        }

        /// <summary>
        /// Performs an action.
        /// </summary>
        /// <param name="a">Action</param>
        private void Do(Action a)
        {
            try
            {
                a();
            }
            catch (EventRaisedException ex)
            {
                // Assigns the raised event.
                this.RaisedEvent = ex.RaisedEvent;
            }
            catch (ReturnUsedException ex)
            {
                // Handles the returning state.
                this.AssertReturnStatementValidity(ex.ReturningState);
            }
            catch (TaskCanceledException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                // Handles generic exception.
                this.ReportGenericAssertion(ex);
            }
        }

        /// <summary>
        /// The machine handles the given event.
        /// </summary>
        /// <param name="e">Type of the event</param>
        private void HandleEvent(Event e)
        {
            Runtime.Assert(e != null, "Machine '{0}' received a null event.\n", this);

            State currentState = this.StateStack.Peek();
            this.Message = e.GetType();
            this.Payload = e.Payload;

            // Checks if the event can be handled by the current state.
            if (!currentState.CanHandleEvent(e))
            {
                // Checks if the event can be handled by any state in the call
                // state stack, if there are any. If it can be handled, then
                // the call state stack is manipulated appropriately.
                if (!this.TryFindStateThatCanHandleEvent(ref currentState, e))
                {
                    Runtime.Assert(!(currentState.IgnoredEvents.Contains(e.GetType()) &&
                        currentState.DeferredEvents.Contains(e.GetType())), "Machine '{0}' " +
                        "received event '{1}' which is both ignored and deferred in state '{2}'.\n",
                        this.GetType().Name, e.GetType().Name, currentState.GetType().Name);
                    Runtime.Assert(currentState.IgnoredEvents.Contains(e.GetType()) ||
                        currentState.DeferredEvents.Contains(e.GetType()), "Machine '{0}' " +
                        "received event '{1}', which cannot be handled in state '{2}'.\n",
                        this.GetType().Name, e.GetType().Name, currentState.GetType().Name);

                    // If the event to process is in the ignored set of the current
                    // state (or stack of states in case of a call transition), then
                    // the event is removed from the inbox queue.
                    if (currentState.IgnoredEvents.Contains(e.GetType()))
                    {
                        return;
                    }
                    // If the event to process is in the deferred set of the current
                    // state (or stack of states in case of a call transition), then
                    // the event processing is deferred and the machine will attempt
                    // to process the next event in the inbox queue.
                    else if (currentState.DeferredEvents.Contains(e.GetType()))
                    {
                        if (Runtime.Options.Mode == Runtime.Mode.Execution)
                            this.Inbox.Add(e);
                        else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
                            this.ScheduledInbox.Add(e);
                        return;
                    }
                }
            }

            // If the event is neither in the ignored nor in the deferred set
            // of the current state, then the machine can process it. The
            // machine checks if the event triggers a step state transition.
            if (currentState.ContainsStepTransition(e))
            {
                var transition = currentState.GetStepTransition(e);
                Type targetState = transition.Item1;
                Action onExitOverride = transition.Item2;
                Utilities.Verbose("{0}: {1} --- STEP ---> {2}\n",
                    this, currentState, targetState);
                this.Goto(targetState, onExitOverride);
            }
            // If the event is neither in the ignored nor in the deferred set
            // of the current state, then the machine can process it. The
            // machine checks if the event triggers a call state transition.
            else if (currentState.ContainsCallTransition(e))
            {
                Type targetState = currentState.GetCallTransition(e);
                Utilities.Verbose("{0}: {1} --- CALL ---> {2}\n",
                    this, currentState, targetState);
                this.Push(targetState);
            }
            // If the event is neither in the ignored nor in the deferred set
            // of the current state, then the machine can process it. The
            // machine checks if the event triggers an action.
            else if (currentState.ContainsActionBinding(e))
            {
                Action action = currentState.GetActionBinding(e);
                this.Do(action);
            }
        }

        #endregion

        #region P# internal methods

        /// <summary>
        /// Starts the machine concurrently with an optional payload.
        /// </summary>
        /// /// <param name="payload">Optional payload</param>
        /// <returns>Task</returns>
        internal Thread Start(Object payload = null)
        {
            Thread thread = new Thread((Object pl) =>
            {
                try
                {
                    this.GotoInitialState(pl);

                    while (this.IsActive)
                    {
                        if (this.RaisedEvent != null)
                        {
                            Event nextEvent = this.RaisedEvent;
                            this.RaisedEvent = null;
                            this.HandleEvent(nextEvent);
                        }
                        else
                        {
                            // We are using a blocking collection so the attempt to
                            // dequeue an event will block if there is no available
                            // event in the mailbox. The operation will unblock when
                            // the next event arrives.
                            Event nextEvent = this.Inbox.Take(this.CTS.Token);
                            if (this.CTS.Token.IsCancellationRequested)
                                break;
                            this.HandleEvent(nextEvent);
                        }
                    }
                }
                catch (TaskCanceledException) { }
            });

            thread.Start(payload);

            return thread;
        }

        /// <summary>
        /// Starts the machine concurrently with an optional payload and
        /// the scheduler enabled.
        /// </summary>
        /// /// <param name="payload">Optional payload</param>
        /// <returns>Task</returns>
        internal Thread ScheduledStart(Object payload = null)
        {
            Thread thread = new Thread((Object pl) =>
            {
                ThreadInfo currThread = Runtime.Scheduler.GetCurrentThreadInfo();
                Runtime.Scheduler.ThreadStarted(currThread);

                try
                {
                    if (Runtime.Scheduler.DeadlockHasOccurred)
                    {
                        throw new TaskCanceledException();
                    }

                    this.GotoInitialState(pl);

                    while (this.IsActive)
                    {
                        if (this.RaisedEvent != null)
                        {
                            Event nextEvent = this.RaisedEvent;
                            this.RaisedEvent = null;
                            this.HandleEvent(nextEvent);
                        }
                        else
                        {
                            // We are using a blocking collection so the attempt to
                            // dequeue an event will block if there is no available
                            // event in the mailbox. The operation will unblock when
                            // the next event arrives.
                            Event nextEvent = this.ScheduledInbox.Take();
                            this.HandleEvent(nextEvent);
                        }
                    }
                }
                catch (TaskCanceledException) { }

                Runtime.Scheduler.ThreadEnded(currThread);
            });

            ThreadInfo threadInfo = Runtime.Scheduler.AddNewThreadInfo(thread);

            thread.Start(payload);

            Runtime.Scheduler.WaitForThreadToStart(threadInfo);

            return thread;
        }

        /// <summary>
        /// Starts the machine at the initial state with an
        /// optional payload.
        /// </summary>
        /// /// <param name="payload">Optional payload</param>
        internal void GotoInitialState(Object payload = null)
        {
            this.Payload = payload;
            // Performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// The machine handles the next available event in its inbox.
        /// </summary>
        internal void HandleNextEvent()
        {
            System.Diagnostics.Debug.Assert(false);

            if (this.RaisedEvent != null)
            {
                Event nextEvent = this.RaisedEvent;
                this.RaisedEvent = null;
                this.HandleEvent(nextEvent);
            }
            else
            {
                //Event nextEvent = this.Inbox.Take(this.CTS.Token);
                //this.HandleEvent(nextEvent);
            }
        }

        /// <summary>
        /// Stop listening to events.
        /// </summary>
        internal void StopListener()
        {
            this.IsActive = false;

            if (Runtime.Options.Mode == Runtime.Mode.Execution)
                this.CTS.Cancel();
            else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
                this.ScheduledInbox.Cancel();
        }

        /// <summary>
        /// Resets the machine ID counter.
        /// </summary>
        internal static void ResetMachineIDCounter()
        {
            Machine.IdCounter = 0;
        }

        #endregion

        #region P# API methods

        /// <summary>
        /// Defines all possible step state transitions for each state.
        /// It must return a dictionary where a key represents
        /// a state and a value represents the state's transitions.
        /// </summary>
        /// <returns>Dictionary<Type, StateTransitions></returns>
        protected virtual Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            return new Dictionary<Type, StepStateTransitions>();
        }

        /// <summary>
        /// Defines all possible call state transitions for each state.
        /// It must return a dictionary where a key represents
        /// a state and a value represents the state's transitions.
        /// </summary>
        /// <returns>Dictionary<Type, StateTransitions></returns>
        protected virtual Dictionary<Type, CallStateTransitions> DefineCallStateTransitions()
        {
            return new Dictionary<Type, CallStateTransitions>();
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
        protected internal void Send(Machine m, Event e)
        {
            Runtime.Send(this.GetType().Name, m, e);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        protected internal void Invoke<T>(Event e)
        {
            Runtime.Invoke<T>(e);
        }

        /// <summary>
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="e">Event</param>
        protected internal void Raise(Event e)
        {
            Utilities.Verbose("Machine {0} raised event {1}.\n", this, e);
            throw new EventRaisedException(e);
        }

        /// <summary>
        /// Pops the current state from the call state stack.
        /// </summary>
        protected internal void Return()
        {
            throw new ReturnUsedException(this.StateStack.Pop());
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
            /// <param name="payload">Optional payload</param>
            /// <returns>Machine</returns>
            public static Machine CreateMachine(Type m, Object payload = null)
            {
                Runtime.Assert(m.IsSubclassOf(typeof(Machine)), "The provided " +
                    "type '{0}' is not a subclass of Machine.\n", m.Name);
                Machine machine = Runtime.TryCreateNewMachineInstance(m, payload);
                return machine;
            }

            /// <summary>
            /// Creates a new machine of type T with an optional payload.
            /// </summary>
            /// <typeparam name="T">Type of machine</typeparam>
            /// <param name="payload">Optional payload</param>
            /// <returns>Machine</returns>
            public static T CreateMachine<T>(Object payload = null)
            {
                Runtime.Assert(typeof(T).IsSubclassOf(typeof(Machine)), "The provided " +
                    "type '{0}' is not a subclass of Machine.\n", typeof(T).Name);
                T machine = Runtime.TryCreateNewMachineInstance<T>(payload);
                return machine;
            }

            /// <summary>
            /// Creates a new monitor of type T with an optional payload.
            /// </summary>
            /// <param name="m">Type of monitor</param>
            /// <param name="payload">Optional payload</param>
            public static void CreateMonitor(Type m, Object payload = null)
            {
                Runtime.Assert(m.IsSubclassOf(typeof(Machine)), "The provided " +
                    "type '{0}' is not a subclass of Machine.\n", m.Name);
                Runtime.Assert(m.IsDefined(typeof(Monitor), false), "The provided " +
                    "type '{0}' is not a monitor.\n", m.Name);
                Machine machine = Runtime.TryCreateNewMonitorInstance(m, payload);
            }

            /// <summary>
            /// Creates a new monitor of type T with an optional payload.
            /// </summary>
            /// <typeparam name="T">Type of monitor</typeparam>
            /// <param name="payload">Optional payload</param>
            public static void CreateMonitor<T>(Object payload = null)
            {
                Runtime.Assert(typeof(T).IsSubclassOf(typeof(Machine)), "The provided " +
                    "type '{0}' is not a subclass of Machine.\n", typeof(T).Name);
                Runtime.Assert(typeof(T).IsDefined(typeof(Monitor), false), "The provided " +
                    "type '{0}' is not a monitor.\n", typeof(T).Name);
                T machine = Runtime.TryCreateNewMonitorInstance<T>(payload);
            }
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Gets the transition state from the given type. The method also
        /// performs error checking to ensure the state's validity.
        /// </summary>
        /// <param name="s">Type of the state</param>
        /// <returns>State</returns>
        private State GetTransitionStateFromType(Type s)
        {
            State transitionState = null;

            Runtime.Assert(s != null, "Machine '{0}' tried to transition to a " +
                "null state from state '{1}'.\n", this.GetType().Name,
                this.StateStack.Peek().GetType().Name);

            try
            {
                transitionState = this.States.First(state => state.GetType() == s);
            }
            catch (InvalidOperationException ex)
            {
                Runtime.Assert(ex == null, "Machine '{0}' tried to transition to invalid " +
                    "state '{1}' from state '{2}'.\n", this.GetType().Name, s.Name,
                    this.StateStack.Peek().GetType().Name);
            }

            return transitionState;
        }

        /// <summary>
        /// Executes the on entry function of the current state.
        /// </summary>
        private void ExecuteCurrentStateOnEntry()
        {
            try
            {
                // Performs the on entry statements of the new state.
                this.StateStack.Peek().ExecuteEntryFunction();
            }
            catch (EventRaisedException ex)
            {
                // Assigns the raised event.
                this.RaisedEvent = ex.RaisedEvent;
            }
            catch (ReturnUsedException ex)
            {
                // Handles the returning state.
                this.AssertReturnStatementValidity(ex.ReturningState);
            }
            catch (TaskCanceledException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                // Handles generic exception.
                this.ReportGenericAssertion(ex);
            }
        }

        /// <summary>
        /// Executes the on exit function of the current state.
        /// </summary>
        /// <param name="onExit">Lambda to override OnExit</param>
        private void ExecuteCurrentStateOnExit(Action onExit)
        {
            try
            {
                if (onExit == null)
                {
                    // Performs the on exit statements of the current state.
                    this.StateStack.Peek().ExecuteExitFunction();
                }
                else
                {
                    // Overrides the on exit method of the current state.
                    onExit();
                }
            }
            catch (EventRaisedException ex)
            {
                // Assigns the raised event.
                this.RaisedEvent = ex.RaisedEvent;
            }
            catch (ReturnUsedException ex)
            {
                // Handles the returning state.
                this.AssertReturnStatementValidity(ex.ReturningState);
            }
            catch (TaskCanceledException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                // Handles generic exception.
                this.ReportGenericAssertion(ex);
            }
        }

        /// <summary>
        /// Attempts to find a state in the call state stack that can handle
        /// the given event. If such a state is found, then it manipulates
        /// the call state stack appropriately.
        /// </summary>
        /// <param name="currentState">Current state</param>
        /// <param name="e">Event</param>
        /// <returns>Boolean value</returns>
        private bool TryFindStateThatCanHandleEvent(ref State currentState, Event e)
        {
            bool result = false;
            State handlerState = null;

            foreach (var state in this.StateStack)
            {
                if (!state.Equals(currentState))
                {
                    if (state.ContainsStepTransition(e) ||
                        state.ContainsCallTransition(e))
                    {
                        result = true;
                        handlerState = state;
                        break;
                    }

                    if (state.ContainsActionBinding(e))
                    {
                        result = true;
                        currentState = state;
                        break;
                    }
                }
            }

            if (result && handlerState != null)
            {
                while (!this.StateStack.Peek().Equals(handlerState))
                {
                    this.StateStack.Pop();
                }

                currentState = handlerState;
            }

            return result;
        }

        #endregion

        #region generic public and override methods

        /// <summary>
        /// Determines whether the specified machine is equal
        /// to the current machine.
        /// </summary>
        /// <param name="m">Machine</param>
        /// <returns>Boolean value</returns>
        public bool Equals(Machine m)
        {
            if (m == null)
            {
                return false;
            }

            return this.Id == m.Id;
        }

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean value</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Machine m = obj as Machine;
            if (m == null)
            {
                return false;
            }

            return this.Id == m.Id;
        }

        /// <summary>
        /// Hash function.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.GetType().Name;
        }

        #endregion

        #region error checking

        /// <summary>
        /// Check machine for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            Runtime.Assert(this.States.Count > 0, "Machine '{0}' must " +
                "have one or more states.\n", this.GetType().Name);
            Runtime.Assert(this.StateStack.Peek() != null, "Machine '{0}' " +
                "must not have a null current state.\n", this.GetType().Name);
        }

        /// <summary>
        /// Checks if the Return() statement was performed properly.
        /// </summary>
        /// <param name="returningState">Returnig state</param>
        private void AssertReturnStatementValidity(State returningState)
        {
            Runtime.Assert(this.StateStack.Count > 0, "Machine '{0}' executed a Return() " +
                "statement while there was only the state '{1}' in the stack.\n",
                this.GetType().Name, returningState.GetType().Name);
        }

        /// <summary>
        /// Reports the generic assertion and raises a generic
        /// runtime assertion error.
        /// </summary>
        /// <param name="ex">Exception</param>
        private void ReportGenericAssertion(Exception ex)
        {
            Runtime.Assert(false, "Exception '{0}' was thrown while machine '{1}' was " +
                "in state '{2}'. The stack trace is:\n{3}\n", ex.GetType(), this.GetType().Name,
                this.StateStack.Peek().GetType().Name, ex.StackTrace);
        }

        #endregion
    }
}
