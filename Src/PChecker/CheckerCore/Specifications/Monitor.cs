// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using PChecker.Actors;
using PChecker.Actors.Events;
using PChecker.Actors.Handlers;
using PChecker.Actors.Logging;
using PChecker.Actors.StateTransitions;
using PChecker.Exceptions;

namespace PChecker.Specifications.Monitors
{
    /// <summary>
    /// Abstract class representing a specification monitor.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/core/specifications">Specifications Overview</see>
    /// for more information.
    /// </remarks>
    public abstract class Monitor
    {
        /// <summary>
        /// Map from monitor types to a set of all possible states types.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<Type>> StateTypeMap =
            new ConcurrentDictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Map from monitor types to a set of all available states.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<State>> StateMap =
            new ConcurrentDictionary<Type, HashSet<State>>();

        /// <summary>
        /// Map from monitor types to a set of all available actions.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> MonitorActionMap =
            new ConcurrentDictionary<Type, Dictionary<string, MethodInfo>>();

        /// <summary>
        /// A set of lockable objects used to protect static initialization of the ActionCache while
        /// also enabling multithreaded initialization of different Actor types.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> ActionCacheLocks =
            new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// A cached array that contains a single event type.
        /// </summary>
        private static readonly Type[] SingleEventTypeArray = new Type[] { typeof(Event) };

        /// <summary>
        /// The runtime that executes this monitor.
        /// </summary>
        private ActorRuntime Runtime;

        /// <summary>
        /// The active monitor state.
        /// </summary>
        private State ActiveState;

        /// <summary>
        /// Dictionary containing all the current action bindings.
        /// </summary>
        internal Dictionary<Type, EventHandlerDeclaration> EventHandlers;

        /// <summary>
        /// Map from action names to cached action delegates.
        /// </summary>
        private readonly Dictionary<string, CachedDelegate> ActionMap;

        /// <summary>
        /// Set of currently ignored event types.
        /// </summary>
        private HashSet<Type> IgnoredEvents;

        /// <summary>
        /// A counter that increases in each step of the execution,
        /// as long as the monitor remains in a hot state. If the
        /// temperature reaches the specified limit, then a potential
        /// liveness bug has been found.
        /// </summary>
        private int LivenessTemperature;

        /// <summary>
        /// Gets the name of this monitor.
        /// </summary>
        protected internal string Name => GetType().FullName;

        /// <summary>
        /// The logger installed to the runtime.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/core/logging" >Logging</see> for more information.
        /// </remarks>
        protected TextWriter Logger => Runtime.Logger;

        /// <summary>
        /// The runtime installed json logger;
        /// </summary>
        protected JsonWriter JsonLogger => Runtime.JsonLogger;

        /// <summary>
        /// Gets the current state.
        /// </summary>
        protected internal Type CurrentState
        {
            get
            {
                if (ActiveState is null)
                {
                    return null;
                }

                return ActiveState.GetType();
            }
        }

        /// <summary>
        /// Gets the current state name.
        /// </summary>
        internal string CurrentStateName
        {
            get => NameResolver.GetQualifiedStateName(CurrentState);
        }

        /// <summary>
        /// Gets the current state name with temperature.
        /// </summary>
        internal string CurrentStateNameWithTemperature
        {
            get
            {
                return CurrentStateName +
                       (IsInHotState() ? "[hot]" :
                           IsInColdState() ? "[cold]" :
                           string.Empty);
            }
        }

        /// <summary>
        /// User-defined hashed state of the monitor. Override to improve the
        /// accuracy of stateful techniques during testing.
        /// </summary>
        protected virtual int HashedState => 0;

        /// <summary>
        /// A pending transition object that has not been returned from ExecuteAction yet.
        /// </summary>
        private Transition PendingTransition;

        /// <summary>
        /// Initializes a new instance of the <see cref="Monitor"/> class.
        /// </summary>
        protected Monitor()
            : base()
        {
            ActionMap = new Dictionary<string, CachedDelegate>();
            LivenessTemperature = 0;
        }

        /// <summary>
        /// Initializes this monitor.
        /// </summary>
        /// <param name="runtime">The runtime that executes this monitor.</param>
        internal void Initialize(ActorRuntime runtime)
        {
            Runtime = runtime;
        }

        /// <summary>
        /// Raises the specified <see cref="Event"/> at the end of the current action.
        /// </summary>
        /// <remarks>
        /// This event is not handled until the action that calls this method returns control back
        /// to the Coyote runtime.  It is handled before any other events are dequeued from the inbox.
        /// Only one of the following can be called per action:
        /// <see cref="Monitor.RaiseEvent"/>, <see cref="Monitor.RaiseGotoStateEvent{T}"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        /// <param name="e">The event to raise.</param>
        protected void RaiseEvent(Event e)
        {
            Assert(e != null, "{0} is raising a null event.", GetType().FullName);
            CheckDanglingTransition();
            PendingTransition = new Transition(Transition.Type.Raise, default, e);
        }

