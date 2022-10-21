// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime;
using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Runtime for creating and executing actors.
    /// </summary>
    internal class ActorRuntime : CoyoteRuntime, IActorRuntime
    {
        /// <summary>
        /// Map from unique actor ids to actors.
        /// </summary>
        private readonly ConcurrentDictionary<ActorId, Actor> ActorMap;

        /// <summary>
        /// Responsible for writing to all registered <see cref="IActorRuntimeLog"/> objects.
        /// </summary>
        protected internal LogWriter LogWriter { get; private set; }

        /// <summary>
        /// Used to log text messages. Use <see cref="ICoyoteRuntime.SetLogger"/>
        /// to replace the logger with a custom one.
        /// </summary>
        public override TextWriter Logger => this.LogWriter.Logger;

        /// <summary>
        /// Callback that is fired when a Coyote event is dropped.
        /// </summary>
        public event OnEventDroppedHandler OnEventDropped;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRuntime"/> class.
        /// </summary>
        internal ActorRuntime(Configuration configuration, IRandomValueGenerator valueGenerator)
            : base(configuration, valueGenerator)
        {
            this.ActorMap = new ConcurrentDictionary<ActorId, Actor>();
            this.LogWriter = new LogWriter(configuration);
        }

        /// <summary>
        /// Creates a fresh actor id that has not yet been bound to any actor.
        /// </summary>
        public ActorId CreateActorId(Type type, string name = null) => new ActorId(type, name, this);

        /// <summary>
        /// Creates a actor id that is uniquely tied to the specified unique name. The
        /// returned actor id can either be a fresh id (not yet bound to any actor), or
        /// it can be bound to a previously created actor. In the second case, this actor
        /// id can be directly used to communicate with the corresponding actor.
        /// </summary>
        public virtual ActorId CreateActorIdFromName(Type type, string name) => new ActorId(type, name, this, true);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled.
        /// </summary>
        public virtual ActorId CreateActor(Type type, Event initialEvent = null, Guid opGroupId = default) =>
            this.CreateActor(null, type, null, initialEvent, null, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled.
        /// </summary>
        public virtual ActorId CreateActor(Type type, string name, Event initialEvent = null, Guid opGroupId = default) =>
            this.CreateActor(null, type, name, initialEvent, null, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified type, using the specified <see cref="ActorId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new actor, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public virtual ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, Guid opGroupId = default) =>
            this.CreateActor(id, type, null, initialEvent, null, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled. The method returns only when the actor is initialized and
        /// the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null, Guid opGroupId = default) =>
            this.CreateActorAndExecuteAsync(null, type, null, initialEvent, null, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled. The method returns only when the actor is
        /// initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null,
            Guid opGroupId = default) =>
            this.CreateActorAndExecuteAsync(null, type, name, initialEvent, null, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>, using the specified unbound
        /// actor id, and passes the specified optional <see cref="Event"/>. This event can only
        /// be used to access its payload, and cannot be handled. The method returns only when
        /// the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event initialEvent = null,
            Guid opGroupId = default) =>
            this.CreateActorAndExecuteAsync(id, type, null, initialEvent, null, opGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        public virtual void SendEvent(ActorId targetId, Event initialEvent, Guid opGroupId = default,
            SendOptions options = null) =>
            this.SendEvent(targetId, initialEvent, null, opGroupId, options);

        /// <summary>
        /// Sends an <see cref="Event"/> to an actor. Returns immediately if the target was already
        /// running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        public virtual Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event initialEvent,
            Guid opGroupId = default, SendOptions options = null) =>
            this.SendEventAndExecuteAsync(targetId, initialEvent, null, opGroupId, options);

        /// <summary>
        /// Returns the operation group id of the actor with the specified id. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="ActorId"/> is not associated with this runtime. During
        /// testing, the runtime asserts that the specified actor is currently executing.
        /// </summary>
        public virtual Guid GetCurrentOperationGroupId(ActorId currentActorId)
        {
            Actor actor = this.GetActorWithId<Actor>(currentActorId);
            return actor is null ? Guid.Empty : actor.OperationGroupId;
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal virtual ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent,
            Actor creator, Guid opGroupId)
        {
            Actor actor = this.CreateActor(id, type, name, creator, opGroupId);
            if (actor is StateMachine)
            {
                this.LogWriter.LogCreateStateMachine(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                this.LogWriter.LogCreateActor(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }

            this.RunActorEventHandler(actor, initialEvent, true);
            return actor.Id;
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>. The method
        /// returns only when the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        internal virtual async Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name,
            Event initialEvent, Actor creator, Guid opGroupId)
        {
            Actor actor = this.CreateActor(id, type, name, creator, opGroupId);
            if (actor is StateMachine)
            {
                this.LogWriter.LogCreateStateMachine(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                this.LogWriter.LogCreateActor(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }

            await this.RunActorEventHandlerAsync(actor, initialEvent, true);
            return actor.Id;
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
        /// </summary>
        private Actor CreateActor(ActorId id, Type type, string name, Actor creator, Guid opGroupId)
        {
            if (!type.IsSubclassOf(typeof(Actor)))
            {
                this.Assert(false, "Type '{0}' is not an actor.", type.FullName);
            }

            if (id is null)
            {
                id = new ActorId(type, name, this);
            }
            else if (id.Runtime != null && id.Runtime != this)
            {
                this.Assert(false, "Unbound actor id '{0}' was created by another runtime.", id.Value);
            }
            else if (id.Type != type.FullName)
            {
                this.Assert(false, "Cannot bind actor id '{0}' of type '{1}' to an actor of type '{2}'.",
                    id.Value, id.Type, type.FullName);
            }
            else
            {
                id.Bind(this);
            }

            // The operation group id of the actor is set using the following precedence:
            // (1) To the specified actor creation operation group id, if it is non-empty.
            // (2) To the operation group id of the creator actor, if it exists.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && creator != null)
            {
                opGroupId = creator.OperationGroupId;
            }

            Actor actor = ActorFactory.Create(type);
            IActorManager actorManager;
            if (actor is StateMachine stateMachine)
            {
                actorManager = new StateMachineManager(this, stateMachine, opGroupId);
            }
            else
            {
                actorManager = new ActorManager(this, actor, opGroupId);
            }

            IEventQueue eventQueue = new EventQueue(actorManager);
            actor.Configure(this, id, actorManager, eventQueue);
            actor.SetupEventHandlers();

            if (!this.ActorMap.TryAdd(id, actor))
            {
                string info = "This typically occurs if either the actor id was created by another runtime instance, " +
                    "or if a actor id from a previous runtime generation was deserialized, but the current runtime " +
                    "has not increased its generation value.";
                this.Assert(false, "An actor with id '{0}' was already created in generation '{1}'. {2}", id.Value, id.Generation, info);
            }

            return actor;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        internal virtual void SendEvent(ActorId targetId, Event e, Actor sender, Guid opGroupId, SendOptions options)
        {
            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, opGroupId, out Actor target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunActorEventHandler(target, null, false);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor. Returns immediately if the target was
        /// already running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        internal virtual async Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Actor sender,
            Guid opGroupId, SendOptions options)
        {
            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, opGroupId, out Actor target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                await this.RunActorEventHandlerAsync(target, null, false);
                return true;
            }

            return enqueueStatus is EnqueueStatus.Dropped;
        }

        /// <summary>
        /// Enqueues an event to the actor with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(ActorId targetId, Event e, Actor sender, Guid opGroupId, out Actor target)
        {
            if (e is null)
            {
                string message = sender != null ?
                    string.Format("{0} is sending a null event.", sender.Id.ToString()) :
                    "Cannot send a null event.";
                this.Assert(false, message);
            }

            if (targetId is null)
            {
                string message = (sender != null) ?
                    string.Format("{0} is sending event {1} to a null actor.", sender.Id.ToString(), e.ToString())
                    : string.Format("Cannot send event {0} to a null actor.", e.ToString());

                this.Assert(false, message);
            }

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified send operation group id, if it is non-empty.
            // (2) To the operation group id of the sender actor, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && sender != null)
            {
                opGroupId = sender.OperationGroupId;
            }

            target = this.GetActorWithId<Actor>(targetId);
            if (target is null || target.IsHalted)
            {
                this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: true);
                this.TryHandleDroppedEvent(e, targetId);
                return EnqueueStatus.Dropped;
            }

            this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: false);

            EnqueueStatus enqueueStatus = target.Enqueue(e, opGroupId, null);
            if (enqueueStatus == EnqueueStatus.Dropped)
            {
                this.TryHandleDroppedEvent(e, targetId);
            }

            return enqueueStatus;
        }

        /// <summary>
        /// Runs a new asynchronous actor event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        private void RunActorEventHandler(Actor actor, Event initialEvent, bool isFresh)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await actor.InitializeAsync(initialEvent);
                    }

                    await actor.RunEventHandlerAsync();
                }
                catch (Exception ex)
                {
                    this.IsRunning = false;
                    this.RaiseOnFailureEvent(ex);
                }
                finally
                {
                    if (actor.IsHalted)
                    {
                        this.ActorMap.TryRemove(actor.Id, out Actor _);
                    }
                }
            });
        }

        /// <summary>
        /// Runs a new asynchronous actor event handler.
        /// </summary>
        private async Task RunActorEventHandlerAsync(Actor actor, Event initialEvent, bool isFresh)
        {
            try
            {
                if (isFresh)
                {
                    await actor.InitializeAsync(initialEvent);
                }

                await actor.RunEventHandlerAsync();
            }
            catch (Exception ex)
            {
                this.IsRunning = false;
                this.RaiseOnFailureEvent(ex);
                return;
            }
        }

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner actor.
        /// </summary>
        internal virtual IActorTimer CreateActorTimer(TimerInfo info, Actor owner) => new ActorTimer(info, owner);

        /// <inheritdoc/>
        internal override void TryCreateMonitor(Type type)
        {
            // Check if monitors are enabled in production.
            if (!this.Configuration.IsMonitoringEnabledInInProduction)
            {
                return;
            }

            lock (this.Monitors)
            {
                if (this.Monitors.Any(m => m.GetType() == type))
                {
                    // Idempotence: only one monitor per type can exist.
                    return;
                }
            }

            this.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass of Monitor.", type.FullName);

            Monitor monitor = (Monitor)Activator.CreateInstance(type);
            monitor.Initialize(this);
            monitor.InitializeStateInformation();

            lock (this.Monitors)
            {
                this.Monitors.Add(monitor);
            }

            this.LogWriter.LogCreateMonitor(type.FullName);

            monitor.GotoStartState();
        }

        /// <inheritdoc/>
        internal override bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType)
        {
            bool result = false;
            if (this.ValueGenerator.Next(maxValue) == 0)
            {
                result = true;
            }

            this.LogWriter.LogRandom(result, callerName, callerType);
            return result;
        }

        /// <inheritdoc/>
        internal override int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
        {
            var result = this.ValueGenerator.Next(maxValue);
            this.LogWriter.LogRandom(result, callerName, callerType);
            return result;
        }

        /// <summary>
        /// Gets the actor of type <typeparamref name="TActor"/> with the specified id,
        /// or null if no such actor exists.
        /// </summary>
        private TActor GetActorWithId<TActor>(ActorId id)
            where TActor : Actor =>
            id != null && this.ActorMap.TryGetValue(id, out Actor value) &&
            value is TActor actor ? actor : null;

        /// <summary>
        /// Notifies that an actor invoked an action.
        /// </summary>
        internal virtual void NotifyInvokedAction(Actor actor, MethodInfo action, string handlingStateName,
            string currentStateName, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogExecuteAction(actor.Id, handlingStateName, currentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that an actor dequeued an <see cref="Event"/>.
        /// </summary>
        internal virtual void NotifyDequeuedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                this.LogWriter.LogDequeueEvent(actor.Id, stateName, e);
            }
        }

        /// <summary>
        /// Notifies that an actor dequeued the default <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDefaultEventDequeued(Actor actor)
        {
        }

        /// <summary>
        /// Notifies that the inbox of the specified actor is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDefaultEventHandlerCheck(Actor actor)
        {
        }

        /// <summary>
        /// Notifies that an actor raised an <see cref="Event"/>.
        /// </summary>
        internal virtual void NotifyRaisedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                this.LogWriter.LogRaiseEvent(actor.Id, stateName, e);
            }
        }

        /// <summary>
        /// Notifies that an actor is handling a raised <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyHandleRaisedEvent(Actor actor, Event e)
        {
        }

        /// <summary>
        /// Notifies that an actor called <see cref="Actor.ReceiveEventAsync(Type[])"/>
        /// or one of its overloaded methods.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyReceiveCalled(Actor actor)
        {
        }

        /// <summary>
        /// Notifies that an actor enqueued an event that it was waiting to receive.
        /// </summary>
        internal virtual void NotifyReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: true);
            }
        }

        /// <summary>
        /// Notifies that an actor received an event without waiting because the event
        /// was already in the inbox when the actor invoked the receive statement.
        /// </summary>
        internal virtual void NotifyReceivedEventWithoutWaiting(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: false);
            }
        }

        /// <summary>
        /// Notifies that an actor is waiting for the specified task to complete.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyWaitTask(Actor actor, Task task)
        {
        }

        /// <summary>
        /// Notifies that an actor is waiting to receive an event of one of the specified types.
        /// </summary>
        internal virtual void NotifyWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                var eventWaitTypesArray = eventTypes.ToArray();
                if (eventWaitTypesArray.Length == 1)
                {
                    this.LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray[0]);
                }
                else
                {
                    this.LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray);
                }
            }
        }

        /// <summary>
        /// Notifies that a state machine entered a state.
        /// </summary>
        internal virtual void NotifyEnteredState(StateMachine stateMachine)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: true);
            }
        }

        /// <summary>
        /// Notifies that a state machine exited a state.
        /// </summary>
        internal virtual void NotifyExitedState(StateMachine stateMachine)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: false);
            }
        }

        /// <summary>
        /// Notifies that a state machine invoked pop.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyPopState(StateMachine stateMachine)
        {
        }

        /// <summary>
        /// Notifies that a state machine invoked an action.
        /// </summary>
        internal virtual void NotifyInvokedOnEntryAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogExecuteAction(stateMachine.Id, stateMachine.CurrentStateName,
                    stateMachine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that a state machine invoked an action.
        /// </summary>
        internal virtual void NotifyInvokedOnExitAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogExecuteAction(stateMachine.Id, stateMachine.CurrentStateName,
                    stateMachine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        internal virtual void NotifyEnteredState(Monitor monitor)
        {
            if (this.Configuration.IsVerbose)
            {
                string monitorState = monitor.CurrentStateNameWithTemperature;
                this.LogWriter.LogMonitorStateTransition(monitor.GetType().FullName, monitorState,
                    true, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        internal virtual void NotifyExitedState(Monitor monitor)
        {
            if (this.Configuration.IsVerbose)
            {
                string monitorState = monitor.CurrentStateNameWithTemperature;
                this.LogWriter.LogMonitorStateTransition(monitor.GetType().FullName, monitorState,
                    false, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        internal virtual void NotifyInvokedAction(Monitor monitor, MethodInfo action, string stateName, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogMonitorExecuteAction(monitor.GetType().FullName, action.Name, stateName);
            }
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        internal virtual void NotifyRaisedEvent(Monitor monitor, Event e)
        {
            if (this.Configuration.IsVerbose)
            {
                string monitorState = monitor.CurrentStateNameWithTemperature;
                this.LogWriter.LogMonitorRaiseEvent(monitor.GetType().FullName, monitorState, e);
            }
        }

        /// <summary>
        /// Notifies that a monitor found an error.
        /// </summary>
        internal void NotifyMonitorError(Monitor monitor)
        {
            if (this.Configuration.IsVerbose)
            {
                string monitorState = monitor.CurrentStateNameWithTemperature;
                this.LogWriter.LogMonitorError(monitor.GetType().FullName, monitorState, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Tries to handle the specified dropped <see cref="Event"/>.
        /// </summary>
        internal void TryHandleDroppedEvent(Event e, ActorId id) => this.OnEventDropped?.Invoke(e, id);

        /// <inheritdoc/>
        public override TextWriter SetLogger(TextWriter logger) => this.LogWriter.SetLogger(logger);

        /// <summary>
        /// Use this method to register an <see cref="IActorRuntimeLog"/>.
        /// </summary>
        public void RegisterLog(IActorRuntimeLog log) => this.LogWriter.RegisterLog(log);

        /// <summary>
        /// Use this method to unregister a previously registered <see cref="IActorRuntimeLog"/>.
        /// </summary>
        public void RemoveLog(IActorRuntimeLog log) => this.LogWriter.RemoveLog(log);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Monitors.Clear();
                this.ActorMap.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
