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
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Type that implements a state machine actor. Inherit from this class to declare
    /// a custom actor with states, state transitions and event handlers.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/programming-models/actors/state-machines">State machines</see>
    /// for more information.
    /// </remarks>
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
        /// A stack of states. The state on the top of the stack represents the current state.
        /// </summary>
        private readonly Stack<State> StateStack;

        /// <summary>
        /// A map from event type to a Stack of HandlerInfo where the stack contains the inheritable
        /// event handlers defined by each state that has been pushed onto the StateStack (if any).
        /// The HandlerInfo also remembers which state the handler was defined on so that when the
        /// handler is invoked the IActorRuntimeLog can be given that information.
        /// </summary>
        private readonly Dictionary<Type, Stack<HandlerInfo>> InheritableEventHandlerMap;

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
        protected internal Type CurrentState { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachine"/> class.
        /// </summary>
        protected StateMachine()
            : base()
        {
            this.StateStack = new Stack<State>();
            this.InheritableEventHandlerMap = new Dictionary<Type, Stack<HandlerInfo>>();
            this.EventHandlerMap = EmptyEventHandlerMap;
            this.StateMachineActionMap = new Dictionary<string, CachedDelegate>();
        }

        /// <summary>
        /// Initializes the actor with the specified optional event.
        /// </summary>
        /// <param name="initialEvent">Optional event used for initialization.</param>
        internal override async Task InitializeAsync(Event initialEvent)
        {
            // Invoke the custom initializer, if there is one.
            await this.InvokeUserCallbackAsync(UserCallbackType.OnInitialize, initialEvent);

            // Execute the entry action of the start state, if there is one.
            await this.ExecuteCurrentStateOnEntryAsync(initialEvent);
            if (this.CurrentStatus is Status.Halting)
            {
                await this.HaltAsync(initialEvent);
            }
        }

        /// <summary>
        /// Raises the specified <see cref="Event"/> at the end of the current action.
        /// </summary>
        /// <remarks>
        /// This event is not handled until the action that calls this method returns control back
        /// to the Coyote runtime.  It is handled before any other events are dequeued from the inbox.
        /// Only one of the following can be called per action:
        /// <see cref="RaiseEvent"/>, <see cref="RaiseGotoStateEvent{T}"/>,  <see cref="RaisePushStateEvent{T}"/> or
        /// <see cref="RaisePopStateEvent"/> and <see cref="RaiseHaltEvent"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        /// <param name="e">The event to raise.</param>
        protected void RaiseEvent(Event e)
        {
            this.Assert(this.CurrentStatus is Status.Active, "{0} invoked RaiseEvent while halting.", this.Id);
            this.Assert(e != null, "{0} is raising a null event.", this.Id);
            this.CheckDanglingTransition();
            this.PendingTransition = new Transition(Transition.Type.RaiseEvent, default, e);
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
        /// to the Coyote runtime.  It is handled before any other events are dequeued from the inbox.
        /// Only one of the following can be called per action:
        /// <see cref="RaiseEvent"/>, <see cref="RaiseGotoStateEvent{T}"/>,  <see cref="RaisePushStateEvent{T}"/> or
        /// <see cref="RaisePopStateEvent"/> and <see cref="RaiseHaltEvent"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        /// <typeparam name="S">Type of the state.</typeparam>
        protected void RaiseGotoStateEvent<S>()
            where S : State =>
            this.RaiseGotoStateEvent(typeof(S));

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
        /// to the Coyote runtime.  It is handled before any other events are dequeued from the inbox.
        /// Only one of the following can be called per action:
        /// <see cref="RaiseEvent"/>, <see cref="RaiseGotoStateEvent{T}"/>,  <see cref="RaisePushStateEvent{T}"/> or
        /// <see cref="RaisePopStateEvent"/> and <see cref="RaiseHaltEvent"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        /// <param name="state">Type of the state.</param>
        protected void RaiseGotoStateEvent(Type state)
        {
            this.Assert(this.CurrentStatus is Status.Active, "{0} invoked GotoState while halting.", this.Id);
            this.Assert(StateTypeCache[this.GetType()].Any(val => val.DeclaringType.Equals(state.DeclaringType) && val.Name.Equals(state.Name)),
                "{0} is trying to transition to non-existing state '{1}'.", this.Id, state.Name);
            this.CheckDanglingTransition();
            this.PendingTransition = new Transition(Transition.Type.GotoState, state, default);
        }

        /// <summary>
        /// Raise a special event that performs a push state operation at the end of the current action.
        /// </summary>
        /// <remarks>
        /// Pushing a state does not pop the current <see cref="State"/>, instead it pushes the specified <see cref="State"/> on the active state stack
        /// so that you can have multiple active states.  In this case events can be handled by all active states on the stack.
        /// This is shorthand for the following code:
        /// <code>
        /// class Event E { }
        /// [OnEventPushState(typeof(E), typeof(S))]
        /// this.RaiseEvent(new E());
        /// </code>
        /// This event is not handled until the action that calls this method returns control back
        /// to the Coyote runtime.  It is handled before any other events are dequeued from the inbox.
        /// Only one of the following can be called per action:
        /// <see cref="RaiseEvent"/>, <see cref="RaiseGotoStateEvent{T}"/>,  <see cref="RaisePushStateEvent{T}"/> or
        /// <see cref="RaisePopStateEvent"/> and <see cref="RaiseHaltEvent"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        /// <typeparam name="S">Type of the state.</typeparam>
        protected void RaisePushStateEvent<S>()
            where S : State =>
            this.RaisePushStateEvent(typeof(S));

        /// <summary>
        /// Raise a special event that performs a push state operation at the end of the current action.
        /// </summary>
        /// <remarks>
        /// Pushing a state does not pop the current <see cref="State"/>, instead it pushes the specified <see cref="State"/> on the active state stack
        /// so that you can have multiple active states.  In this case events can be handled by all active states on the stack.
        /// This is shorthand for the following code:
        /// <code>
        /// class Event E { }
        /// [OnEventPushState(typeof(E), typeof(S))]
        /// this.RaiseEvent(new E());
        /// </code>
        /// This event is not handled until the action that calls this method returns control back
        /// to the Coyote runtime.  It is handled before any other events are dequeued from the inbox.
        /// Only one of the following can be called per action:
        /// <see cref="RaiseEvent"/>, <see cref="RaiseGotoStateEvent{T}"/>,  <see cref="RaisePushStateEvent{T}"/> or
        /// <see cref="RaisePopStateEvent"/> and <see cref="RaiseHaltEvent"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        /// <param name="state">Type of the state.</param>
        protected void RaisePushStateEvent(Type state)
        {
            this.Assert(this.CurrentStatus is Status.Active, "{0} invoked PushState while halting.", this.Id);
            this.Assert(StateTypeCache[this.GetType()].Any(val => val.DeclaringType.Equals(state.DeclaringType) && val.Name.Equals(state.Name)),
                "{0} is trying to transition to non-existing state '{1}'.", this.Id, state.Name);
            this.CheckDanglingTransition();
            this.PendingTransition = new Transition(Transition.Type.PushState, state, default);
        }

        /// <summary>
        /// Raise a special event that performs a pop state operation at the end of the current action.
        /// </summary>
        /// <remarks>
        /// Popping a state pops the current <see cref="State"/> that was pushed using <see cref='RaisePushStateEvent'/> or an OnEventPushStateAttribute.
        /// An assert is raised if there are no states left to pop.
        /// This event is not handled until the action that calls this method returns control back
        /// to the Coyote runtime.  It is handled before any other events are dequeued from the inbox.
        ///
        /// Only one of the following can be called per action:
        /// <see cref="RaiseEvent"/>, <see cref="RaiseGotoStateEvent{T}"/>,  <see cref="RaisePushStateEvent{T}"/> or
        /// <see cref="RaisePopStateEvent"/> and <see cref="RaiseHaltEvent"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        protected void RaisePopStateEvent()
        {
            this.Assert(this.CurrentStatus is Status.Active, "{0} invoked PopState while halting.", this.Id);
            this.CheckDanglingTransition();
            this.PendingTransition = new Transition(Transition.Type.PopState, null, default);
        }

        /// <summary>
        /// Raises a <see cref='HaltEvent'/> to halt the actor at the end of the current action.
        /// </summary>
        /// <remarks>
        /// This event is not handled until the action that calls this method returns control back
        /// to the Coyote runtime.  It is handled before any other events are dequeued from the inbox.
        ///
        /// Only one of the following can be called per action:
        /// <see cref="RaiseEvent"/>, <see cref="RaiseGotoStateEvent{T}"/>,  <see cref="RaisePushStateEvent{T}"/> or
        /// <see cref="RaisePopStateEvent"/> and <see cref="RaiseHaltEvent"/>.
        /// An Assert is raised if you accidentally try and do two of these operations in a single action.
        /// </remarks>
        protected override void RaiseHaltEvent()
        {
            base.RaiseHaltEvent();
            this.CheckDanglingTransition();
            this.PendingTransition = new Transition(Transition.Type.Halt, null, default);
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
            Type currentState = this.CurrentState;

            while (true)
            {
                if (this.CurrentState is null)
                {
                    // If the stack of states is empty then halt or fail the state machine.
                    if (e is HaltEvent)
                    {
                        // If it is the halt event, then change the actor status to halting.
                        this.CurrentStatus = Status.Halting;
                        break;
                    }

                    string currentStateName = NameResolver.GetQualifiedStateName(currentState);
                    await this.InvokeUserCallbackAsync(UserCallbackType.OnEventUnhandled, e, currentStateName);
                    if (this.CurrentStatus is Status.Active)
                    {
                        // If the event cannot be handled then report an error, else halt gracefully.
                        var ex = new UnhandledEventException(e, currentStateName, "Unhandled Event");
                        bool isHalting = this.OnUnhandledEventExceptionHandler(ex, e);
                        this.Assert(isHalting, "{0} received event '{1}' that cannot be handled.",
                            this.Id, e.GetType().FullName);
                    }

                    break;
                }

                if (e is GotoStateEvent gotoStateEvent)
                {
                    await this.GotoStateAsync(gotoStateEvent.State, null, e);
                }
                else if (e is PushStateEvent pushStateEvent)
                {
                    await this.PushStateAsync(pushStateEvent.State, e);
                }
                else if (this.EventHandlerMap.ContainsKey(e.GetType()))
                {
                    await this.HandleEventAsync(e, this.StateStack.Peek(), this.EventHandlerMap[e.GetType()]);
                }
                else
                {
                    bool hasWildCard = this.TryGetInheritedHandler(typeof(WildCardEvent), out HandlerInfo wildInfo);
                    if (this.EventHandlerMap.ContainsKey(typeof(WildCardEvent)))
                    {
                        // A non-inherited wildcard handler cannot beat a "specific" event handler if that
                        // "specific" event handler is also at the top of the stack.
                        wildInfo = new HandlerInfo(this.StateStack.Peek(), this.StateStack.Count,
                            this.EventHandlerMap[typeof(WildCardEvent)]);
                        hasWildCard = true;
                    }

                    bool hasSpecific = this.TryGetInheritedHandler(e.GetType(), out HandlerInfo info);

                    if ((hasWildCard && hasSpecific && wildInfo.StackDepth > info.StackDepth) ||
                        (!hasSpecific && hasWildCard))
                    {
                        // Then wild card takes precedence over earlier specific event handlers.
                        await this.HandleEventAsync(e, wildInfo.State, wildInfo.Handler);
                    }
                    else if (hasSpecific)
                    {
                        // Then specific event is more recent than any wild card events.
                        await this.HandleEventAsync(e, info.State, info.Handler);
                    }
                    else if (this.ActionMap.TryGetValue(e.GetType(), out CachedDelegate handler))
                    {
                        // Allow StateMachine to have class level OnEventDoActions the same way Actor allows.
                        this.Runtime.NotifyInvokedAction(this, handler.MethodInfo, this.CurrentStateName, this.CurrentStateName, e);
                        await this.InvokeActionAsync(handler, e);
                    }
                    else
                    {
                        // If the current state cannot handle the event.
                        await this.ExecuteCurrentStateOnExitAsync(null, e);
                        if (this.CurrentStatus is Status.Active)
                        {
                            this.Runtime.LogWriter.LogPopStateUnhandledEvent(this.Id, this.CurrentStateName, e);
                            this.DoStatePop();
                            continue;
                        }
                    }
                }

                break;
            }
        }

        private async Task HandleEventAsync(Event e, State declaringState, EventHandlerDeclaration eventHandler)
        {
            string handlingStateName = NameResolver.GetQualifiedStateName(declaringState.GetType());
            if (eventHandler is ActionEventHandlerDeclaration actionEventHandler)
            {
                CachedDelegate cachedAction = this.StateMachineActionMap[actionEventHandler.Name];
                this.Runtime.NotifyInvokedAction(this, cachedAction.MethodInfo, handlingStateName, this.CurrentStateName, e);
                await this.InvokeActionAsync(cachedAction, e);
                await this.ApplyEventHandlerTransitionAsync(this.PendingTransition, e);
            }
            else if (eventHandler is GotoStateTransition gotoTransition)
            {
                await this.GotoStateAsync(gotoTransition.TargetState, gotoTransition.Lambda, e);
            }
            else if (eventHandler is PushStateTransition pushTransition)
            {
                await this.PushStateAsync(pushTransition.TargetState, e);
            }
        }

        /// <summary>
        /// Executes the on entry action of the current state.
        /// </summary>
        private async Task ExecuteCurrentStateOnEntryAsync(Event e)
        {
            this.Runtime.NotifyEnteredState(this);

            CachedDelegate entryAction = null;
            if (this.StateStack.Peek().EntryAction != null)
            {
                entryAction = this.StateMachineActionMap[this.StateStack.Peek().EntryAction];
            }

            // Invokes the entry action of the new state, if there is one available.
            if (entryAction != null)
            {
                this.Runtime.NotifyInvokedOnEntryAction(this, entryAction.MethodInfo, e);
                await this.InvokeActionAsync(entryAction, e);
                await this.ApplyEventHandlerTransitionAsync(this.PendingTransition, e);
            }
        }

        /// <summary>
        /// Executes the on exit action of the current state.
        /// </summary>
        private async Task ExecuteCurrentStateOnExitAsync(string eventHandlerExitActionName, Event e)
        {
            this.Runtime.NotifyExitedState(this);

            CachedDelegate exitAction = null;
            if (this.StateStack.Peek().ExitAction != null)
            {
                exitAction = this.StateMachineActionMap[this.StateStack.Peek().ExitAction];
            }

            // Invokes the exit action of the current state,
            // if there is one available.
            if (exitAction != null)
            {
                this.Runtime.NotifyInvokedOnExitAction(this, exitAction.MethodInfo, e);
                await this.InvokeActionAsync(exitAction, e);
                Transition transition = this.PendingTransition;
                this.Assert(transition.TypeValue is Transition.Type.None ||
                    transition.TypeValue is Transition.Type.Halt,
                    "{0} has performed a '{1}' transition from an OnExit action.",
                    this.Id, transition.TypeValue);
                await this.ApplyEventHandlerTransitionAsync(transition, e);
            }

            // Invokes the exit action of the event handler,
            // if there is one available.
            if (eventHandlerExitActionName != null && this.CurrentStatus is Status.Active)
            {
                CachedDelegate eventHandlerExitAction = this.StateMachineActionMap[eventHandlerExitActionName];
                this.Runtime.NotifyInvokedOnExitAction(this, eventHandlerExitAction.MethodInfo, e);
                await this.InvokeActionAsync(eventHandlerExitAction, e);
                Transition transition = this.PendingTransition;
                this.Assert(transition.TypeValue is Transition.Type.None ||
                    transition.TypeValue is Transition.Type.Halt,
                    "{0} has performed a '{1}' transition from an OnExit action.",
                    this.Id, transition.TypeValue);
                await this.ApplyEventHandlerTransitionAsync(transition, e);
            }
        }

        /// <summary>
        /// Applies the specified event handler transition.
        /// </summary>
        private async Task ApplyEventHandlerTransitionAsync(Transition transition, Event e)
        {
            if (transition.TypeValue != this.PendingTransition.TypeValue && this.PendingTransition.TypeValue != Transition.Type.None)
            {
                this.CheckDanglingTransition();
            }
            else if (transition.TypeValue is Transition.Type.RaiseEvent)
            {
                this.PendingTransition = default;
                this.Inbox.RaiseEvent(transition.Event, this.OperationGroupId);
            }
            else if (transition.TypeValue is Transition.Type.GotoState)
            {
                this.PendingTransition = default;
                this.Inbox.RaiseEvent(new GotoStateEvent(transition.State), this.OperationGroupId);
            }
            else if (transition.TypeValue is Transition.Type.PushState)
            {
                this.PendingTransition = default;
                this.Inbox.RaiseEvent(new PushStateEvent(transition.State), this.OperationGroupId);
            }
            else if (transition.TypeValue is Transition.Type.PopState)
            {
                this.PendingTransition = default;
                var prevStateName = this.CurrentStateName;
                this.Runtime.NotifyPopState(this);

                // The state machine performs the on exit action of the current state.
                await this.ExecuteCurrentStateOnExitAsync(null, e);
                if (this.CurrentStatus is Status.Active)
                {
                    this.DoStatePop();
                    this.Runtime.LogWriter.LogPopState(this.Id, prevStateName, this.CurrentStateName);
                    this.Assert(this.CurrentState != null, "{0} popped its state with no matching push state.", this.Id);
                }
            }
            else if (transition.TypeValue is Transition.Type.Halt)
            {
                // If it is the halt transition, then change the actor status to halting.
                this.PendingTransition = default;
                this.CurrentStatus = Status.Halting;
            }
            else
            {
                this.PendingTransition = default;
            }
        }

        /// <summary>
        /// Notifies that a Transition was created but not returned to the StateMachine.
        /// </summary>
        private void CheckDanglingTransition()
        {
            var transition = this.PendingTransition;
            this.PendingTransition = default;

            if (transition.TypeValue != Transition.Type.None)
            {
                string prefix = string.Format("{0} transition created by {1} in state {2} was not processed",
                    transition.TypeValue, this.GetType().FullName, this.CurrentStateName);
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

                this.Assert(false, prefix + suffix);
            }
        }

        /// <summary>
        /// Performs a goto transition to the specified state.
        /// </summary>
        private async Task GotoStateAsync(Type s, string onExitActionName, Event e)
        {
            this.Runtime.LogWriter.LogGotoState(this.Id, this.CurrentStateName,
                $"{s.DeclaringType}.{NameResolver.GetStateNameForLogging(s)}");

            // The state machine performs the on exit action of the current state.
            await this.ExecuteCurrentStateOnExitAsync(onExitActionName, e);
            if (this.CurrentStatus is Status.Active)
            {
                this.DoStatePop();

                // The state machine transitions to the new state.
                var nextState = StateInstanceCache[this.GetType()].First(val => val.GetType().Equals(s));
                this.DoStatePush(nextState);

                // The state machine performs the on entry action of the new state.
                await this.ExecuteCurrentStateOnEntryAsync(e);
            }
        }

        /// <summary>
        /// Performs a push transition to the specified state.
        /// </summary>
        private async Task PushStateAsync(Type s, Event e)
        {
            this.Runtime.LogWriter.LogPushState(this.Id, this.CurrentStateName, s.FullName);

            var nextState = StateInstanceCache[this.GetType()].First(val => val.GetType().Equals(s));
            this.DoStatePush(nextState);

            // The state machine performs the on entry statements of the new state.
            await this.ExecuteCurrentStateOnEntryAsync(e);
        }

        private void PushHandler(State state, Type eventType, EventHandlerDeclaration handler)
        {
            if (handler.Inheritable)
            {
                if (!this.InheritableEventHandlerMap.TryGetValue(eventType, out Stack<HandlerInfo> stack))
                {
                    stack = new Stack<HandlerInfo>();
                    this.InheritableEventHandlerMap[eventType] = stack;
                }

                stack.Push(new HandlerInfo(state, this.StateStack.Count, handler));
            }
        }

        private bool TryGetInheritedHandler(Type eventType, out HandlerInfo result)
        {
            if (this.InheritableEventHandlerMap.TryGetValue(eventType, out Stack<HandlerInfo> stack) && stack.Count > 0)
            {
                result = stack.Peek();
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Configures the state transitions of the state machine when a state is pushed into the stack.
        /// </summary>
        private void DoStatePush(State state)
        {
            this.EventHandlerMap = state.EventHandlers;  // non-inheritable handlers.

            this.StateStack.Push(state);
            this.CurrentState = state.GetType();
            this.CurrentStateName = NameResolver.GetQualifiedStateName(this.CurrentState);

            // Push the inheritable event handlers.
            foreach (var eventHandler in state.InheritableEventHandlers)
            {
                this.PushHandler(state, eventHandler.Key, eventHandler.Value);
            }
        }

        /// <summary>
        /// Configures the state transitions of the state machine
        /// when a state is popped.
        /// </summary>
        private void DoStatePop()
        {
            State state = this.StateStack.Pop();
            foreach (var item in this.InheritableEventHandlerMap)
            {
                var stack = item.Value;
                if (stack != null && stack.Count > 0 && stack.Peek().State == state)
                {
                    stack.Pop();
                }
            }

            if (this.StateStack.Count > 0)
            {
                // re-instate the non-inheritable handlers from previous state.
                state = this.StateStack.Peek();
                this.CurrentState = state.GetType();
                this.CurrentStateName = NameResolver.GetQualifiedStateName(this.CurrentState);
                this.EventHandlerMap = this.StateStack.Peek().EventHandlers;
            }
            else
            {
                this.EventHandlerMap = EmptyEventHandlerMap;
                this.CurrentState = null;
                this.CurrentStateName = string.Empty;
            }
        }

        /// <summary>
        /// Get the appropriate inherited event handler for the given event.
        /// </summary>
        /// <param name="e">The event we want to handle</param>
        /// <param name="info">The HandlerInfo in the state stack</param>
        /// <returns>True if a handler is found, otherwise false</returns>
        private bool GetInheritedEventHandler(Event e, ref HandlerInfo info)
        {
            Type eventType = e.GetType();
            // Wild card only takes precidence if it is higher on the state stack.
            bool hasWildCard = this.TryGetInheritedHandler(typeof(WildCardEvent), out HandlerInfo wildInfo);
            if (this.EventHandlerMap.ContainsKey(typeof(WildCardEvent)))
            {
                // a non-inherited wildcard handler cannot beat a "specific" IgnoreEvent instruction if that
                // "specific" instruction is also at the top of the stack.
                wildInfo.StackDepth = this.StateStack.Count;
                wildInfo.State = this.StateStack.Peek();
                wildInfo.Handler = this.EventHandlerMap[typeof(WildCardEvent)];
                hasWildCard = true;
            }

            bool hasSpecific = this.TryGetInheritedHandler(eventType, out info);

            if ((hasSpecific && hasWildCard && wildInfo.StackDepth > info.StackDepth) ||
                (!hasSpecific && hasWildCard))
            {
                info = wildInfo;
                return true;
            }

            if (hasSpecific)
            {
                return true;
            }

            info = new HandlerInfo(null, 0, null);
            return false;
        }

        /// <summary>
        /// Checks if the specified event is ignored in the current state.
        /// </summary>
        internal bool IsEventIgnoredInCurrentState(Event e)
        {
            if (e is TimerElapsedEvent timeoutEvent && !this.Timers.ContainsKey(timeoutEvent.Info))
            {
                // The timer that created this timeout event is not active.
                return true;
            }

            Type eventType = e.GetType();

            // If a non-inheritable transition is defined, then the event is not ignored
            // because the non-inheritable operation takes precedent.
            if (this.EventHandlerMap.ContainsKey(eventType))
            {
                return false;
            }

            HandlerInfo info = new HandlerInfo(null, 0, null);
            if (this.GetInheritedEventHandler(e, ref info))
            {
                return info.Handler is IgnoreEventHandlerDeclaration;
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified event is deferred in the current state.
        /// </summary>
        internal bool IsEventDeferredInCurrentState(Event e)
        {
            Type eventType = e.GetType();

            // If a non-inheritable transition is defined, then the event is not deferred.
            if (this.EventHandlerMap.ContainsKey(eventType))
            {
                return false;
            }

            HandlerInfo info = new HandlerInfo(null, 0, null);
            if (this.GetInheritedEventHandler(e, ref info))
            {
                return info.Handler is DeferEventHandlerDeclaration;
            }

            return false;
        }

        /// <summary>
        /// Checks if a default handler is installed in current state.
        /// </summary>
        internal bool IsDefaultHandlerInstalledInCurrentState() =>
            this.EventHandlerMap.ContainsKey(typeof(DefaultEvent)) ||
            this.TryGetInheritedHandler(typeof(DefaultEvent), out _);

        /// <summary>
        /// Returns the hashed state of this state machine.
        /// </summary>
        internal override int GetHashedState()
        {
            unchecked
            {
                var hash = 19;
                hash = (hash * 31) + this.GetType().GetHashCode();
                hash = (hash * 31) + this.Id.Value.GetHashCode();
                hash = (hash * 31) + this.IsHalted.GetHashCode();

                hash = (hash * 31) + this.Manager.GetCachedState();

                foreach (var state in this.StateStack)
                {
                    hash = (hash * 31) + state.GetType().GetHashCode();
                }

                hash = (hash * 31) + this.Inbox.GetCachedState();

                // Adds the user-defined hashed state.
                hash = (hash * 31) + this.HashedState;

                return hash;
            }
        }

        /// <summary>
        /// Extracts user declarations and setups the event handlers and state transitions.
        /// </summary>
        internal override void SetupEventHandlers()
        {
            base.SetupEventHandlers();
            Type stateMachineType = this.GetType();

            // If this type has not already been setup in the ActionCache, then we need to try and grab the ActionCacheLock
            // for this type.  First make sure we have one and only one lockable object for this type.
            object syncObject = ActionCacheLocks.GetOrAdd(stateMachineType, _ => new object());

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
                        Type baseType = stateMachineType;
                        while (baseType != typeof(StateMachine))
                        {
                            foreach (var s in baseType.GetNestedTypes(BindingFlags.Instance |
                                BindingFlags.NonPublic | BindingFlags.Public |
                                BindingFlags.DeclaredOnly))
                            {
                                this.ExtractStateTypes(s);
                            }

                            baseType = baseType.BaseType;
                        }
                    }

                    // Caches the available state instances for this state machine type.
                    if (StateInstanceCache.TryAdd(stateMachineType, new HashSet<State>()))
                    {
                        foreach (var type in StateTypeCache[stateMachineType])
                        {
                            Type stateType = type;
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
                                Type declaringType = this.GetType();
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

                            ConstructorInfo constructor = stateType.GetConstructor(Type.EmptyTypes);
                            var lambda = Expression.Lambda<Func<State>>(
                                Expression.New(constructor)).Compile();
                            State state = lambda();

                            try
                            {
                                state.InitializeState();
                            }
                            catch (InvalidOperationException ex)
                            {
                                this.Assert(false, "{0} {1} in state '{2}'.", this.Id, ex.Message, state);
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
                            map.Add(state.EntryAction, this.GetActionWithName(state.EntryAction));
                        }

                        if (state.ExitAction != null &&
                            !map.ContainsKey(state.ExitAction))
                        {
                            map.Add(state.ExitAction, this.GetActionWithName(state.ExitAction));
                        }

                        foreach (var handler in state.InheritableEventHandlers.Values)
                        {
                            if (handler is ActionEventHandlerDeclaration action)
                            {
                                if (!map.ContainsKey(action.Name))
                                {
                                    map.Add(action.Name, this.GetActionWithName(action.Name));
                                }
                            }
                        }

                        foreach (var handler in state.EventHandlers.Values)
                        {
                            if (handler is GotoStateTransition transition)
                            {
                                if (transition.Lambda != null &&
                                    !map.ContainsKey(transition.Lambda))
                                {
                                    map.Add(transition.Lambda, this.GetActionWithName(transition.Lambda));
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
                this.StateMachineActionMap.Add(kvp.Key, new CachedDelegate(kvp.Value, this));
            }

            var initialStates = StateInstanceCache[stateMachineType].Where(state => state.IsStart).ToList();
            this.Assert(initialStates.Count != 0, "{0} must declare a start state.", this.Id);
            this.Assert(initialStates.Count is 1, "{0} can not declare more than one start states.", this.Id);

            this.DoStatePush(initialStates[0]);
            this.AssertStateValidity();
        }

        /// <summary>
        /// Returns the type of the state at the specified state
        /// stack index, if there is one.
        /// </summary>
        internal Type GetStateTypeAtStackIndex(int index)
        {
            return this.StateStack.ElementAtOrDefault(index)?.GetType();
        }

        /// <summary>
        /// Processes a type, looking for states.
        /// </summary>
        private void ExtractStateTypes(Type type)
        {
            Stack<Type> stack = new Stack<Type>();
            stack.Push(type);

            while (stack.Count > 0)
            {
                Type nextType = stack.Pop();

                if (nextType.IsClass && nextType.IsSubclassOf(typeof(State)))
                {
                    StateTypeCache[this.GetType()].Add(nextType);
                }
                else if (nextType.IsClass && nextType.IsSubclassOf(typeof(StateGroup)))
                {
                    // Adds the contents of the group of states to the stack.
                    foreach (var t in nextType.GetNestedTypes(BindingFlags.Instance |
                        BindingFlags.NonPublic | BindingFlags.Public |
                        BindingFlags.DeclaredOnly))
                    {
                        this.Assert(t.IsSubclassOf(typeof(StateGroup)) || t.IsSubclassOf(typeof(State)),
                            "'{0}' is neither a group of states nor a state.", t.Name);
                        stack.Push(t);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the set of all states in the state machine (for code coverage).
        /// </summary>
        internal HashSet<string> GetAllStates()
        {
            this.Assert(StateInstanceCache.ContainsKey(this.GetType()), "{0} has not populated its states yet.", this.Id);

            var allStates = new HashSet<string>();
            foreach (var state in StateInstanceCache[this.GetType()])
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
            this.Assert(StateInstanceCache.ContainsKey(this.GetType()), "{0} has not populated its states yet.", this.Id);

            var pairs = new HashSet<Tuple<string, string>>();
            foreach (var state in StateInstanceCache[this.GetType()])
            {
                foreach (var binding in from b in state.InheritableEventHandlers.Concat(state.EventHandlers)
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
            this.Assert(StateTypeCache[this.GetType()].Count > 0, "{0} must have one or more states.", this.Id);
            this.Assert(this.StateStack.Peek() != null, "{0} must not have a null current state.", this.Id);
        }

        /// <summary>
        /// Returns the formatted strint to be used with a fair nondeterministic boolean choice.
        /// </summary>
        private protected override string FormatFairRandom(string callerMemberName, string callerFilePath, int callerLineNumber) =>
            string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}_{4}",
                this.Id.Name, this.CurrentStateName, callerMemberName, callerFilePath, callerLineNumber.ToString());

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        private protected override void ReportUnhandledException(Exception ex, string actionName)
        {
            var state = this.CurrentState is null ? "<unknown>" : this.CurrentStateName;
            this.Runtime.WrapAndThrowException(ex, "{0} (state '{1}', action '{2}')", this.Id, state, actionName);
        }

        /// <summary>
        /// Defines the <see cref="StateMachine"/> transition that is the
        /// result of executing an event handler.  Transitions are created by using
        /// <see cref="RaiseGotoStateEvent{T}"/>, <see cref="RaiseEvent"/>, <see cref="RaisePushStateEvent{T}"/> or
        /// <see cref="RaisePopStateEvent"/> and <see cref="RaiseHaltEvent"/>.
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
                this.TypeValue = type;
                this.State = state;
                this.Event = e;
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
                /// A transition created by <see cref="RaisePushStateEvent{S}"/> that pushes the specified <see cref="StateMachine.State"/>
                /// on the stack of <see cref="StateMachine"/> states.
                /// </summary>
                PushState,

                /// <summary>
                /// A transition created by <see cref="RaisePopStateEvent"/> that pops the current <see cref="StateMachine.State"/>
                /// from the stack of <see cref="StateMachine"/> states.
                /// </summary>
                PopState,

                /// <summary>
                /// A transition created by <see cref="RaiseHaltEvent"/> that halts the <see cref="StateMachine"/>.
                /// </summary>
                Halt
            }
        }

        /// <summary>
        /// A struct used to track event handlers that are pushed or popped on the StateStack.
        /// </summary>
        private struct HandlerInfo
        {
            /// <summary>
            /// The state that provided this EventHandler.
            /// </summary>
            public State State;

            /// <summary>
            /// Records where this State is in the StateStack.  This information is needed to implement WildCardEvent
            /// semantics.  A specific Handler closest to the top of the stack (higher StackDepth) wins over a
            /// WildCardEvent further down the stack (lower StackDepth).
            /// </summary>
            public int StackDepth;

            /// <summary>
            /// The event handler for a given event Type defined by the State.
            /// </summary>
            public EventHandlerDeclaration Handler;

            public HandlerInfo(State state, int depth, EventHandlerDeclaration handler)
            {
                this.State = state;
                this.StackDepth = depth;
                this.Handler = handler;
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
            /// Map containing all event handler declarations.
            /// </summary>
            internal Dictionary<Type, EventHandlerDeclaration> InheritableEventHandlers;

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
                this.IsStart = false;

                this.InheritableEventHandlers = new Dictionary<Type, EventHandlerDeclaration>();
                this.EventHandlers = new Dictionary<Type, EventHandlerDeclaration>();

                if (this.GetType().GetCustomAttribute(typeof(OnEntryAttribute), true) is OnEntryAttribute entryAttribute)
                {
                    this.EntryAction = entryAttribute.Action;
                }

                if (this.GetType().GetCustomAttribute(typeof(OnExitAttribute), true) is OnExitAttribute exitAttribute)
                {
                    this.ExitAction = exitAttribute.Action;
                }

                if (this.GetType().IsDefined(typeof(StartAttribute), false))
                {
                    this.IsStart = true;
                }

                // Events with already declared handlers.
                var handledEvents = new HashSet<Type>();

                // Install event handlers.
                this.InstallGotoTransitions(handledEvents);
                this.InstallPushTransitions(handledEvents);
                this.InstallActionBindings(handledEvents);
                this.InstallIgnoreHandlers(handledEvents);
                this.InstallDeferHandlers(handledEvents);
            }

            /// <summary>
            /// Declares goto event handlers, if there are any.
            /// </summary>
            private void InstallGotoTransitions(HashSet<Type> handledEvents)
            {
                var gotoAttributes = this.GetType().GetCustomAttributes(typeof(OnEventGotoStateAttribute), false)
                    as OnEventGotoStateAttribute[];

                foreach (var attr in gotoAttributes)
                {
                    CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                    if (attr.Action is null)
                    {
                        this.EventHandlers.Add(attr.Event, new GotoStateTransition(attr.State));
                    }
                    else
                    {
                        this.EventHandlers.Add(attr.Event, new GotoStateTransition(attr.State, attr.Action));
                    }

                    handledEvents.Add(attr.Event);
                }

                this.InheritGotoTransitions(this.GetType().BaseType, handledEvents);
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
                    if (this.EventHandlers.ContainsKey(attr.Event))
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
                    this.EventHandlers.Add(kvp.Key, kvp.Value);
                }

                this.InheritGotoTransitions(baseState.BaseType, handledEvents);
            }

            /// <summary>
            /// Declares push event handlers, if there are any.
            /// </summary>
            private void InstallPushTransitions(HashSet<Type> handledEvents)
            {
                var pushAttributes = this.GetType().GetCustomAttributes(typeof(OnEventPushStateAttribute), false)
                    as OnEventPushStateAttribute[];

                foreach (var attr in pushAttributes)
                {
                    CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                    this.EventHandlers.Add(attr.Event, new PushStateTransition(attr.State));
                    handledEvents.Add(attr.Event);
                }

                this.InheritPushTransitions(this.GetType().BaseType, handledEvents);
            }

            /// <summary>
            /// Inherits push event handlers from a base state, if there is one.
            /// </summary>
            private void InheritPushTransitions(Type baseState, HashSet<Type> handledEvents)
            {
                if (!baseState.IsSubclassOf(typeof(State)))
                {
                    return;
                }

                var pushAttributesInherited = baseState.GetCustomAttributes(typeof(OnEventPushStateAttribute), false)
                    as OnEventPushStateAttribute[];

                var pushTransitionsInherited = new Dictionary<Type, PushStateTransition>();
                foreach (var attr in pushAttributesInherited)
                {
                    if (this.EventHandlers.ContainsKey(attr.Event))
                    {
                        continue;
                    }

                    CheckEventHandlerAlreadyInherited(attr.Event, baseState, handledEvents);

                    pushTransitionsInherited.Add(attr.Event, new PushStateTransition(attr.State));
                    handledEvents.Add(attr.Event);
                }

                foreach (var kvp in pushTransitionsInherited)
                {
                    this.EventHandlers.Add(kvp.Key, kvp.Value);
                }

                this.InheritPushTransitions(baseState.BaseType, handledEvents);
            }

            /// <summary>
            /// Installs action bindings, if there are any.
            /// </summary>
            private void InstallActionBindings(HashSet<Type> handledEvents)
            {
                var doAttributes = this.GetType().GetCustomAttributes(typeof(OnEventDoActionAttribute), false)
                    as OnEventDoActionAttribute[];

                foreach (var attr in doAttributes)
                {
                    CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                    this.InheritableEventHandlers.Add(attr.Event, new ActionEventHandlerDeclaration(attr.Action));
                    handledEvents.Add(attr.Event);
                }

                this.InheritActionBindings(this.GetType().BaseType, handledEvents);
            }

            /// <summary>
            /// Inherits action bindings from a base state, if there is one.
            /// </summary>
            private void InheritActionBindings(Type baseState, HashSet<Type> handledEvents)
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
                    if (this.InheritableEventHandlers.ContainsKey(attr.Event))
                    {
                        continue;
                    }

                    CheckEventHandlerAlreadyInherited(attr.Event, baseState, handledEvents);

                    actionBindingsInherited.Add(attr.Event, new ActionEventHandlerDeclaration(attr.Action));
                    handledEvents.Add(attr.Event);
                }

                foreach (var kvp in actionBindingsInherited)
                {
                    this.InheritableEventHandlers.Add(kvp.Key, kvp.Value);
                }

                this.InheritActionBindings(baseState.BaseType, handledEvents);
            }

            /// <summary>
            /// Declares ignore event handlers, if there are any.
            /// </summary>
            private void InstallIgnoreHandlers(HashSet<Type> handledEvents)
            {
                HashSet<Type> ignoredEvents = new HashSet<Type>();
                if (this.GetType().GetCustomAttribute(typeof(IgnoreEventsAttribute), false) is IgnoreEventsAttribute ignoreEventsAttribute)
                {
                    foreach (var e in ignoreEventsAttribute.Events)
                    {
                        CheckEventHandlerAlreadyDeclared(e, handledEvents);

                        this.InheritableEventHandlers.Add(e, new IgnoreEventHandlerDeclaration());
                        ignoredEvents.Add(e);
                        handledEvents.Add(e);
                    }
                }

                this.InheritIgnoreHandlers(this.GetType().BaseType, handledEvents, ignoredEvents);
            }

            /// <summary>
            /// Inherits ignore event handlers from a base state, if there is one.
            /// </summary>
            private void InheritIgnoreHandlers(Type baseState, HashSet<Type> handledEvents, HashSet<Type> ignoredEvents)
            {
                if (!baseState.IsSubclassOf(typeof(State)))
                {
                    return;
                }

                if (baseState.GetCustomAttribute(typeof(IgnoreEventsAttribute), false) is IgnoreEventsAttribute ignoreEventsAttribute)
                {
                    foreach (var e in ignoreEventsAttribute.Events)
                    {
                        if (ignoredEvents.Contains(e))
                        {
                            continue;
                        }

                        CheckEventHandlerAlreadyInherited(e, baseState, handledEvents);

                        this.InheritableEventHandlers.Add(e, new IgnoreEventHandlerDeclaration());
                        ignoredEvents.Add(e);
                        handledEvents.Add(e);
                    }
                }

                this.InheritIgnoreHandlers(baseState.BaseType, handledEvents, ignoredEvents);
            }

            /// <summary>
            /// Declares defer event handlers, if there are any.
            /// </summary>
            private void InstallDeferHandlers(HashSet<Type> handledEvents)
            {
                HashSet<Type> deferredEvents = new HashSet<Type>();
                if (this.GetType().GetCustomAttribute(typeof(DeferEventsAttribute), false) is DeferEventsAttribute deferEventsAttribute)
                {
                    foreach (var e in deferEventsAttribute.Events)
                    {
                        CheckEventHandlerAlreadyDeclared(e, handledEvents);
                        this.InheritableEventHandlers.Add(e, new DeferEventHandlerDeclaration());
                        deferredEvents.Add(e);
                        handledEvents.Add(e);
                    }
                }

                this.InheritDeferHandlers(this.GetType().BaseType, handledEvents, deferredEvents);
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

                        CheckEventHandlerAlreadyInherited(e, baseState, handledEvents);
                        this.InheritableEventHandlers.Add(e, new DeferEventHandlerDeclaration());
                        deferredEvents.Add(e);
                        handledEvents.Add(e);
                    }
                }

                this.InheritDeferHandlers(baseState.BaseType, handledEvents, deferredEvents);
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
                    this.Action = actionName;
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
                    this.Action = actionName;
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
                    this.Event = eventType;
                    this.State = stateType;
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventGotoStateAttribute"/> class.
                /// </summary>
                /// <param name="eventType">The type of the dequeued event.</param>
                /// <param name="stateType">The type of the state.</param>
                /// <param name="actionName">Name of action to perform on exit.</param>
                public OnEventGotoStateAttribute(Type eventType, Type stateType, string actionName)
                {
                    this.Event = eventType;
                    this.State = stateType;
                    this.Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring a push state transition when the state machine
            /// is in the specified state and dequeues an event of the specified type.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            protected sealed class OnEventPushStateAttribute : Attribute
            {
                /// <summary>
                /// The type of the dequeued event.
                /// </summary>
                internal Type Event;

                /// <summary>
                /// The type of the state.
                /// </summary>
                internal Type State;

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventPushStateAttribute"/> class.
                /// </summary>
                /// <param name="eventType">The type of the dequeued event.</param>
                /// <param name="stateType">The type of the state.</param>
                public OnEventPushStateAttribute(Type eventType, Type stateType)
                {
                    this.Event = eventType;
                    this.State = stateType;
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
                    this.Event = eventType;
                    this.Action = actionName;
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
                    this.Events = eventTypes;
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
                    this.Events = eventTypes;
                }
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
