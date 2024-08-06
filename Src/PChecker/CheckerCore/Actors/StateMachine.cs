// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using PChecker.Actors.Events;
using PChecker.Actors.Exceptions;
using PChecker.Actors.Handlers;
using PChecker.Actors.StateTransitions;
using PChecker.Exceptions;

namespace PChecker.Actors
{
    /// <summary>
    /// Type that implements a state machine actor. Inherit from this class to declare
    /// a custom actor with states, state transitions and event handlers.
    /// </summary>
    public abstract class StateMachine : Actor
    {
        /// <summary>
        /// Cache of state machine types to a map of action names to action declarations.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> ActionCache =
            new ConcurrentDictionary<Type, Dictionary<string, MethodInfo>>();

        /// <summary>
        /// A set of lockable objects used to protect static initialization of the ActionCache while
        /// also enabling multithreaded initialization of different StateMachine types.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> ActionCacheLocks =
            new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Cache of state machine types to a set of all possible states types.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<Type>> StateTypeCache =
            new ConcurrentDictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Cache of state machine types to a set of all available state instances.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<State>> StateInstanceCache =
            new ConcurrentDictionary<Type, HashSet<State>>();
        

        /// <summary>
        /// A map from event type to EventHandlerDeclaration for those EventHandlerDeclarations that
        /// are not inheritable on the state stack.
        /// </summary>
        private Dictionary<Type, EventHandlerDeclaration> EventHandlerMap;

        /// <summary>
        /// This is just so we don't have to allocate an empty map more than once.
        /// </summary>
        private static readonly Dictionary<Type, EventHandlerDeclaration> EmptyEventHandlerMap = new Dictionary<Type, EventHandlerDeclaration>();

        /// <summary>
        /// Map from action names to cached action delegates for all states in this state machine.
        /// </summary>
        private readonly Dictionary<string, CachedDelegate> StateMachineActionMap;

        /// <summary>
        /// Newly created Transition that hasn't been returned from InvokeActionAsync yet.
        /// </summary>
        private Transition PendingTransition;

