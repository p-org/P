// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Type that implements an actor. Inherit from this class to declare a custom actor.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/programming-models/actors/overview">Programming
    /// model: asynchronous actors</see> for more information.
    /// </remarks>
    public abstract class Actor
    {
        /// <summary>
        /// Cache of actor types to a map of event types to action declarations.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>> ActionCache =
            new ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>>();

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
        /// The runtime that executes this actor.
        /// </summary>
        internal ActorRuntime Runtime { get; private set; }

        /// <summary>
        /// Unique id that identifies this actor.
        /// </summary>
        protected internal ActorId Id { get; private set; }

        /// <summary>
        /// Manages the actor.
        /// </summary>
        internal IActorManager Manager { get; private set; }

        /// <summary>
        /// The inbox of the actor. Incoming events are enqueued here.
        /// Events are dequeued to be processed.
        /// </summary>
        private protected IEventQueue Inbox;

        /// <summary>
        /// Map from event types to cached action delegates.
        /// </summary>
        private protected readonly Dictionary<Type, CachedDelegate> ActionMap;

        /// <summary>
        /// Map that contains the active timers.
        /// </summary>
        private protected readonly Dictionary<TimerInfo, IActorTimer> Timers;

        /// <summary>
        /// The current status of the actor. It is marked volatile as
        /// the runtime can read it concurrently.
        /// </summary>
        private protected volatile Status CurrentStatus;

        /// <summary>
        /// Gets the name of the current state, if there is one.
        /// </summary>
        internal string CurrentStateName { get; private protected set; }

        /// <summary>
        /// Checks if the actor is halted.
        /// </summary>
        internal bool IsHalted => this.CurrentStatus is Status.Halted;

        /// <summary>
        /// Checks if a default handler is available.
        /// </summary>
        internal bool IsDefaultHandlerAvailable { get; private set; }

        /// <summary>
        /// Id used to identify subsequent operations performed by this actor. This value
        /// is initially either <see cref="Guid.Empty"/> or the <see cref="Guid"/> specified
        /// upon creation. This value is automatically set to the operation group id of the
        /// last dequeue or receive operation, if it is not <see cref="Guid.Empty"/>. This
        /// value can also be manually set using the property.
        /// </summary>
        protected internal virtual Guid OperationGroupId
        {
            get => this.Manager.OperationGroupId;

            set
            {
                this.Manager.OperationGroupId = value;
            }
        }

        /// <summary>
        /// The installed runtime logger.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/core/logging" >Logging</see> for more information.
        /// </remarks>
        protected TextWriter Logger => this.Runtime.Logger;

        /// <summary>
        /// User-defined hashed state of the actor. Override to improve the
        /// accuracy of stateful techniques during testing.
        /// </summary>
        protected virtual int HashedState => 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Actor"/> class.
        /// </summary>
        protected Actor()
        {
            this.ActionMap = new Dictionary<Type, CachedDelegate>();
            this.Timers = new Dictionary<TimerInfo, IActorTimer>();
            this.CurrentStatus = Status.Active;
            this.CurrentStateName = default;
            this.IsDefaultHandlerAvailable = false;
        }

        /// <summary>
        /// Configures the actor.
        /// </summary>
        internal void Configure(ActorRuntime runtime, ActorId id, IActorManager manager, IEventQueue inbox)
        {
            this.Runtime = runtime;
            this.Id = id;
            this.Manager = manager;
            this.Inbox = inbox;
        }

        /// <summary>
        /// Initializes the actor with the specified optional event.
        /// </summary>
        /// <param name="initialEvent">Optional event used for initialization.</param>
        internal virtual async Task InitializeAsync(Event initialEvent)
        {
            // Invoke the custom initializer, if there is one.
            await this.InvokeUserCallbackAsync(UserCallbackType.OnInitialize, initialEvent);
            if (this.CurrentStatus is Status.Halting)
            {
                await this.HaltAsync(initialEvent);
            }
        }

        /// <summary>
        /// Creates a new actor of the specified type and with the specified optional
        /// <see cref="Event"/>. This <see cref="Event"/> can only be used to access
        /// its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="initialEvent">Optional initialization event.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The unique actor id.</returns>
        protected ActorId CreateActor(Type type, Event initialEvent = null, Guid opGroupId = default) =>
            this.Runtime.CreateActor(null, type, null, initialEvent, this, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified type and name, and with the specified
        /// optional <see cref="Event"/>. This <see cref="Event"/> can only be used to
        /// access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <param name="initialEvent">Optional initialization event.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The unique actor id.</returns>
        protected ActorId CreateActor(Type type, string name, Event initialEvent = null, Guid opGroupId = default) =>
            this.Runtime.CreateActor(null, type, name, initialEvent, this, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, using the specified
        /// unbound actor id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="id">Unbound actor id.</param>
        /// <param name="type">Type of the actor.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <param name="initialEvent">Optional initialization event.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        protected void CreateActor(ActorId id, Type type, string name, Event initialEvent = null, Guid opGroupId = default) =>
            this.Runtime.CreateActor(id, type, name, initialEvent, this, opGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a target.
        /// </summary>
        /// <param name="id">The id of the target.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <param name="options">Optional configuration of a send operation.</param>
        protected void SendEvent(ActorId id, Event e, Guid opGroupId = default, SendOptions options = null) =>
            this.Runtime.SendEvent(id, e, this, opGroupId, options);

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified type
        /// that satisfies an optional predicate.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="predicate">The optional predicate.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> ReceiveEventAsync(Type eventType, Func<Event, bool> predicate = null)
        {
            this.Assert(this.CurrentStatus is Status.Active, "{0} invoked ReceiveEventAsync while halting.", this.Id);
            this.Runtime.NotifyReceiveCalled(this);
            return this.Inbox.ReceiveEventAsync(eventType, predicate);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types.
        /// </summary>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> ReceiveEventAsync(params Type[] eventTypes)
        {
            this.Assert(this.CurrentStatus is Status.Active, "{0} invoked ReceiveEventAsync while halting.", this.Id);
            this.Runtime.NotifyReceiveCalled(this);
            return this.Inbox.ReceiveEventAsync(eventTypes);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types
        /// that satisfy the specified predicates.
        /// </summary>
        /// <param name="events">Event types and predicates.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> ReceiveEventAsync(params Tuple<Type, Func<Event, bool>>[] events)
        {
            this.Assert(this.CurrentStatus is Status.Active, "{0} invoked ReceiveEventAsync while halting.", this.Id);
            this.Runtime.NotifyReceiveCalled(this);
            return this.Inbox.ReceiveEventAsync(events);
        }

        /// <summary>
        /// Starts a timer that sends a <see cref="TimerElapsedEvent"/> to this actor after the
        /// specified due time. The timer accepts an optional payload to be used during timeout.
        /// The timer is automatically disposed after it timeouts. To manually stop and dispose
        /// the timer, invoke the <see cref="StopTimer"/> method.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/programming-models/actors/timers">Using timers in actors</see> for more information.
        /// </remarks>
        /// <param name="startDelay">The amount of time to wait before sending the timeout event.</param>
        /// <param name="customEvent">Optional custom event to raise instead of the default TimerElapsedEvent.</param>
        /// <returns>Handle that contains information about the timer.</returns>
        protected TimerInfo StartTimer(TimeSpan startDelay, TimerElapsedEvent customEvent = null)
        {
            // The specified due time and period must be valid.
            this.Assert(startDelay.TotalMilliseconds >= 0, "{0} registered a timer with a negative due time.", this.Id);
            return this.RegisterTimer(startDelay, Timeout.InfiniteTimeSpan, customEvent);
        }

        /// <summary>
        /// Starts a periodic timer that sends a <see cref="TimerElapsedEvent"/> to this actor after
        /// the specified due time, and then repeats after each specified period. The timer accepts
        /// an optional payload to be used during timeout. The timer can be stopped by invoking the
        /// <see cref="StopTimer"/> method.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/programming-models/actors/timers">Using timers in actors</see> for more information.
        /// </remarks>
        /// <param name="startDelay">The amount of time to wait before sending the first timeout event.</param>
        /// <param name="period">The time interval between timeout events.</param>
        /// <param name="customEvent">Optional custom event to raise instead of the default TimerElapsedEvent.</param>
        /// <returns>Handle that contains information about the timer.</returns>
        protected TimerInfo StartPeriodicTimer(TimeSpan startDelay, TimeSpan period, TimerElapsedEvent customEvent = null)
        {
            // The specified due time and period must be valid.
            this.Assert(startDelay.TotalMilliseconds >= 0, "{0} registered a periodic timer with a negative due time.", this.Id);
            this.Assert(period.TotalMilliseconds >= 0, "{0} registered a periodic timer with a negative period.", this.Id);
            return this.RegisterTimer(startDelay, period, customEvent);
        }

        /// <summary>
        /// Stops and disposes the specified timer.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/programming-models/actors/timers">Using timers in actors</see> for more information.
        /// </remarks>
        /// <param name="info">Handle that contains information about the timer.</param>
        protected void StopTimer(TimerInfo info)
        {
            this.Assert(info.OwnerId == this.Id, "{0} is not allowed to dispose timer '{1}', which is owned by {2}.",
                this.Id, info, info.OwnerId);
            this.UnregisterTimer(info);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>The controlled nondeterministic choice.</returns>
        protected bool RandomBoolean() => this.Runtime.GetNondeterministicBooleanChoice(2, this.Id.Name, this.Id.Type);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate a number in the range [0..maxValue), where 0
        /// triggers true.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The controlled nondeterministic choice.</returns>
        protected bool RandomBoolean(int maxValue) =>
            this.Runtime.GetNondeterministicBooleanChoice(maxValue, this.Id.Name, this.Id.Type);

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during
        /// analysis or testing. The value is used to generate an integer in
        /// the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The controlled nondeterministic integer.</returns>
        protected int RandomInteger(int maxValue) =>
            this.Runtime.GetNondeterministicIntegerChoice(maxValue, this.Id.Name, this.Id.Type);

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <typeparam name="T">Type of the monitor.</typeparam>
        /// <param name="e">Event to send to the monitor.</param>
        protected void Monitor<T>(Event e) => this.Monitor(typeof(T), e);

        /// <summary>
        /// Invokes the specified monitor with the specified event.
        /// </summary>
        /// <param name="type">Type of the monitor.</param>
        /// <param name="e">The event to send.</param>
        protected void Monitor(Type type, Event e)
        {
            this.Assert(e != null, "{0} is sending a null event.", this.Id);
            this.Runtime.Monitor(type, e, this.Id.Name, this.Id.Type, this.CurrentStateName);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate) => this.Runtime.Assert(predicate);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, object arg0) =>
            this.Runtime.Assert(predicate, s, arg0);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, object arg0, object arg1) =>
            this.Runtime.Assert(predicate, s, arg0, arg1);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            this.Runtime.Assert(predicate, s, arg0, arg1, arg2);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, params object[] args) =>
            this.Runtime.Assert(predicate, s, args);

        /// <summary>
        /// Raises a <see cref='HaltEvent'/> to halt the actor at the end of the current action.
        /// </summary>
        protected virtual void RaiseHaltEvent()
        {
            this.Assert(this.CurrentStatus is Status.Active, "{0} invoked Halt while halting.", this.Id);
            this.CurrentStatus = Status.Halting;
        }

        /// <summary>
        /// Asynchronous callback that is invoked when the actor is initialized with an optional event.
        /// </summary>
        /// <param name="initialEvent">Optional event used for initialization.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        protected virtual Task OnInitializeAsync(Event initialEvent) => Task.CompletedTask;

        /// <summary>
        /// Asynchronous callback that is invoked when the actor successfully dequeues
        /// an event from its inbox. This method is not called when the dequeue happens
        /// via a receive statement.
        /// </summary>
        /// <param name="e">The event that was dequeued.</param>
        protected virtual Task OnEventDequeuedAsync(Event e) => Task.CompletedTask;

        /// <summary>
        /// Asynchronous callback that is invoked when the actor finishes handling a dequeued
        /// event, unless the handler of the dequeued event caused the actor to halt (either
        /// normally or due to an exception). The actor will either become idle or dequeue
        /// the next event from its inbox.
        /// </summary>
        /// <param name="e">The event that was handled.</param>
        protected virtual Task OnEventHandledAsync(Event e) => Task.CompletedTask;

        /// <summary>
        /// Asynchronous callback that is invoked when the actor receives an event that
        /// it is not prepared to handle. The callback is invoked first, after which the
        /// actor will necessarily throw an <see cref="UnhandledEventException"/>
        /// </summary>
        /// <param name="e">The event that was unhandled.</param>
        /// <param name="state">The state when the event was dequeued.</param>
        protected virtual Task OnEventUnhandledAsync(Event e, string state) => Task.CompletedTask;

        /// <summary>
        /// Asynchronous callback that is invoked when the actor handles an exception.
        /// </summary>
        /// <param name="ex">The exception thrown by the actor.</param>
        /// <param name="e">The event being handled when the exception was thrown.</param>
        /// <returns>The action that the runtime should take.</returns>
        protected virtual Task OnExceptionHandledAsync(Exception ex, Event e) => Task.CompletedTask;

        /// <summary>
        /// Asynchronous callback that is invoked when the actor halts.
        /// </summary>
        /// <param name="e">The event being handled when the actor halted.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        protected virtual Task OnHaltAsync(Event e) => Task.CompletedTask;

        /// <summary>
        /// Enqueues the specified event and its metadata.
        /// </summary>
        internal EnqueueStatus Enqueue(Event e, Guid opGroupId, EventInfo info)
        {
            if (this.CurrentStatus is Status.Halted)
            {
                return EnqueueStatus.Dropped;
            }

            return this.Inbox.Enqueue(e, opGroupId, info);
        }

        /// <summary>
        /// Runs the event handler. The handler terminates if there is no next
        /// event to process or if the actor has halted.
        /// </summary>
        internal async Task RunEventHandlerAsync()
        {
            Event lastDequeuedEvent = null;
            while (this.CurrentStatus != Status.Halted && this.Runtime.IsRunning)
            {
                (DequeueStatus status, Event e, Guid opGroupId, EventInfo info) = this.Inbox.Dequeue();
                if (opGroupId != Guid.Empty)
                {
                    // Inherit the operation group id of the dequeued operation, if it is non-empty.
                    this.Manager.OperationGroupId = opGroupId;
                }

                if (status is DequeueStatus.Success)
                {
                    // Notify the runtime for a new event to handle. This is only used
                    // during bug-finding and operation bounding, because the runtime
                    // has to schedule an actor when a new operation is dequeued.
                    this.Runtime.NotifyDequeuedEvent(this, e, info);
                    await this.InvokeUserCallbackAsync(UserCallbackType.OnEventDequeued, e);
                    lastDequeuedEvent = e;
                }
                else if (status is DequeueStatus.Raised)
                {
                    // Only supported by types (e.g. StateMachine) that allow
                    // the user to explicitly raise events.
                    this.Runtime.NotifyHandleRaisedEvent(this, e);
                }
                else if (status is DequeueStatus.Default)
                {
                    this.Runtime.LogWriter.LogDefaultEventHandler(this.Id, this.CurrentStateName);

                    // If the default event was dequeued, then notify the runtime.
                    // This is only used during bug-finding, because the runtime must
                    // instrument a scheduling point between default event handlers.
                    this.Runtime.NotifyDefaultEventDequeued(this);
                }
                else if (status is DequeueStatus.NotAvailable)
                {
                    // Terminate the handler as there is no event available.
                    break;
                }

                if (e is TimerElapsedEvent timeoutEvent &&
                    timeoutEvent.Info.Period.TotalMilliseconds < 0)
                {
                    // If the timer is not periodic, then dispose it.
                    this.UnregisterTimer(timeoutEvent.Info);
                }

                if (this.CurrentStatus is Status.Active)
                {
                    // Handles the next event, if the actor is not halted.
                    await this.HandleEventAsync(e);
                }

                if (!this.Inbox.IsEventRaised && lastDequeuedEvent != null && this.CurrentStatus != Status.Halted)
                {
                    // Inform the user that the actor handled the dequeued event.
                    await this.InvokeUserCallbackAsync(UserCallbackType.OnEventHandled, lastDequeuedEvent);
                    lastDequeuedEvent = null;
                }

                if (this.CurrentStatus is Status.Halting)
                {
                    // If the current status is halting, then halt the actor.
                    await this.HaltAsync(e);
                }
            }
        }

        /// <summary>
        /// Handles the specified <see cref="Event"/>.
        /// </summary>
        private protected virtual async Task HandleEventAsync(Event e)
        {
            if (this.ActionMap.TryGetValue(e.GetType(), out CachedDelegate cachedAction) ||
                this.ActionMap.TryGetValue(typeof(WildCardEvent), out cachedAction))
            {
                this.Runtime.NotifyInvokedAction(this, cachedAction.MethodInfo, null, null, e);
                await this.InvokeActionAsync(cachedAction, e);
            }
            else if (e is HaltEvent)
            {
                // If it is the halt event, then change the actor status to halting.
                this.CurrentStatus = Status.Halting;
            }
            else
            {
                await this.InvokeUserCallbackAsync(UserCallbackType.OnEventUnhandled, e);
                if (this.CurrentStatus is Status.Active)
                {
                    // If the event cannot be handled then report an error, else halt gracefully.
                    var ex = new UnhandledEventException(e, default, "Unhandled Event");
                    bool isHalting = this.OnUnhandledEventExceptionHandler(ex, e);
                    this.Assert(isHalting, "{0} received event '{1}' that cannot be handled.",
                        this.Id, e.GetType().FullName);
                }
            }
        }

        /// <summary>
        /// Invokes the specified action delegate.
        /// </summary>
        private protected async Task InvokeActionAsync(CachedDelegate cachedAction, Event e)
        {
            try
            {
                if (cachedAction.IsAsync)
                {
                    Task task = null;
                    if (cachedAction.Handler is Func<Event, Task> taskFuncWithEvent)
                    {
                        task = taskFuncWithEvent(e);
                    }
                    else if (cachedAction.Handler is Func<Task> taskFunc)
                    {
                        task = taskFunc();
                    }

                    this.Runtime.NotifyWaitTask(this, task);

                    // We have no reliable stack for awaited operations.
                    await task;
                }
                else if (cachedAction.Handler is Action<Event> actionWithEvent)
                {
                    actionWithEvent(e);
                }
                else if (cachedAction.Handler is Action action)
                {
                    action();
                }
            }
            catch (Exception ex) when (this.OnExceptionHandler(ex, cachedAction.MethodInfo.Name, e))
            {
                // User handled the exception.
                await this.OnExceptionHandledAsync(ex, e);
            }
            catch (Exception ex) when (!cachedAction.IsAsync && this.InvokeOnFailureExceptionFilter(cachedAction, ex))
            {
                // Use an exception filter to call OnFailure before the stack
                // has been unwound. If the exception filter does not fail-fast,
                // it returns false to process the exception normally.
            }
            catch (Exception ex)
            {
                await this.TryHandleActionInvocationExceptionAsync(ex, cachedAction.MethodInfo.Name);
            }
        }

        /// <summary>
        /// Invokes the specified event handler user callback.
        /// </summary>
        private protected async Task InvokeUserCallbackAsync(string callbackType, Event e, string currentState = default)
        {
            try
            {
                Task task = null;
                if (callbackType is UserCallbackType.OnInitialize)
                {
                    task = this.OnInitializeAsync(e);
                }
                else if (callbackType is UserCallbackType.OnEventDequeued)
                {
                    task = this.OnEventDequeuedAsync(e);
                }
                else if (callbackType is UserCallbackType.OnEventHandled)
                {
                    task = this.OnEventHandledAsync(e);
                }
                else if (callbackType is UserCallbackType.OnEventUnhandled)
                {
                    task = this.OnEventUnhandledAsync(e, currentState);
                }

                this.Runtime.NotifyWaitTask(this, task);
                await task;
            }
            catch (Exception ex) when (this.OnExceptionHandler(ex, callbackType, e))
            {
                // User handled the exception.
                await this.OnExceptionHandledAsync(ex, e);
            }
            catch (Exception ex)
            {
                // Reports the unhandled exception.
                await this.TryHandleActionInvocationExceptionAsync(ex, callbackType);
            }
        }

        /// <summary>
        /// An exception filter that calls <see cref="CoyoteRuntime.OnFailure"/>,
        /// which can choose to fast-fail the app to get a full dump.
        /// </summary>
        /// <param name="action">The action being executed when the failure occurred.</param>
        /// <param name="ex">The exception being tested.</param>
        private protected bool InvokeOnFailureExceptionFilter(CachedDelegate action, Exception ex)
        {
            // This is called within the exception filter so the stack has not yet been unwound.
            // If the call does not fail-fast, return false to process the exception normally.
            this.Runtime.RaiseOnFailureEvent(new ActionExceptionFilterException(action.MethodInfo.Name, ex));
            return false;
        }

        /// <summary>
        /// Tries to handle an exception thrown during an action invocation.
        /// </summary>
        private protected Task TryHandleActionInvocationExceptionAsync(Exception ex, string actionName)
        {
            Exception innerException = ex;
            while (innerException is TargetInvocationException)
            {
                innerException = innerException.InnerException;
            }

            if (innerException is AggregateException)
            {
                innerException = innerException.InnerException;
            }

            if (innerException is ExecutionCanceledException || innerException is TaskSchedulerException)
            {
                this.CurrentStatus = Status.Halted;
                Debug.WriteLine($"<Exception> {innerException.GetType().Name} was thrown from {this.Id}.");
            }
            else
            {
                // Reports the unhandled exception.
                this.ReportUnhandledException(innerException, actionName);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks if the specified event is ignored.
        /// </summary>
        internal bool IsEventIgnored(Event e)
        {
            if (e is TimerElapsedEvent timeoutEvent && !this.Timers.ContainsKey(timeoutEvent.Info))
            {
                // The timer that created this timeout event is not active.
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the hashed state of this actor.
        /// </summary>
        internal virtual int GetHashedState()
        {
            unchecked
            {
                var hash = 19;
                hash = (hash * 31) + this.GetType().GetHashCode();
                hash = (hash * 31) + this.Id.Value.GetHashCode();
                hash = (hash * 31) + this.IsHalted.GetHashCode();

                hash = (hash * 31) + this.Manager.GetCachedState();
                hash = (hash * 31) + this.Inbox.GetCachedState();

                // Adds the user-defined hashed state.
                hash = (hash * 31) + this.HashedState;

                return hash;
            }
        }

        /// <summary>
        /// Registers a new timer using the specified configuration.
        /// </summary>
        private protected TimerInfo RegisterTimer(TimeSpan dueTime, TimeSpan period, TimerElapsedEvent customEvent)
        {
            var info = new TimerInfo(this.Id, dueTime, period, customEvent);
            var timer = this.Runtime.CreateActorTimer(info, this);
            this.Runtime.LogWriter.LogCreateTimer(info);
            this.Timers.Add(info, timer);
            return info;
        }

        /// <summary>
        /// Unregisters the specified timer.
        /// </summary>
        private protected void UnregisterTimer(TimerInfo info)
        {
            if (!this.Timers.TryGetValue(info, out IActorTimer timer))
            {
                this.Assert(info.OwnerId == this.Id, "Timer '{0}' is already disposed.", info);
            }

            this.Runtime.LogWriter.LogStopTimer(info);
            this.Timers.Remove(info);
            using (timer)
            {
                // sometimes timer can be null.
            }
        }

        /// <summary>
        /// Extracts user declarations and sets up the event handlers.
        /// </summary>
        internal virtual void SetupEventHandlers()
        {
            if (!ActionCache.ContainsKey(this.GetType()))
            {
                Stack<Type> actorTypes = new Stack<Type>();
                for (var actorType = this.GetType(); typeof(Actor).IsAssignableFrom(actorType); actorType = actorType.BaseType)
                {
                    actorTypes.Push(actorType);
                }

                // process base types in reverse order, so mosts derrived type is cached first.
                while (actorTypes.Count > 0)
                {
                    this.SetupEventHandlers(actorTypes.Pop());
                }
            }

            // Now we have all derrived types cached, we can build the combined action map for this type.
            for (var actorType = this.GetType(); typeof(Actor).IsAssignableFrom(actorType); actorType = actorType.BaseType)
            {
                // Populates the map of event handlers for this actor instance.
                foreach (var kvp in ActionCache[actorType])
                {
                    // use the most derrived action handler for a given event (ignoring any base handlers defined for the same event).
                    if (!this.ActionMap.ContainsKey(kvp.Key))
                    {
                        // MethodInfo.Invoke catches the exception to wrap it in a TargetInvocationException.
                        // This unwinds the stack before the ExecuteAction exception filter is invoked, so
                        // call through a delegate instead (which is also much faster than Invoke).
                        this.ActionMap.Add(kvp.Key, new CachedDelegate(kvp.Value, this));
                    }
                }
            }
        }

        private void SetupEventHandlers(Type actorType)
        {
            // If this type has not already been setup in the ActionCache, then we need to try and grab the ActionCacheLock
            // for this type.  First make sure we have one and only one lockable object for this type.
            object syncObject = ActionCacheLocks.GetOrAdd(actorType, _ => new object());

            // Locking this syncObject ensures only one thread enters the initialization code to update
            // the ActionCache for this specific Actor type.
            lock (syncObject)
            {
                if (ActionCache.ContainsKey(actorType))
                {
                    // Note: even if we won the GetOrAdd, there is a tiny window of opportunity for another thread
                    // to slip in and lock the syncObject before us, so we have to check the ActionCache again
                    // here just in case.
                }
                else
                {
                    // Events with already declared handlers.
                    var handledEvents = new HashSet<Type>();

                    // Map containing all action bindings.
                    var actionBindings = new Dictionary<Type, ActionEventHandlerDeclaration>();
                    var doAttributes = actorType.GetCustomAttributes(typeof(OnEventDoActionAttribute), false)
                        as OnEventDoActionAttribute[];

                    foreach (var attr in doAttributes)
                    {
                        this.Assert(!handledEvents.Contains(attr.Event),
                            "{0} declared multiple handlers for event '{1}'.",
                            actorType.FullName, attr.Event);
                        actionBindings.Add(attr.Event, new ActionEventHandlerDeclaration(attr.Action));
                        handledEvents.Add(attr.Event);
                    }

                    var map = new Dictionary<Type, MethodInfo>();
                    foreach (var action in actionBindings)
                    {
                        if (!map.ContainsKey(action.Key))
                        {
                            map.Add(action.Key, this.GetActionWithName(action.Value.Name));
                        }
                    }

                    // Caches the action declarations for this actor type.
                    ActionCache.TryAdd(actorType, map);
                }
            }
        }

        /// <summary>
        /// Returns the set of all registered events (for code coverage).
        /// It does not include events that are deferred or ignored.
        /// </summary>
        internal HashSet<string> GetAllRegisteredEvents()
        {
            return new HashSet<string>(from key in this.ActionMap.Keys select key.FullName);
        }

        /// <summary>
        /// Returns the action with the specified name.
        /// </summary>
        private protected MethodInfo GetActionWithName(string actionName)
        {
            MethodInfo action;
            Type actorType = this.GetType();

            do
            {
                BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy;
                action = actorType.GetMethod(actionName, bindingFlags, Type.DefaultBinder, SingleEventTypeArray, null);
                if (action is null)
                {
                    action = actorType.GetMethod(actionName, bindingFlags, Type.DefaultBinder, Array.Empty<Type>(), null);
                }

                actorType = actorType.BaseType;
            }
            while (action is null && actorType != typeof(StateMachine) && actorType != typeof(Actor));

            this.Assert(action != null, "Cannot detect action declaration '{0}' in '{1}'.", actionName, this.GetType().FullName);
            this.AssertActionValidity(action);
            return action;
        }

        /// <summary>
        /// Checks the validity of the specified action.
        /// </summary>
        private void AssertActionValidity(MethodInfo action)
        {
            Type actionType = action.DeclaringType;
            ParameterInfo[] parameters = action.GetParameters();
            this.Assert(parameters.Length is 0 ||
                (parameters.Length is 1 && parameters[0].ParameterType == typeof(Event)),
                "Action '{0}' in '{1}' must either accept no parameters or a single parameter of type 'Event'.",
                action.Name, actionType.Name);

            // Check if the action is an 'async' method.
            if (action.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
            {
                this.Assert(action.ReturnType == typeof(Task),
                    "Async action '{0}' in '{1}' must have 'Task' return type.",
                    action.Name, actionType.Name);
            }
            else
            {
                this.Assert(action.ReturnType == typeof(void),
                    "Action '{0}' in '{1}' must have 'void' return type.",
                    action.Name, actionType.Name);
            }
        }

        /// <summary>
        /// Returns the formatted strint to be used with a fair nondeterministic boolean choice.
        /// </summary>
        private protected virtual string FormatFairRandom(string callerMemberName, string callerFilePath, int callerLineNumber) =>
            string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}",
                this.Id.Name, callerMemberName, callerFilePath, callerLineNumber.ToString());

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        private protected virtual void ReportUnhandledException(Exception ex, string actionName)
        {
            this.Runtime.WrapAndThrowException(ex, $"{0} (action '{1}')", this.Id, actionName);
        }

        /// <summary>
        /// Invokes user callback when the actor throws an exception.
        /// </summary>
        /// <param name="ex">The exception thrown by the actor.</param>
        /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        /// <param name="e">The event being handled when the exception was thrown.</param>
        /// <returns>True if the exception was handled, else false if it should continue to get thrown.</returns>
        private protected bool OnExceptionHandler(Exception ex, string methodName, Event e)
        {
            if (ex is ExecutionCanceledException)
            {
                // Internal exception used during testing.
                return false;
            }

            this.Runtime.LogWriter.LogExceptionThrown(this.Id, this.CurrentStateName, methodName, ex);

            OnExceptionOutcome outcome = this.OnException(ex, methodName, e);
            if (outcome is OnExceptionOutcome.ThrowException)
            {
                return false;
            }
            else if (outcome is OnExceptionOutcome.Halt)
            {
                this.CurrentStatus = Status.Halting;
            }

            this.Runtime.LogWriter.LogExceptionHandled(this.Id, this.CurrentStateName, methodName, ex);
            return true;
        }

        /// <summary>
        /// Invokes user callback when the actor receives an event that it cannot handle.
        /// </summary>
        /// <param name="ex">The exception thrown by the actor.</param>
        /// <param name="e">The unhandled event.</param>
        /// <returns>True if the the actor should gracefully halt, else false if the exception
        /// should continue to get thrown.</returns>
        private protected bool OnUnhandledEventExceptionHandler(UnhandledEventException ex, Event e)
        {
            this.Runtime.LogWriter.LogExceptionThrown(this.Id, ex.CurrentStateName, string.Empty, ex);

            OnExceptionOutcome outcome = this.OnException(ex, string.Empty, e);
            if (outcome is OnExceptionOutcome.ThrowException)
            {
                return false;
            }

            this.CurrentStatus = Status.Halting;
            this.Runtime.LogWriter.LogExceptionHandled(this.Id, ex.CurrentStateName, string.Empty, ex);
            return true;
        }

        /// <summary>
        /// User callback when the actor throws an exception. By default,
        /// the actor throws the exception causing the runtime to fail.
        /// </summary>
        /// <param name="ex">The exception thrown by the actor.</param>
        /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        /// <param name="e">The event being handled when the exception was thrown.</param>
        /// <returns>The action that the runtime should take.</returns>
        protected virtual OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
        {
            return OnExceptionOutcome.ThrowException;
        }

        /// <summary>
        /// Halts the actor.
        /// </summary>
        /// <param name="e">The event being handled when the actor halts.</param>
        private protected Task HaltAsync(Event e)
        {
            this.CurrentStatus = Status.Halted;

            // Close the inbox, which will stop any subsequent enqueues.
            this.Inbox.Close();

            this.Runtime.LogWriter.LogHalt(this.Id, this.Inbox.Size);

            // Dispose any held resources.
            this.Inbox.Dispose();
            foreach (var timer in this.Timers.Keys.ToList())
            {
                this.UnregisterTimer(timer);
            }

            // Invoke user callback.
            return this.OnHaltAsync(e);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Actor m &&
                this.GetType() == m.GetType())
            {
                return this.Id.Value == m.Id.Value;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Id.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current actor.
        /// </summary>
        public override string ToString()
        {
            return this.Id.Name;
        }

        /// <summary>
        /// The status of the actor.
        /// </summary>
        private protected enum Status
        {
            /// <summary>
            /// The actor is active.
            /// </summary>
            Active = 0,

            /// <summary>
            /// The actor is halting.
            /// </summary>
            Halting,

            /// <summary>
            /// The actor is halted.
            /// </summary>
            Halted
        }

        /// <summary>
        /// The type of a user callback.
        /// </summary>
        private protected static class UserCallbackType
        {
            internal const string OnInitialize = nameof(Actor.OnInitializeAsync);
            internal const string OnEventDequeued = nameof(Actor.OnEventDequeuedAsync);
            internal const string OnEventHandled = nameof(Actor.OnEventHandledAsync);
            internal const string OnEventUnhandled = nameof(Actor.OnEventUnhandledAsync);
            internal const string OnExceptionHandled = nameof(Actor.OnExceptionHandledAsync);
            internal const string OnHalt = nameof(Actor.OnHaltAsync);
        }

        /// <summary>
        /// Attribute for declaring which action should be invoked
        /// to handle a dequeued event of the specified type.
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
    }
}