        /// <summary>
        /// Raise a special event that performs a goto state operation at the end of the current action.
        /// </summary>
        /// <remarks>
        /// Goto state pops the current <see cref="State"/> and pushes the specified <see cref="State"/> on the active state stack.
        ///
        /// This is shorthand for the following code:
        /// <code>
        /// class Event E { }
        /// [OnEventGotoState(typeof(E), typeof(S))]
        /// this.RaiseEvent(new E());
        /// </code>
        /// This event is not handled until the action that calls this method returns control back
        /// to the Coyote runtime.  It is handled before any other events are dequeued from the inbox.
        /// Only one of the following can be called per action:
        /// <see cref="Monitor.RaiseEvent"/>, <see cref="Monitor.RaiseGotoStateEvent{T}"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        /// <typeparam name="S">Type of the state.</typeparam>
        protected void RaiseGotoStateEvent<S>()
            where S : State =>
            RaiseGotoStateEvent(typeof(S));

        /// <summary>
        /// Raise a special event that performs a goto state operation at the end of the current action.
        /// </summary>
        /// <remarks>
        /// Goto state pops the current <see cref="State"/> and pushes the specified <see cref="State"/> on the active state stack.
        ///
        /// This is shorthand for the following code:
        /// <code>
        /// class Event E { }
        /// [OnEventGotoState(typeof(E), typeof(S))]
        /// this.RaiseEvent(new E());
        /// </code>
        /// This event is not handled until the action that calls this method returns control back
        /// to the Coyote runtime.  It is handled before any other events are dequeued from the inbox.
        /// Only one of the following can be called per action:
        /// <see cref="Monitor.RaiseEvent"/>, <see cref="Monitor.RaiseGotoStateEvent{T}"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        /// <param name="state">Type of the state.</param>
        protected void RaiseGotoStateEvent(Type state)
        {
            // If the state is not a state of the monitor, then report an error and exit.
            Assert(StateTypeMap[GetType()].Any(val => val.DeclaringType.Equals(state.DeclaringType) && val.Name.Equals(state.Name)),
                "{0} is trying to transition to non-existing state '{1}'.", GetType().FullName, state.Name);
            CheckDanglingTransition();
            PendingTransition = new Transition(Transition.Type.Goto, state, default);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate)
        {
            if (!predicate)
            {
                Runtime.NotifyMonitorError(this);
                Runtime.Assert(false);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                Runtime.NotifyMonitorError(this);
                Runtime.Assert(false, s, args);
            }
        }

        /// <summary>
        /// Notifies the monitor to handle the received event.
        /// </summary>
        internal void MonitorEvent(Event e, string senderName, string senderType, string senderState)
        {
            e.Sender = senderName;
            Runtime.LogWriter.LogMonitorProcessEvent(GetType().FullName, CurrentStateName,
                senderName, senderType, senderState, e);
            HandleEvent(e);
        }

        /// <summary>
        /// Handles the given event.
        /// </summary>
        private void HandleEvent(Event e)
        {
            // Do not process an ignored event.
            if (IsEventIgnoredInCurrentState(e))
            {
                return;
            }

            while (true)
            {
                if (ActiveState is null)
                {
                    // If the event cannot be handled, then report an error and exit.
                    Assert(false, "{0} received event '{1}' that cannot be handled.",
                        GetType().FullName, e.GetType().FullName);
                }

                // If current state cannot handle the event then null the state.
                if (!CanHandleEvent(e.GetType()))
                {
                    Runtime.NotifyExitedState(this);
                    ActiveState = null;
                    continue;
                }

                if (e.GetType() == typeof(GotoStateEvent))
                {
                    // Checks if the event is a goto state event.
                    var targetState = (e as GotoStateEvent).State;
                    GotoState(targetState, null, e);
                }
                else if (EventHandlers.ContainsKey(e.GetType()))
                {
                    // Checks if the event can trigger an action.
                    var handler = EventHandlers[e.GetType()];
                    if (handler is ActionEventHandlerDeclaration action)
                    {
                        Do(action.Name, e);
                    }
                    else if (handler is GotoStateTransition transition)
                    {
                        GotoState(transition.TargetState, transition.Lambda, e);
                    }
                }
                else if (EventHandlers.ContainsKey(typeof(WildCardEvent)))
                {
                    // Checks if the event can trigger an action.
                    var handler = EventHandlers[typeof(WildCardEvent)];
                    if (handler is ActionEventHandlerDeclaration action)
                    {
                        Do(action.Name, e);
                    }
                    else if (handler is GotoStateTransition transition)
                    {
                        GotoState(transition.TargetState, transition.Lambda, e);
                    }
                }

                break;
            }
        }