        /// <summary>
        /// Gets the <see cref="Type"/> of the current state.
        /// </summary>
        protected internal State CurrentState { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachine"/> class.
        /// </summary>
        protected StateMachine()
            : base()
        {
            EventHandlerMap = EmptyEventHandlerMap;
            StateMachineActionMap = new Dictionary<string, CachedDelegate>();
        }

        /// <summary>
        /// Initializes the actor with the specified optional event.
        /// </summary>
        /// <param name="initialEvent">Optional event used for initialization.</param>
        internal override async Task InitializeAsync(Event initialEvent)
        {
            // Invoke the custom initializer, if there is one.
            await InvokeUserCallbackAsync(UserCallbackType.OnInitialize, initialEvent);

            // Execute the entry action of the start state, if there is one.
            await ExecuteCurrentStateOnEntryAsync(initialEvent);
            if (CurrentStatus is Status.Halting)
            {
                await HaltAsync(initialEvent);
            }
        }

        /// <summary>
        /// Raises the specified <see cref="Event"/> at the end of the current action.
        /// </summary>
        /// <remarks>
        /// This event is not handled until the action that calls this method returns control back
        /// to the runtime.  It is handled before any other events are dequeued from the inbox.
        /// Only one of the following can be called per action:
        /// <see cref="RaiseEvent"/>, <see cref="RaiseGotoStateEvent{T}"/> or
        /// <see cref="RaiseHaltEvent"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        /// <param name="e">The event to raise.</param>
        protected void RaiseEvent(Event e)
        {
            Assert(CurrentStatus is Status.Active, "{0} invoked RaiseEvent while halting.", Id);
            Assert(e != null, "{0} is raising a null event.", Id);
            CheckDanglingTransition();
            PendingTransition = new Transition(Transition.Type.RaiseEvent, default, e);
        }

        /// <summary>
        /// Raise a special event that performs a goto state operation at the end of the current action.
        /// </summary>
        /// <remarks>
        /// Goto state pops the current <see cref="State"/> and pushes the specified <see cref="State"/> on the active state stack.
        /// This is shorthand for the following code:
        /// <code>
        /// class Event E { }
        /// [OnEventGotoState(typeof(E), typeof(S))]
        /// this.RaiseEvent(new E());
        /// </code>
        /// This event is not handled until the action that calls this method returns control back
        /// to the runtime.  It is handled before any other events are dequeued from the inbox.
        /// Only one of the following can be called per action:
        /// <see cref="RaiseEvent"/>, <see cref="RaiseGotoStateEvent{T}"/> or
        /// <see cref="RaiseHaltEvent"/>.
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
        /// This is shorthand for the following code:
        /// <code>
        /// class Event E { }
        /// [OnEventGotoState(typeof(E), typeof(S))]
        /// this.RaiseEvent(new E());
        /// </code>
        /// This event is not handled until the action that calls this method returns control back
        /// to the runtime.  It is handled before any other events are dequeued from the inbox.
        /// Only one of the following can be called per action:
        /// <see cref="RaiseEvent"/>, <see cref="RaiseGotoStateEvent{T}"/> <see cref="RaiseHaltEvent"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        /// <param name="state">Type of the state.</param>
        protected void RaiseGotoStateEvent(Type state)
        {
            Assert(CurrentStatus is Status.Active, "{0} invoked GotoState while halting.", Id);
            Assert(StateTypeCache[GetType()].Any(val => val.DeclaringType.Equals(state.DeclaringType) && val.Name.Equals(state.Name)),
                "{0} is trying to transition to non-existing state '{1}'.", Id, state.Name);
            CheckDanglingTransition();
            PendingTransition = new Transition(Transition.Type.GotoState, state, default);
        }

        /// <summary>
        /// Raises a <see cref='HaltEvent'/> to halt the actor at the end of the current action.
        /// </summary>
        /// <remarks>
        /// This event is not handled until the action that calls this method returns control back
        /// to the runtime.  It is handled before any other events are dequeued from the inbox.
        ///
        /// Only one of the following can be called per action:
        /// <see cref="RaiseEvent"/>, <see cref="RaiseGotoStateEvent{T}"/> and <see cref="RaiseHaltEvent"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        protected override void RaiseHaltEvent()
        {
            base.RaiseHaltEvent();
            CheckDanglingTransition();
            PendingTransition = new Transition(Transition.Type.Halt, null, default);
        }

        /// <summary>
        /// Asynchronous callback that is invoked when the actor finishes handling a dequeued
        /// event, unless the handler of the dequeued event raised an event or caused the actor
        /// to halt (either normally or due to an exception). Unless this callback raises an
        /// event, the actor will either become idle or dequeue the next event from its inbox.
        /// </summary>
        /// <param name="e">The event that was handled.</param>
        protected override Task OnEventHandledAsync(Event e) => Task.CompletedTask;

        /// <summary>
        /// Handles the specified <see cref="Event"/>.
        /// </summary>
        private protected override async Task HandleEventAsync(Event e)
        {
            var currentState = CurrentState;

            while (true)
            {
                if (CurrentState is null)
                {
                    // If the stack of states is empty then halt or fail the state machine.
                    if (e is HaltEvent)
                    {
                        // If it is the halt event, then change the actor status to halting.
                        CurrentStatus = Status.Halting;
                        break;
                    }

                    var currentStateName = NameResolver.GetQualifiedStateName(currentState.GetType());
                    await InvokeUserCallbackAsync(UserCallbackType.OnEventUnhandled, e, currentStateName);
                    if (CurrentStatus is Status.Active)
                    {
                        // If the event cannot be handled then report an error, else halt gracefully.
                        var ex = new UnhandledEventException(e, currentStateName, "Unhandled Event");
                        var isHalting = OnUnhandledEventExceptionHandler(ex, e);
                        Assert(isHalting, "{0} received event '{1}' that cannot be handled.",
                            Id, e.GetType().FullName);
                    }

                    break;
                }

                if (e is GotoStateEvent gotoStateEvent)
                {
                    await GotoStateAsync(gotoStateEvent.State, null, e);
                }
                else if (EventHandlerMap.ContainsKey(e.GetType()))
                {
                    await HandleEventAsync(e, currentState, EventHandlerMap[e.GetType()]);
                }
                else
                {
                    if (TryGetHandler(e.GetType(), out EventHandlerDeclaration ehandler))
                    {
                        // Then specific event is more recent than any wild card events.
                        await HandleEventAsync(e, currentState, ehandler);
                    }
                    else if (ActionMap.TryGetValue(e.GetType(), out var handler))
                    {
                        // Allow StateMachine to have class level OnEventDoActions the same way Actor allows.
                        Runtime.NotifyInvokedAction(this, handler.MethodInfo, CurrentStateName, CurrentStateName, e);
                        await InvokeActionAsync(handler, e);
                    }
                    else
                    {
                        // If the current state cannot handle the event.
                        await ExecuteCurrentStateOnExitAsync(null, e);
                        if (CurrentStatus is Status.Active)
                        {
                            Runtime.LogWriter.LogPopStateUnhandledEvent(Id, CurrentStateName, e);
                            EventHandlerMap = EmptyEventHandlerMap;
                            CurrentState = null;
                            CurrentStateName = string.Empty;
                            continue;
                        }
                    }
                }

                break;
            }
        }

        private bool TryGetHandler(Type e, out EventHandlerDeclaration o)
        {
            if (EventHandlerMap.ContainsKey(e))
            {
                o = EventHandlerMap[e];
                return true;
            }

            if (EventHandlerMap.ContainsKey(typeof(WildCardEvent)))
            {
                o = EventHandlerMap[typeof(WildCardEvent)];
                return true;
            }

            o = null;
            return false;
        }

        private async Task HandleEventAsync(Event e, State declaringState, EventHandlerDeclaration eventHandler)
        {
            var handlingStateName = NameResolver.GetQualifiedStateName(declaringState.GetType());
            if (eventHandler is ActionEventHandlerDeclaration actionEventHandler)
            {
                var cachedAction = StateMachineActionMap[actionEventHandler.Name];
                Runtime.NotifyInvokedAction(this, cachedAction.MethodInfo, handlingStateName, CurrentStateName, e);
                await InvokeActionAsync(cachedAction, e);
                await ApplyEventHandlerTransitionAsync(PendingTransition, e);
            }
            else if (eventHandler is GotoStateTransition gotoTransition)
            {
                await GotoStateAsync(gotoTransition.TargetState, gotoTransition.Lambda, e);
            }
            else if (eventHandler is PushStateTransition pushTransition)
            {
                await PushStateAsync(pushTransition.TargetState, e);
            }
        }

        /// <summary>
        /// Executes the on entry action of the current state.
        /// </summary>
        private async Task ExecuteCurrentStateOnEntryAsync(Event e)
        {
            Runtime.NotifyEnteredState(this);

            CachedDelegate entryAction = null;
            if (CurrentState.EntryAction != null)
            {
                entryAction = StateMachineActionMap[CurrentState.EntryAction];
            }

            // Invokes the entry action of the new state, if there is one available.
            if (entryAction != null)
            {
                Runtime.NotifyInvokedOnEntryAction(this, entryAction.MethodInfo, e);
                await InvokeActionAsync(entryAction, e);
                await ApplyEventHandlerTransitionAsync(PendingTransition, e);
            }
        }

        /// <summary>
        /// Executes the on exit action of the current state.
        /// </summary>
        private async Task ExecuteCurrentStateOnExitAsync(string eventHandlerExitActionName, Event e)
        {
            Runtime.NotifyExitedState(this);

            CachedDelegate exitAction = null;
            if (CurrentState.ExitAction != null)
            {
                exitAction = StateMachineActionMap[CurrentState.ExitAction];
            }

            // Invokes the exit action of the current state,
            // if there is one available.
            if (exitAction != null)
            {
                Runtime.NotifyInvokedOnExitAction(this, exitAction.MethodInfo, e);
                await InvokeActionAsync(exitAction, e);
                var transition = PendingTransition;
                Assert(transition.TypeValue is Transition.Type.None ||
                       transition.TypeValue is Transition.Type.Halt,
                    "{0} has performed a '{1}' transition from an OnExit action.",
                    Id, transition.TypeValue);
                await ApplyEventHandlerTransitionAsync(transition, e);
            }

            // Invokes the exit action of the event handler,
            // if there is one available.
            if (eventHandlerExitActionName != null && CurrentStatus is Status.Active)
            {
                var eventHandlerExitAction = StateMachineActionMap[eventHandlerExitActionName];
                Runtime.NotifyInvokedOnExitAction(this, eventHandlerExitAction.MethodInfo, e);
                await InvokeActionAsync(eventHandlerExitAction, e);
                var transition = PendingTransition;
                Assert(transition.TypeValue is Transition.Type.None ||
                       transition.TypeValue is Transition.Type.Halt,
                    "{0} has performed a '{1}' transition from an OnExit action.",
                    Id, transition.TypeValue);
                await ApplyEventHandlerTransitionAsync(transition, e);
            }
        }

        /// <summary>
        /// Applies the specified event handler transition.
        /// </summary>
        private Task ApplyEventHandlerTransitionAsync(Transition transition, Event e)
        {
            if (transition.TypeValue != PendingTransition.TypeValue && PendingTransition.TypeValue != Transition.Type.None)
            {
                CheckDanglingTransition();
            }
            else if (transition.TypeValue is Transition.Type.RaiseEvent)
            {
                PendingTransition = default;
                Inbox.RaiseEvent(transition.Event, OperationGroupId);
            }
            else if (transition.TypeValue is Transition.Type.GotoState)
            {
                PendingTransition = default;
                Inbox.RaiseEvent(new GotoStateEvent(transition.State), OperationGroupId);
            }
            else if (transition.TypeValue is Transition.Type.Halt)
            {
                // If it is the halt transition, then change the actor status to halting.
                PendingTransition = default;
                CurrentStatus = Status.Halting;
            }
            else
            {
                PendingTransition = default;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Notifies that a Transition was created but not returned to the StateMachine.
        /// </summary>
        private void CheckDanglingTransition()
        {
            var transition = PendingTransition;
            PendingTransition = default;

            if (transition.TypeValue != Transition.Type.None)
            {
                var prefix = string.Format("{0} transition created by {1} in state {2} was not processed",
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
        /// Performs a goto transition to the specified state.
        /// </summary>
        private async Task GotoStateAsync(Type s, string onExitActionName, Event e)
        {
            Runtime.LogWriter.LogGotoState(Id, CurrentStateName,
                $"{s.DeclaringType}.{NameResolver.GetStateNameForLogging(s)}");

            // The state machine performs the on exit action of the current state.
            await ExecuteCurrentStateOnExitAsync(onExitActionName, e);
            if (CurrentStatus is Status.Active)
            {
                // The state machine transitions to the new state.
                var nextState = StateInstanceCache[GetType()].First(val => val.GetType().Equals(s));
                DoStateTransition(nextState);

                // The state machine performs the on entry action of the new state.
                await ExecuteCurrentStateOnEntryAsync(e);
            }
        }

        /// <summary>
        /// Performs a push transition to the specified state.
        /// </summary>
        private async Task PushStateAsync(Type s, Event e)
        {

            var nextState = StateInstanceCache[GetType()].First(val => val.GetType().Equals(s));
            DoStateTransition(nextState);

            // The state machine performs the on entry statements of the new state.
            await ExecuteCurrentStateOnEntryAsync(e);
        }

        /// <summary>
        /// Configures the state transitions of the state machine when a state is pushed into the stack.
        /// </summary>
        private void DoStateTransition(State state)
        {
            EventHandlerMap = state.EventHandlers;  // non-inheritable handlers.
            CurrentState = state;
            CurrentStateName = NameResolver.GetQualifiedStateName(CurrentState.GetType());
        }

        

        /// <summary>
        /// Checks if the specified event is ignored in the current state.
        /// </summary>
        internal bool IsEventIgnoredInCurrentState(Event e)
        {
            var eventType = e.GetType();

            // If a non-inheritable transition is defined, then the event is not ignored
            // because the non-inheritable operation takes precedent.
            if (EventHandlerMap.ContainsKey(eventType))
            {
                return EventHandlerMap[eventType] is IgnoreEventHandlerDeclaration;
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified event is deferred in the current state.
        /// </summary>
        internal bool IsEventDeferredInCurrentState(Event e)
        {
            var eventType = e.GetType();

            // If a non-inheritable transition is defined, then the event is not deferred.
            if (EventHandlerMap.ContainsKey(eventType))
            {
                return EventHandlerMap[eventType] is DeferEventHandlerDeclaration;
            }
            return false;
        }

        /// <summary>
        /// Checks if a default handler is installed in current state.
        /// </summary>
        internal bool IsDefaultHandlerInstalledInCurrentState() =>
            EventHandlerMap.ContainsKey(typeof(DefaultEvent));

        /// <summary>
        /// Returns the hashed state of this state machine.
        /// </summary>
        internal override int GetHashedState()
        {
            unchecked
            {
                var hash = 19;
                hash = (hash * 31) + GetType().GetHashCode();
                hash = (hash * 31) + Id.Value.GetHashCode();
                hash = (hash * 31) + IsHalted.GetHashCode();

                hash = (hash * 31) + Manager.GetCachedState();
                
                hash = (hash * 31) + Inbox.GetCachedState();

                return hash;
            }
        }

        /// <summary>
        /// Extracts user declarations and setups the event handlers and state transitions.
        /// </summary>
        internal override void SetupEventHandlers()
        {
            base.SetupEventHandlers();
            var stateMachineType = GetType();

            // If this type has not already been setup in the ActionCache, then we need to try and grab the ActionCacheLock
            // for this type.  First make sure we have one and only one lockable object for this type.
            var syncObject = ActionCacheLocks.GetOrAdd(stateMachineType, _ => new object());

            // Locking this syncObject ensures only one thread enters the initialization code to update
            // the ActionCache for this specific Actor type.
            lock (syncObject)
            {
                if (ActionCache.ContainsKey(stateMachineType))
                {
                    // Note: even if we won the GetOrAdd, there is a tiny window of opportunity for another thread
                    // to slip in and lock the syncObject before us, so we have to check the ActionCache again
                    // here just in case.
                }
                else
                {
                    // Caches the available state types for this state machine type.
                    if (StateTypeCache.TryAdd(stateMachineType, new HashSet<Type>()))
                    {
                        var baseType = stateMachineType;
                        while (baseType != typeof(StateMachine))
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

                    // Caches the available state instances for this state machine type.
                    if (StateInstanceCache.TryAdd(stateMachineType, new HashSet<State>()))
                    {
                        foreach (var type in StateTypeCache[stateMachineType])
                        {
                            var stateType = type;
                            if (type.IsAbstract)
                            {
                                continue;
                            }

                            if (type.IsGenericType)
                            {
                                // If the state type is generic (only possible if inherited by a generic state
                                // machine declaration), then iterate through the base state machine classes to
                                // identify the runtime generic type, and use it to instantiate the runtime state
                                // type. This type can be then used to create the state constructor.
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
                            var lambda = Expression.Lambda<Func<State>>(
                                Expression.New(constructor)).Compile();
                            var state = lambda();

                            try
                            {
                                state.InitializeState();
                            }
                            catch (InvalidOperationException ex)
                            {
                                Assert(false, "{0} {1} in state '{2}'.", Id, ex.Message, state);
                            }

                            StateInstanceCache[stateMachineType].Add(state);
                        }
                    }

                    // Caches the action declarations for this state machine type.
                    var map = new Dictionary<string, MethodInfo>();
                    foreach (var state in StateInstanceCache[stateMachineType])
                    {
                        if (state.EntryAction != null &&
                            !map.ContainsKey(state.EntryAction))
                        {
                            map.Add(state.EntryAction, GetActionWithName(state.EntryAction));
                        }

                        if (state.ExitAction != null &&
                            !map.ContainsKey(state.ExitAction))
                        {
                            map.Add(state.ExitAction, GetActionWithName(state.ExitAction));
                        }

                        foreach (var handler in state.EventHandlers.Values)
                        {
                            if (handler is GotoStateTransition transition)
                            {
                                if (transition.Lambda != null &&
                                    !map.ContainsKey(transition.Lambda))
                                {
                                    map.Add(transition.Lambda, GetActionWithName(transition.Lambda));
                                }
                            }
                            
                            if (handler is ActionEventHandlerDeclaration action)
                            {
                                if (!map.ContainsKey(action.Name))
                                {
                                    map.Add(action.Name, GetActionWithName(action.Name));
                                }
                            }
                        }
                    }

                    ActionCache.TryAdd(stateMachineType, map);
                }
            }

            // Populates the map of event handlers for this state machine instance.
            foreach (var kvp in ActionCache[stateMachineType])
            {
                StateMachineActionMap.Add(kvp.Key, new CachedDelegate(kvp.Value, this));
            }

            var initialStates = StateInstanceCache[stateMachineType].Where(state => state.IsStart).ToList();
            Assert(initialStates.Count != 0, "{0} must declare a start state.", Id);
            Assert(initialStates.Count is 1, "{0} can not declare more than one start states.", Id);

            DoStateTransition(initialStates[0]);
            AssertStateValidity();
        }

        /// <summary>
        /// Processes a type, looking for states.
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
                    StateTypeCache[GetType()].Add(nextType);
                }
            }
        }

        /// <summary>
        /// Returns the set of all states in the state machine (for code coverage).
        /// </summary>
        internal HashSet<string> GetAllStates()
        {
            Assert(StateInstanceCache.ContainsKey(GetType()), "{0} has not populated its states yet.", Id);

            var allStates = new HashSet<string>();
            foreach (var state in StateInstanceCache[GetType()])
            {
                allStates.Add(NameResolver.GetQualifiedStateName(state.GetType()));
            }

            return allStates;
        }

        private static bool IncludeInCoverage(EventHandlerDeclaration handler)
        {
            if (handler is DeferEventHandlerDeclaration || handler is IgnoreEventHandlerDeclaration)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the state machine (for code coverage).
        /// It does not include events that are deferred or ignored.
        /// </summary>
        internal HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            Assert(StateInstanceCache.ContainsKey(GetType()), "{0} has not populated its states yet.", Id);

            var pairs = new HashSet<Tuple<string, string>>();
            foreach (var state in StateInstanceCache[GetType()])
            {
                foreach (var binding in from b in state.EventHandlers 
                         where IncludeInCoverage(b.Value)
                         select b)
                {
                    pairs.Add(Tuple.Create(NameResolver.GetQualifiedStateName(state.GetType()), binding.Key.FullName));
                }
            }

            return pairs;
        }

        /// <summary>
        /// Checks the state machine for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            Assert(StateTypeCache[GetType()].Count > 0, "{0} must have one or more states.", Id);
            Assert(CurrentState != null, "{0} must not have a null current state.", Id);
        }

        /// <summary>
        /// Returns the formatted strint to be used with a fair nondeterministic boolean choice.
        /// </summary>
        private protected override string FormatFairRandom(string callerMemberName, string callerFilePath, int callerLineNumber) =>
            string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}_{4}",
                Id.Name, CurrentStateName, callerMemberName, callerFilePath, callerLineNumber.ToString());

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        private protected override void ReportUnhandledException(Exception ex, string actionName)
        {
            var state = CurrentState is null ? "<unknown>" : CurrentStateName;
            Runtime.WrapAndThrowException(ex, "{0} (state '{1}', action '{2}')", Id, state, actionName);
        }

        /// <summary>
        /// Defines the <see cref="StateMachine"/> transition that is the
        /// result of executing an event handler.  Transitions are created by using
        /// <see cref="RaiseGotoStateEvent{T}"/>, <see cref="RaiseEvent"/> and <see cref="RaiseHaltEvent"/>.
        /// The Transition is processed by the Coyote runtime when
        /// an event handling method of a StateMachine returns a Transition object.
        /// This means such a method can only do one such Transition per method call.
        /// If the method wants to do a conditional transition it can return
        /// Transition.None to indicate no transition is to be performed.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/programming-models/actors/state-machines">State machines</see> for more information.
        /// </remarks>
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
            /// This special transition represents a transition that does not change the current <see cref="StateMachine.State"/>.
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
            /// Defines the type of a <see cref="StateMachine"/> transition.
            /// </summary>
            public enum Type
            {
                /// <summary>
                /// A transition that does not change the <see cref="StateMachine.State"/>.
                /// This is the value used by <see cref="Transition.None"/>.
                /// </summary>
                None = 0,

                /// <summary>
                /// A transition created by <see cref="StateMachine.RaiseEvent(Event)"/> that raises an <see cref="Event"/> bypassing
                /// the <see cref="StateMachine.State"/> inbox.
                /// </summary>
                RaiseEvent,

                /// <summary>
                /// A transition created by <see cref="RaiseGotoStateEvent{S}"/> that pops the current <see cref="StateMachine.State"/>
                /// and pushes the specified <see cref="StateMachine.State"/> on the
                /// stack of <see cref="StateMachine"/> states.
                /// </summary>
                GotoState,

                /// <summary>
                /// A transition created by <see cref="RaiseHaltEvent"/> that halts the <see cref="StateMachine"/>.
                /// </summary>
                Halt
            }
        }

        /// <summary>
        /// Abstract class representing a state.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/programming-models/actors/state-machines">State machines</see> for more information.
        /// </remarks>
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
            /// Map containing all non-inheritable event handler declarations.
            /// </summary>
            internal Dictionary<Type, EventHandlerDeclaration> EventHandlers;

            /// <summary>
            /// True if this is the start state.
            /// </summary>
            internal bool IsStart { get; private set; }

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
                EventHandlers = new Dictionary<Type, EventHandlerDeclaration>();

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

                // Events with already declared handlers.
                var handledEvents = new HashSet<Type>();

                // Install event handlers.
                InstallGotoTransitions(handledEvents);
                InstallActionBindings(handledEvents);
                InstallIgnoreHandlers(handledEvents);
                InstallDeferHandlers(handledEvents);
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
            }

            /// <summary>
            /// Installs action bindings, if there are any.
            /// </summary>
            private void InstallActionBindings(HashSet<Type> handledEvents)
            {
                var doAttributes = GetType().GetCustomAttributes(typeof(OnEventDoActionAttribute), false)
                    as OnEventDoActionAttribute[];

                foreach (var attr in doAttributes)
                {
                    CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                    EventHandlers.Add(attr.Event, new ActionEventHandlerDeclaration(attr.Action));
                    handledEvents.Add(attr.Event);
                }
            }
            
            /// <summary>
            /// Declares ignore event handlers, if there are any.
            /// </summary>
            private void InstallIgnoreHandlers(HashSet<Type> handledEvents)
            {
                var ignoredEvents = new HashSet<Type>();
                if (GetType().GetCustomAttribute(typeof(IgnoreEventsAttribute), false) is IgnoreEventsAttribute ignoreEventsAttribute)
                {
                    foreach (var e in ignoreEventsAttribute.Events)
                    {
                        CheckEventHandlerAlreadyDeclared(e, handledEvents);

                        EventHandlers.Add(e, new IgnoreEventHandlerDeclaration());
                        ignoredEvents.Add(e);
                        handledEvents.Add(e);
                    }
                }
            }
            

            /// <summary>
            /// Declares defer event handlers, if there are any.
            /// </summary>
            private void InstallDeferHandlers(HashSet<Type> handledEvents)
            {
                var deferredEvents = new HashSet<Type>();
                if (GetType().GetCustomAttribute(typeof(DeferEventsAttribute), false) is DeferEventsAttribute deferEventsAttribute)
                {
                    foreach (var e in deferEventsAttribute.Events)
                    {
                        CheckEventHandlerAlreadyDeclared(e, handledEvents);
                        EventHandlers.Add(e, new DeferEventHandlerDeclaration());
                        deferredEvents.Add(e);
                        handledEvents.Add(e);
                    }
                }

                InheritDeferHandlers(GetType().BaseType, handledEvents, deferredEvents);
            }

            /// <summary>
            /// Inherits defer event handlers from a base state, if there is one.
            /// </summary>
            private void InheritDeferHandlers(Type baseState, HashSet<Type> handledEvents, HashSet<Type> deferredEvents)
            {
                if (!baseState.IsSubclassOf(typeof(State)))
                {
                    return;
                }

                if (baseState.GetCustomAttribute(typeof(DeferEventsAttribute), false) is DeferEventsAttribute deferEventsAttribute)
                {
                    foreach (var e in deferEventsAttribute.Events)
                    {
                        if (deferredEvents.Contains(e))
                        {
                            continue;
                        }
                        CheckEventHandlerAlreadyDeclared(e, handledEvents);
                        EventHandlers.Add(e, new DeferEventHandlerDeclaration());
                        deferredEvents.Add(e);
                        handledEvents.Add(e);
                    }
                }

                InheritDeferHandlers(baseState.BaseType, handledEvents, deferredEvents);
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
            /// Attribute for declaring the state that a state machine transitions upon creation.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class StartAttribute : Attribute
            {
            }

            /// <summary>
            /// Attribute for declaring what action to perform when entering a state.
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
                /// <param name="actionName">The name of the action to execute.</param>
                public OnEntryAttribute(string actionName)
                {
                    Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring what action to perform when exiting a state.
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
                /// <param name="actionName">The name of the action to execute.</param>
                public OnExitAttribute(string actionName)
                {
                    Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring a goto state transition when the state machine
            /// is in the specified state and dequeues an event of the specified type.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            protected sealed class OnEventGotoStateAttribute : Attribute
            {
                /// <summary>
                /// The type of the dequeued event.
                /// </summary>
                internal readonly Type Event;

                /// <summary>
                /// The type of the state.
                /// </summary>
                internal readonly Type State;

                /// <summary>
                /// Action name.
                /// </summary>
                internal readonly string Action;

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventGotoStateAttribute"/> class.
                /// </summary>
                /// <param name="eventType">The type of the dequeued event.</param>
                /// <param name="stateType">The type of the state.</param>
                public OnEventGotoStateAttribute(Type eventType, Type stateType)
                {
                    Event = eventType;
                    State = stateType;
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventGotoStateAttribute"/> class.
                /// </summary>
                /// <param name="eventType">The type of the dequeued event.</param>
                /// <param name="stateType">The type of the state.</param>
                /// <param name="actionName">Name of action to perform on exit.</param>
                public OnEventGotoStateAttribute(Type eventType, Type stateType, string actionName)
                {
                    Event = eventType;
                    State = stateType;
                    Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring which action should be invoked when the state machine
            /// is in the specified state to handle a dequeued event of the specified type.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            protected sealed class OnEventDoActionAttribute : Attribute
            {
                /// <summary>
                /// The type of the dequeued event.
                /// </summary>
                internal Type Event;

                /// <summary>
                /// The name of the action to invoke.
                /// </summary>
                internal string Action;

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventDoActionAttribute"/> class.
                /// </summary>
                /// <param name="eventType">The type of the dequeued event.</param>
                /// <param name="actionName">The name of the action to invoke.</param>
                public OnEventDoActionAttribute(Type eventType, string actionName)
                {
                    Event = eventType;
                    Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring what events should be deferred in a state.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class DeferEventsAttribute : Attribute
            {
                /// <summary>
                /// Event types.
                /// </summary>
                internal Type[] Events;

                /// <summary>
                /// Initializes a new instance of the <see cref="DeferEventsAttribute"/> class.
                /// </summary>
                /// <param name="eventTypes">Event types</param>
                public DeferEventsAttribute(params Type[] eventTypes)
                {
                    Events = eventTypes;
                }
            }

            /// <summary>
            /// Attribute for declaring what events should be ignored in a state.
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
        }
    }
}