        /// <summary>
        /// Checks if the specified event is ignored in the current monitor state.
        /// </summary>
        private bool IsEventIgnoredInCurrentState(Event e)
        {
            if (IgnoredEvents.Contains(e.GetType()) ||
                IgnoredEvents.Contains(typeof(WildCardEvent)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Invokes an action.
        /// </summary>
        [DebuggerStepThrough]
        private void Do(string actionName, Event e)
        {
            var cachedAction = ActionMap[actionName];
            Runtime.NotifyInvokedAction(this, cachedAction.MethodInfo, CurrentStateNameWithTemperature, e);
            ExecuteAction(cachedAction, e);
            ApplyEventHandlerTransition(PendingTransition);
        }

        /// <summary>
        /// Executes the on entry function of the current state.
        /// </summary>
        [DebuggerStepThrough]
        private void ExecuteCurrentStateOnEntry(Event e)
        {
            Runtime.NotifyEnteredState(this);

            CachedDelegate entryAction = null;
            if (ActiveState.EntryAction != null)
            {
                entryAction = ActionMap[ActiveState.EntryAction];
            }

            // Invokes the entry action of the new state,
            // if there is one available.
            if (entryAction != null)
            {
                ExecuteAction(entryAction, e);
                ApplyEventHandlerTransition(PendingTransition);
            }
        }

        /// <summary>
        /// Executes the on exit function of the current state.
        /// </summary>
        [DebuggerStepThrough]
        private void ExecuteCurrentStateOnExit(string eventHandlerExitActionName, Event e)
        {
            Runtime.NotifyExitedState(this);

            CachedDelegate exitAction = null;
            if (ActiveState.ExitAction != null)
            {
                exitAction = ActionMap[ActiveState.ExitAction];
            }

            // Invokes the exit action of the current state,
            // if there is one available.
            if (exitAction != null)
            {
                ExecuteAction(exitAction, e);
                var transition = PendingTransition;
                Assert(transition.TypeValue is Transition.Type.None,
                    "{0} has performed a '{1}' transition from an OnExit action.",
                    GetType().FullName, transition.TypeValue);
                ApplyEventHandlerTransition(transition);
            }

            // Invokes the exit action of the event handler,
            // if there is one available.
            if (eventHandlerExitActionName != null)
            {
                var eventHandlerExitAction = ActionMap[eventHandlerExitActionName];
                ExecuteAction(eventHandlerExitAction, e);
                var transition = PendingTransition;
                Assert(transition.TypeValue is Transition.Type.None,
                    "{0} has performed a '{1}' transition from an OnExit action.",
                    GetType().FullName, transition.TypeValue);
                ApplyEventHandlerTransition(transition);
            }
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        [DebuggerStepThrough]
        private void ExecuteAction(CachedDelegate cachedAction, Event e)
        {
            try
            {
                if (cachedAction.Handler is Action<Event> actionWithEvent)
                {
                    actionWithEvent(e);
                }
                else if (cachedAction.Handler is Action action)
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                var innerException = ex;
                while (innerException is TargetInvocationException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is AggregateException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is ExecutionCanceledException ||
                    innerException is TaskSchedulerException)
                {
                    throw;
                }
                else
                {
                    // Reports the unhandled exception.
                    ReportUnhandledException(innerException, cachedAction.MethodInfo.Name);
                }
            }
        }

        /// <summary>
        /// Applies the specified event handler transition.
        /// </summary>
        private void ApplyEventHandlerTransition(Transition transition)
        {
            if (transition.TypeValue != PendingTransition.TypeValue && PendingTransition.TypeValue != Transition.Type.None)
            {
                CheckDanglingTransition();
            }
            else if (transition.TypeValue is Transition.Type.Raise)
            {
                PendingTransition = default;
                var e = transition.Event;
                Runtime.NotifyRaisedEvent(this, e);
                HandleEvent(e);
            }
            else if (transition.TypeValue is Transition.Type.Goto)
            {
                PendingTransition = default;
                var e = new GotoStateEvent(transition.State);
                Runtime.NotifyRaisedEvent(this, e);
                HandleEvent(e);
            }
            else
            {
                PendingTransition = default;
            }
        }

        /// <summary>
        /// Notifies that a Transition was created but not returned to the Monitor.
        /// </summary>
        private void CheckDanglingTransition()
        {
            var transition = PendingTransition;
            PendingTransition = default;

            if (transition.TypeValue != Transition.Type.None)
            {
                var prefix = string.Format("{0} Transition created by {1} in state {2} was not processed",
                    transition.TypeValue, GetType().FullName, CurrentStateName);
                string suffix = null;

                if (transition.State != null && transition.Event != null)
                {
                    suffix = string.Format(", state {0}, event {1}.", transition.State, transition.Event);
                }
                else if (transition.State != null)
                {
                    suffix = string.Format(", state {0}.", transition.State);
                }
                else if (transition.Event != null)
                {
                    suffix = string.Format(", event {0}.", transition.Event);
                }

                Assert(false, prefix + suffix);
            }
        }

        /// <summary>
        /// Performs a goto transition to the given state.
        /// </summary>
        private void GotoState(Type s, string onExitActionName, Event e)
        {
            // The monitor performs the on exit statements of the current state.
            ExecuteCurrentStateOnExit(onExitActionName, e);

            var nextState = StateMap[GetType()].First(val => val.GetType().Equals(s));
            ConfigureStateTransitions(nextState);

            // The monitor transitions to the new state.
            ActiveState = nextState;

            if (nextState.IsCold)
            {
                LivenessTemperature = 0;
            }

            // The monitor performs the on entry statements of the new state.
            ExecuteCurrentStateOnEntry(e);
        }

        /// <summary>
        /// Checks if the state can handle the given event type. An event
        /// can be handled if it is deferred, or leads to a transition or
        /// action binding.
        /// </summary>
        private bool CanHandleEvent(Type e)
        {
            if (EventHandlers.ContainsKey(e) ||
                EventHandlers.ContainsKey(typeof(WildCardEvent)) ||
                e == typeof(GotoStateEvent))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks the liveness temperature of the monitor and report
        /// a potential liveness bug if the temperature passes the
        /// specified threshold. Only works in a liveness monitor.
        /// </summary>
        internal void CheckLivenessTemperature()
        {
            if (ActiveState.IsHot &&
                Runtime.CheckerConfiguration.LivenessTemperatureThreshold > 0)
            {
                LivenessTemperature++;
                if (LivenessTemperature > Runtime.
                        CheckerConfiguration.LivenessTemperatureThreshold)
                {
                    Runtime.NotifyMonitorError(this);
                    Runtime.Assert(false,
                        "{0} detected potential liveness bug in hot state '{1}'.",
                        GetType().FullName, CurrentStateName);
                }
            }
        }

        /// <summary>
        /// Checks the liveness temperature of the monitor and report
        /// a potential liveness bug if the temperature passes the
        /// specified threshold. Only works in a liveness monitor.
        /// </summary>
        internal void CheckLivenessTemperature(int livenessTemperature)
        {
            if (livenessTemperature > Runtime.CheckerConfiguration.LivenessTemperatureThreshold)
            {
                Runtime.Assert(
                    livenessTemperature <= Runtime.CheckerConfiguration.LivenessTemperatureThreshold,
                    $"{GetType().FullName} detected infinite execution that violates a liveness property.");
            }
        }

        /// <summary>
        /// Returns true if the monitor is in a hot state.
        /// </summary>
        internal bool IsInHotState() => ActiveState?.IsHot ?? false;

        /// <summary>
        /// Returns true if the monitor is in a hot state. Also outputs
        /// the name of the current state.
        /// </summary>
        internal bool IsInHotState(out string stateName)
        {
            stateName = CurrentStateName;
            return ActiveState.IsHot;
        }

        /// <summary>
        /// Returns true if the monitor is in a cold state.
        /// </summary>
        internal bool IsInColdState() => ActiveState?.IsCold ?? false;

        /// <summary>
        /// Returns true if the monitor is in a cold state. Also outputs
        /// the name of the current state.
        /// </summary>
        internal bool IsInColdState(out string stateName)
        {
            stateName = CurrentStateName;
            return ActiveState.IsCold;
        }

        /// <summary>
        /// Returns a nullable boolean indicating liveness temperature: true for hot, false for cold, else null.
        /// </summary>
        internal bool? GetHotState()
        {
            return IsInHotState() ? true :
                IsInColdState() ? (bool?)false :
                null;
        }

        /// <summary>
        /// Returns the hashed state of this monitor.
        /// </summary>
        internal int GetHashedState()
        {
            unchecked
            {
                var hash = 19;

                hash = (hash * 31) + GetType().GetHashCode();
                hash = (hash * 31) + CurrentState.GetHashCode();

                if (HashedState != 0)
                {
                    // Adds the user-defined hashed state.
                    hash = (hash * 31) + HashedState;
                }

                return hash;
            }
        }

        /// <summary>
        /// Returns a string that represents the current monitor.
        /// </summary>
        public override string ToString() => GetType().FullName;

        /// <summary>
        /// Transitions to the start state, and executes the
        /// entry action, if there is any.
        /// </summary>
        internal void GotoStartState()
        {
            ExecuteCurrentStateOnEntry(DefaultEvent.Instance);
        }

        /// <summary>
        /// Initializes information about the states of the monitor.
        /// </summary>
        internal void InitializeStateInformation()
        {
            var monitorType = GetType();

            // If this type has not already been setup in the MonitorActionMap, then we need to try and grab the ActionCacheLock
            // for this type.  First make sure we have one and only one lockable object for this type.
            var syncObject = ActionCacheLocks.GetOrAdd(monitorType, _ => new object());

            // Locking this syncObject ensures only one thread enters the initialization code to update
            // the ActionCache for this specific Actor type.
            lock (syncObject)
            {
                if (MonitorActionMap.ContainsKey(monitorType))
                {
                    // Note: even if we won the GetOrAdd, there is a tiny window of opportunity for another thread
                    // to slip in and lock the syncObject before us, so we have to check the ActionCache again
                    // here just in case.
                }
                else
                {
                    // Caches the actions declarations for this monitor type.
                    if (MonitorActionMap.TryAdd(monitorType, new Dictionary<string, MethodInfo>()))
                    {
                        // Caches the available state types for this monitor type.
                        if (StateTypeMap.TryAdd(monitorType, new HashSet<Type>()))
                        {
                            var baseType = monitorType;
                            while (baseType != typeof(Monitor))
                            {
                                foreach (var s in baseType.GetNestedTypes(BindingFlags.Instance |
                                                                          BindingFlags.NonPublic | BindingFlags.Public |
                                                                          BindingFlags.DeclaredOnly))
                                {
                                    ExtractStateTypes(s);
                                }

                                baseType = baseType.BaseType;
                            }
                        }

                        // Caches the available state instances for this monitor type.
                        if (StateMap.TryAdd(monitorType, new HashSet<State>()))
                        {
                            foreach (var type in StateTypeMap[monitorType])
                            {
                                var stateType = type;
                                if (type.IsAbstract)
                                {
                                    continue;
                                }

                                if (type.IsGenericType)
                                {
                                    // If the state type is generic (only possible if inherited by a
                                    // generic monitor declaration), then iterate through the base
                                    // monitor classes to identify the runtime generic type, and use
                                    // it to instantiate the runtime state type. This type can be
                                    // then used to create the state constructor.
                                    var declaringType = GetType();
                                    while (!declaringType.IsGenericType ||
                                           !type.DeclaringType.FullName.Equals(declaringType.FullName.Substring(
                                               0, declaringType.FullName.IndexOf('['))))
                                    {
                                        declaringType = declaringType.BaseType;
                                    }

                                    if (declaringType.IsGenericType)
                                    {
                                        stateType = type.MakeGenericType(declaringType.GetGenericArguments());
                                    }
                                }

                                var constructor = stateType.GetConstructor(Type.EmptyTypes);
                                var lambda = Expression.Lambda<Func<State>>(Expression.New(constructor)).Compile();
                                var state = lambda();

                                state.InitializeState();

                                Assert(
                                    (state.IsCold && !state.IsHot) ||
                                    (!state.IsCold && state.IsHot) ||
                                    (!state.IsCold && !state.IsHot),
                                    "State '{0}' of {1} cannot be both cold and hot.", type.FullName, GetType().FullName);

                                StateMap[monitorType].Add(state);
                            }
                        }

                        foreach (var state in StateMap[monitorType])
                        {
                            if (state.EntryAction != null &&
                                !MonitorActionMap[monitorType].ContainsKey(state.EntryAction))
                            {
                                MonitorActionMap[monitorType].Add(
                                    state.EntryAction,
                                    GetActionWithName(state.EntryAction));
                            }

                            if (state.ExitAction != null &&
                                !MonitorActionMap[monitorType].ContainsKey(state.ExitAction))
                            {
                                MonitorActionMap[monitorType].Add(
                                    state.ExitAction,
                                    GetActionWithName(state.ExitAction));
                            }

                            foreach (var handler in state.EventHandlers.Values)
                            {
                                if (handler is ActionEventHandlerDeclaration action)
                                {
                                    if (!MonitorActionMap[monitorType].ContainsKey(action.Name))
                                    {
                                        MonitorActionMap[monitorType].Add(
                                            action.Name,
                                            GetActionWithName(action.Name));
                                    }
                                }
                                else if (handler is GotoStateTransition transition)
                                {
                                    if (transition.Lambda != null &&
                                        !MonitorActionMap[monitorType].ContainsKey(transition.Lambda))
                                    {
                                        MonitorActionMap[monitorType].Add(
                                            transition.Lambda,
                                            GetActionWithName(transition.Lambda));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Populates the map of actions for this monitor instance.
            foreach (var kvp in MonitorActionMap[monitorType])
            {
                ActionMap.Add(kvp.Key, new CachedDelegate(kvp.Value, this));
            }

            var initialStates = StateMap[monitorType].Where(state => state.IsStart).ToList();
            Assert(initialStates.Count != 0, "{0} must declare a start state.", GetType().FullName);
            Assert(initialStates.Count == 1, "{0} can not declare more than one start states.", GetType().FullName);

            ConfigureStateTransitions(initialStates.Single());
            ActiveState = initialStates.Single();

            AssertStateValidity();
        }

        /// <summary>
        /// Processes a type, looking for monitor states.
        /// </summary>
        private void ExtractStateTypes(Type type)
        {
            var stack = new Stack<Type>();
            stack.Push(type);

            while (stack.Count > 0)
            {
                var nextType = stack.Pop();

                if (nextType.IsClass && nextType.IsSubclassOf(typeof(State)))
                {
                    StateTypeMap[GetType()].Add(nextType);
                }
                else if (nextType.IsClass && nextType.IsSubclassOf(typeof(StateGroup)))
                {
                    // Adds the contents of the group of states to the stack.
                    foreach (var t in nextType.GetNestedTypes(BindingFlags.Instance |
                                                              BindingFlags.NonPublic | BindingFlags.Public |
                                                              BindingFlags.DeclaredOnly))
                    {
                        Assert(t.IsSubclassOf(typeof(StateGroup)) || t.IsSubclassOf(typeof(State)),
                            "'{0}' is neither a group of states nor a state.", t.Name);
                        stack.Push(t);
                    }
                }
            }
        }

        /// <summary>
        /// Configures the state transitions of the monitor.
        /// </summary>
        private void ConfigureStateTransitions(State state)
        {
            EventHandlers = state.EventHandlers;
            IgnoredEvents = state.IgnoredEvents;
        }

        /// <summary>
        /// Returns the action with the specified name.
        /// </summary>
        private MethodInfo GetActionWithName(string actionName)
        {
            MethodInfo action;
            var monitorType = GetType();

            do
            {
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.FlattenHierarchy;
                action = monitorType.GetMethod(actionName, bindingFlags, Type.DefaultBinder, SingleEventTypeArray, null);
                if (action is null)
                {
                    action = monitorType.GetMethod(actionName, bindingFlags, Type.DefaultBinder, Array.Empty<Type>(), null);
                }

                monitorType = monitorType.BaseType;
            }
            while (action is null && monitorType != typeof(Monitor));

            Assert(action != null, "Cannot detect action declaration '{0}' in {1}.",
                actionName, GetType().FullName);

            var parameters = action.GetParameters();
            Assert(parameters.Length is 0 ||
                   (parameters.Length is 1 && parameters[0].ParameterType == typeof(Event)),
                "Action '{0}' in {1} must either accept no parameters or a single parameter of type 'Event'.",
                action.Name, GetType().FullName);

            Assert(action.ReturnType == typeof(void) || action.ReturnType == typeof(Transition),
                "Action '{0}' in {1} must have 'void' or 'Transition' return type.",
                action.Name, GetType().FullName);

            return action;
        }

        /// <summary>
        /// Check monitor for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            Assert(StateTypeMap[GetType()].Count > 0, "{0} must have one or more states.", GetType().FullName);
            Assert(ActiveState != null, "{0} must not have a null current state.", GetType().FullName);
        }

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        private void ReportUnhandledException(Exception ex, string actionName)
        {
            var state = CurrentState is null ? "<unknown>" : CurrentStateName;
            Runtime.WrapAndThrowException(ex, "{0} (state '{1}', action '{2}')",
                GetType().FullName, state, actionName);
        }

        /// <summary>
        /// Returns the set of all states in the monitor (for code coverage).
        /// </summary>
        internal HashSet<string> GetAllStates()
        {
            Assert(StateMap.ContainsKey(GetType()), "{0} has not populated its states yet.", GetType().FullName);

            var allStates = new HashSet<string>();
            foreach (var state in StateMap[GetType()])
            {
                allStates.Add(NameResolver.GetQualifiedStateName(state.GetType()));
            }

            return allStates;
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the monitor (for code coverage).
        /// </summary>
        internal HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            Assert(StateMap.ContainsKey(GetType()), "{0} has not populated its states yet.", GetType().FullName);

            var pairs = new HashSet<Tuple<string, string>>();
            foreach (var state in StateMap[GetType()])
            {
                foreach (var binding in state.EventHandlers)
                {
                    pairs.Add(Tuple.Create(NameResolver.GetQualifiedStateName(state.GetType()), binding.Key.FullName));
                }
            }

            return pairs;
        }

        /// <summary>
        /// Defines the <see cref="Monitor"/> transition that is the
        /// result of executing an event handler.  Transitions are created by using
        /// <see cref="Monitor.RaiseGotoStateEvent{T}"/>, or <see cref="Monitor.RaiseEvent"/>.
        /// The Transition is processed by the Coyote runtime when
        /// an event handling method returns a Transition object.
        /// This means such a method can only do one such Transition per method call.
        /// If the method wants to do a conditional transition it can return
        /// Transition.None to indicate no transition is to be performed.
        /// </summary>
        internal readonly struct Transition
        {
            /// <summary>
            /// The type of the transition.
            /// </summary>
            public readonly Type TypeValue;

            /// <summary>
            /// The target state of the transition, if there is one.
            /// </summary>
            internal readonly System.Type State;

            /// <summary>
            /// The event participating in the transition, if there is one.
            /// </summary>
            internal readonly Event Event;

            /// <summary>
            /// This special transition represents a transition that does not change the current <see cref="Monitor.State"/>.
            /// </summary>
            public static Transition None = default;

            /// <summary>
            /// Initializes a new instance of the <see cref="Transition"/> struct.
            /// </summary>
            /// <param name="type">The type of the transition.</param>
            /// <param name="state">The target state of the transition, if there is one.</param>
            /// <param name="e">The event participating in the transition, if there is one.</param>
            internal Transition(Type type, System.Type state, Event e)
            {
                TypeValue = type;
                State = state;
                Event = e;
            }

            /// <summary>
            /// Defines the type of a <see cref="Monitor"/> transition.
            /// </summary>
            public enum Type
            {
                /// <summary>
                /// A transition that does not change the <see cref="Monitor.State"/>.
                /// This is the value used by <see cref="Transition.None"/>.
                /// </summary>
                None = 0,

                /// <summary>
                /// A transition created by <see cref="Monitor.RaiseEvent(Event)"/> that raises an <see cref="Event"/> bypassing
                /// the <see cref="Monitor.State"/> inbox.
                /// </summary>
                Raise,

                /// <summary>
                /// A transition created by <see cref="Monitor.RaiseGotoStateEvent{S}"/> from the current <see cref="Monitor.State"/>
                /// to the specified <see cref="Monitor.State"/>.
                /// </summary>
                Goto
            }
        }

        /// <summary>
        /// Abstract class representing a state.
        /// </summary>
        public abstract class State
        {
            /// <summary>
            /// The entry action of the state.
            /// </summary>
            internal string EntryAction { get; private set; }

            /// <summary>
            /// The exit action of the state.
            /// </summary>
            internal string ExitAction { get; private set; }

            /// <summary>
            /// Dictionary containing all the event handlers.
            /// </summary>
            internal Dictionary<Type, EventHandlerDeclaration> EventHandlers;

            /// <summary>
            /// Set of ignored event types.
            /// </summary>
            internal HashSet<Type> IgnoredEvents;

            /// <summary>
            /// True if this is the start state.
            /// </summary>
            internal bool IsStart { get; private set; }

            /// <summary>
            /// Returns true if this is a hot state.
            /// </summary>
            internal bool IsHot { get; private set; }

            /// <summary>
            /// Returns true if this is a cold state.
            /// </summary>
            internal bool IsCold { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="State"/> class.
            /// </summary>
            protected State()
            {
            }

            /// <summary>
            /// Initializes the state.
            /// </summary>
            internal void InitializeState()
            {
                IsStart = false;
                IsHot = false;
                IsCold = false;

                EventHandlers = new Dictionary<Type, EventHandlerDeclaration>();

                IgnoredEvents = new HashSet<Type>();

                if (GetType().GetCustomAttribute(typeof(OnEntryAttribute), true) is OnEntryAttribute entryAttribute)
                {
                    EntryAction = entryAttribute.Action;
                }

                if (GetType().GetCustomAttribute(typeof(OnExitAttribute), true) is OnExitAttribute exitAttribute)
                {
                    ExitAction = exitAttribute.Action;
                }

                if (GetType().IsDefined(typeof(StartAttribute), false))
                {
                    IsStart = true;
                }

                if (GetType().IsDefined(typeof(HotAttribute), false))
                {
                    IsHot = true;
                }

                if (GetType().IsDefined(typeof(ColdAttribute), false))
                {
                    IsCold = true;
                }

                // Events with already declared handlers.
                var handledEvents = new HashSet<Type>();

                // Install event handlers.
                InstallGotoTransitions(handledEvents);
                InstallActionHandlers(handledEvents);
                InstallIgnoreHandlers(handledEvents);
            }

            /// <summary>
            /// Declares goto event handlers, if there are any.
            /// </summary>
            private void InstallGotoTransitions(HashSet<Type> handledEvents)
            {
                var gotoAttributes = GetType().GetCustomAttributes(typeof(OnEventGotoStateAttribute), false)
                    as OnEventGotoStateAttribute[];

                foreach (var attr in gotoAttributes)
                {
                    CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                    if (attr.Action is null)
                    {
                        EventHandlers.Add(attr.Event, new GotoStateTransition(attr.State));
                    }
                    else
                    {
                        EventHandlers.Add(attr.Event, new GotoStateTransition(attr.State, attr.Action));
                    }

                    handledEvents.Add(attr.Event);
                }

                InheritGotoTransitions(GetType().BaseType, handledEvents);
            }

            /// <summary>
            /// Inherits goto event handlers from a base state, if there is one.
            /// </summary>
            private void InheritGotoTransitions(Type baseState, HashSet<Type> handledEvents)
            {
                if (!baseState.IsSubclassOf(typeof(State)))
                {
                    return;
                }

                var gotoAttributesInherited = baseState.GetCustomAttributes(typeof(OnEventGotoStateAttribute), false)
                    as OnEventGotoStateAttribute[];

                var gotoTransitionsInherited = new Dictionary<Type, GotoStateTransition>();
                foreach (var attr in gotoAttributesInherited)
                {
                    if (EventHandlers.ContainsKey(attr.Event))
                    {
                        continue;
                    }

                    CheckEventHandlerAlreadyInherited(attr.Event, baseState, handledEvents);

                    if (attr.Action is null)
                    {
                        gotoTransitionsInherited.Add(attr.Event, new GotoStateTransition(attr.State));
                    }
                    else
                    {
                        gotoTransitionsInherited.Add(attr.Event, new GotoStateTransition(attr.State, attr.Action));
                    }

                    handledEvents.Add(attr.Event);
                }

                foreach (var kvp in gotoTransitionsInherited)
                {
                    EventHandlers.Add(kvp.Key, kvp.Value);
                }

                InheritGotoTransitions(baseState.BaseType, handledEvents);
            }

            /// <summary>
            /// Declares action event handlers, if there are any.
            /// </summary>
            private void InstallActionHandlers(HashSet<Type> handledEvents)
            {
                var doAttributes = GetType().GetCustomAttributes(typeof(OnEventDoActionAttribute), false)
                    as OnEventDoActionAttribute[];

                foreach (var attr in doAttributes)
                {
                    CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                    EventHandlers.Add(attr.Event, new ActionEventHandlerDeclaration(attr.Action));
                    handledEvents.Add(attr.Event);
                }

                InheritActionHandlers(GetType().BaseType, handledEvents);
            }

            /// <summary>
            /// Inherits action event handlers from a base state, if there is one.
            /// </summary>
            private void InheritActionHandlers(Type baseState, HashSet<Type> handledEvents)
            {
                if (!baseState.IsSubclassOf(typeof(State)))
                {
                    return;
                }

                var doAttributesInherited = baseState.GetCustomAttributes(typeof(OnEventDoActionAttribute), false)
                    as OnEventDoActionAttribute[];

                var actionBindingsInherited = new Dictionary<Type, ActionEventHandlerDeclaration>();
                foreach (var attr in doAttributesInherited)
                {
                    if (EventHandlers.ContainsKey(attr.Event))
                    {
                        continue;
                    }

                    CheckEventHandlerAlreadyInherited(attr.Event, baseState, handledEvents);

                    actionBindingsInherited.Add(attr.Event, new ActionEventHandlerDeclaration(attr.Action));
                    handledEvents.Add(attr.Event);
                }

                foreach (var kvp in actionBindingsInherited)
                {
                    EventHandlers.Add(kvp.Key, kvp.Value);
                }

                InheritActionHandlers(baseState.BaseType, handledEvents);
            }

            /// <summary>
            /// Declares ignore event handlers, if there are any.
            /// </summary>
            private void InstallIgnoreHandlers(HashSet<Type> handledEvents)
            {
                if (GetType().GetCustomAttribute(typeof(IgnoreEventsAttribute), false) is IgnoreEventsAttribute ignoreEventsAttribute)
                {
                    foreach (var e in ignoreEventsAttribute.Events)
                    {
                        CheckEventHandlerAlreadyDeclared(e, handledEvents);
                    }

                    IgnoredEvents.UnionWith(ignoreEventsAttribute.Events);
                    handledEvents.UnionWith(ignoreEventsAttribute.Events);
                }

                InheritIgnoreHandlers(GetType().BaseType, handledEvents);
            }

            /// <summary>
            /// Inherits ignore event handlers from a base state, if there is one.
            /// </summary>
            private void InheritIgnoreHandlers(Type baseState, HashSet<Type> handledEvents)
            {
                if (!baseState.IsSubclassOf(typeof(State)))
                {
                    return;
                }

                if (baseState.GetCustomAttribute(typeof(IgnoreEventsAttribute), false) is IgnoreEventsAttribute ignoreEventsAttribute)
                {
                    foreach (var e in ignoreEventsAttribute.Events)
                    {
                        if (IgnoredEvents.Contains(e))
                        {
                            continue;
                        }

                        CheckEventHandlerAlreadyInherited(e, baseState, handledEvents);
                    }

                    IgnoredEvents.UnionWith(ignoreEventsAttribute.Events);
                    handledEvents.UnionWith(ignoreEventsAttribute.Events);
                }

                InheritIgnoreHandlers(baseState.BaseType, handledEvents);
            }

            /// <summary>
            /// Checks if an event handler has been already declared.
            /// </summary>
            private static void CheckEventHandlerAlreadyDeclared(Type e, HashSet<Type> handledEvents)
            {
                if (handledEvents.Contains(e))
                {
                    throw new InvalidOperationException($"declared multiple handlers for event '{e}'");
                }
            }

            /// <summary>
            /// Checks if an event handler has been already inherited.
            /// </summary>
            private static void CheckEventHandlerAlreadyInherited(Type e, Type baseState, HashSet<Type> handledEvents)
            {
                if (handledEvents.Contains(e))
                {
                    throw new InvalidOperationException($"inherited multiple handlers for event '{e}' from state '{baseState}'");
                }
            }

            /// <summary>
            /// Attribute for declaring that a state of a monitor
            /// is the start one.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class StartAttribute : Attribute
            {
            }

            /// <summary>
            /// Attribute for declaring what action to perform
            /// when entering a monitor state.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class OnEntryAttribute : Attribute
            {
                /// <summary>
                /// Action name.
                /// </summary>
                internal readonly string Action;

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEntryAttribute"/> class.
                /// </summary>
                /// <param name="actionName">Action name</param>
                public OnEntryAttribute(string actionName)
                {
                    Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring what action to perform
            /// when exiting a monitor state.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class OnExitAttribute : Attribute
            {
                /// <summary>
                /// Action name.
                /// </summary>
                internal string Action;

                /// <summary>
                /// Initializes a new instance of the <see cref="OnExitAttribute"/> class.
                /// </summary>
                /// <param name="actionName">Action name</param>
                public OnExitAttribute(string actionName)
                {
                    Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring which state a monitor should transition to
            /// when it receives an event in a given state.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            protected sealed class OnEventGotoStateAttribute : Attribute
            {
                /// <summary>
                /// Event type.
                /// </summary>
                internal readonly Type Event;

                /// <summary>
                /// State type.
                /// </summary>
                internal readonly Type State;

                /// <summary>
                /// Action name.
                /// </summary>
                internal readonly string Action;

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventGotoStateAttribute"/> class.
                /// </summary>
                /// <param name="eventType">Event type</param>
                /// <param name="stateType">State type</param>
                public OnEventGotoStateAttribute(Type eventType, Type stateType)
                {
                    Event = eventType;
                    State = stateType;
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventGotoStateAttribute"/> class.
                /// </summary>
                /// <param name="eventType">Event type</param>
                /// <param name="stateType">State type</param>
                /// <param name="actionName">Name of action to perform on exit</param>
                public OnEventGotoStateAttribute(Type eventType, Type stateType, string actionName)
                {
                    Event = eventType;
                    State = stateType;
                    Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring what action a monitor should perform
            /// when it receives an event in a given state.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            protected sealed class OnEventDoActionAttribute : Attribute
            {
                /// <summary>
                /// Event type.
                /// </summary>
                internal Type Event;

                /// <summary>
                /// Action name.
                /// </summary>
                internal string Action;

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventDoActionAttribute"/> class.
                /// </summary>
                /// <param name="eventType">Event type</param>
                /// <param name="actionName">Action name</param>
                public OnEventDoActionAttribute(Type eventType, string actionName)
                {
                    Event = eventType;
                    Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring what events should be ignored in
            /// a monitor state.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class IgnoreEventsAttribute : Attribute
            {
                /// <summary>
                /// Event types.
                /// </summary>
                internal Type[] Events;

                /// <summary>
                /// Initializes a new instance of the <see cref="IgnoreEventsAttribute"/> class.
                /// </summary>
                /// <param name="eventTypes">Event types</param>
                public IgnoreEventsAttribute(params Type[] eventTypes)
                {
                    Events = eventTypes;
                }
            }

            /// <summary>
            /// Attribute for declaring a cold monitor state. A monitor that
            /// is in a cold state satisfies a liveness property.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class ColdAttribute : Attribute
            {
            }

            /// <summary>
            /// Attribute for declaring a hot monitor state. A monitor that
            /// is in a hot state violates a liveness property.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class HotAttribute : Attribute
            {
            }
        }

        /// <summary>
        /// Abstract class used for representing a group of related states.
        /// </summary>
        public abstract class StateGroup
        {
        }
    }
}