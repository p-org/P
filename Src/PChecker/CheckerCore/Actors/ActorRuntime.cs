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
using PChecker.Actors.EventQueues;
using PChecker.Actors.Events;
using PChecker.Actors.Exceptions;
using PChecker.Actors.Logging;
using PChecker.Actors.Managers;
using PChecker.Random;
using PChecker.Runtime;
using PChecker.Specifications.Monitors;
using EventInfo = PChecker.Actors.Events.EventInfo;

namespace PChecker.Actors
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
        public override TextWriter Logger => LogWriter.Logger;

        /// <summary>
        /// Used to log json trace outputs.
        /// </summary>
        public JsonWriter JsonLogger => LogWriter.JsonLogger;

        /// <summary>
        /// Callback that is fired when a Coyote event is dropped.
        /// </summary>
        public event OnEventDroppedHandler OnEventDropped;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRuntime"/> class.
        /// </summary>
        internal ActorRuntime(CheckerConfiguration checkerConfiguration, IRandomValueGenerator valueGenerator)
            : base(checkerConfiguration, valueGenerator)
        {
            ActorMap = new ConcurrentDictionary<ActorId, Actor>();
            LogWriter = new LogWriter(checkerConfiguration);
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
            CreateActor(null, type, null, initialEvent, null, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled.
        /// </summary>
        public virtual ActorId CreateActor(Type type, string name, Event initialEvent = null, Guid opGroupId = default) =>
            CreateActor(null, type, name, initialEvent, null, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified type, using the specified <see cref="ActorId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new actor, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public virtual ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, Guid opGroupId = default) =>
            CreateActor(id, type, null, initialEvent, null, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled. The method returns only when the actor is initialized and
        /// the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null, Guid opGroupId = default) =>
            CreateActorAndExecuteAsync(null, type, null, initialEvent, null, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled. The method returns only when the actor is
        /// initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null,
            Guid opGroupId = default) =>
            CreateActorAndExecuteAsync(null, type, name, initialEvent, null, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>, using the specified unbound
        /// actor id, and passes the specified optional <see cref="Event"/>. This event can only
        /// be used to access its payload, and cannot be handled. The method returns only when
        /// the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event initialEvent = null,
            Guid opGroupId = default) =>
            CreateActorAndExecuteAsync(id, type, null, initialEvent, null, opGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        public virtual void SendEvent(ActorId targetId, Event initialEvent, Guid opGroupId = default) =>
            SendEvent(targetId, initialEvent, null, opGroupId);

        /// <summary>
        /// Sends an <see cref="Event"/> to an actor. Returns immediately if the target was already
        /// running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        public virtual Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event initialEvent,
            Guid opGroupId = default) =>
            SendEventAndExecuteAsync(targetId, initialEvent, null, opGroupId);

        /// <summary>
        /// Returns the operation group id of the actor with the specified id. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="ActorId"/> is not associated with this runtime. During
        /// testing, the runtime asserts that the specified actor is currently executing.
        /// </summary>
        public virtual Guid GetCurrentOperationGroupId(ActorId currentActorId)
        {
            var actor = GetActorWithId<Actor>(currentActorId);
            return actor is null ? Guid.Empty : actor.OperationGroupId;
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal virtual ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent,
            Actor creator, Guid opGroupId)
        {
            var actor = CreateActor(id, type, name, creator, opGroupId);
            if (actor is StateMachine)
            {
                LogWriter.LogCreateStateMachine(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                LogWriter.LogCreateActor(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }

            RunActorEventHandler(actor, initialEvent, true);
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
            var actor = CreateActor(id, type, name, creator, opGroupId);
            if (actor is StateMachine)
            {
                LogWriter.LogCreateStateMachine(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                LogWriter.LogCreateActor(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }

            await RunActorEventHandlerAsync(actor, initialEvent, true);
            return actor.Id;
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
        /// </summary>
        private Actor CreateActor(ActorId id, Type type, string name, Actor creator, Guid opGroupId)
        {
            if (!type.IsSubclassOf(typeof(Actor)))
            {
                Assert(false, "Type '{0}' is not an actor.", type.FullName);
            }

            if (id is null)
            {
                id = new ActorId(type, name, this);
            }
            else if (id.Runtime != null && id.Runtime != this)
            {
                Assert(false, "Unbound actor id '{0}' was created by another runtime.", id.Value);
            }
            else if (id.Type != type.FullName)
            {
                Assert(false, "Cannot bind actor id '{0}' of type '{1}' to an actor of type '{2}'.",
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

            var actor = ActorFactory.Create(type);
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

            if (!ActorMap.TryAdd(id, actor))
            {
                var info = "This typically occurs if either the actor id was created by another runtime instance, " +
                           "or if a actor id from a previous runtime generation was deserialized, but the current runtime " +
                           "has not increased its generation value.";
                Assert(false, "An actor with id '{0}' was already created in generation '{1}'. {2}", id.Value, id.Generation, info);
            }

            return actor;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        internal virtual void SendEvent(ActorId targetId, Event e, Actor sender, Guid opGroupId)
        {
            e.Sender = sender.ToString();
            e.Receiver = targetId.ToString();
            var enqueueStatus = EnqueueEvent(targetId, e, sender, opGroupId, out var target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                RunActorEventHandler(target, null, false);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor. Returns immediately if the target was
        /// already running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        internal virtual async Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Actor sender,
            Guid opGroupId)
        {
            var enqueueStatus = EnqueueEvent(targetId, e, sender, opGroupId, out var target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                await RunActorEventHandlerAsync(target, null, false);
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
                var message = sender != null ?
                    string.Format("{0} is sending a null event.", sender.Id.ToString()) :
                    "Cannot send a null event.";
                Assert(false, message);
            }

            if (targetId is null)
            {
                var message = (sender != null) ? $"{sender.Id.ToString()} is sending event {e.ToString()} to a null actor."
                    : $"Cannot send event {e.ToString()} to a null actor.";

                Assert(false, message);
            }

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified send operation group id, if it is non-empty.
            // (2) To the operation group id of the sender actor, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && sender != null)
            {
                opGroupId = sender.OperationGroupId;
            }

            target = GetActorWithId<Actor>(targetId);
            if (target is null || target.IsHalted)
            {
                LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: true);
                TryHandleDroppedEvent(e, targetId);
                return EnqueueStatus.Dropped;
            }

            LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: false);

            var enqueueStatus = target.Enqueue(e, opGroupId, null);
            if (enqueueStatus == EnqueueStatus.Dropped)
            {
                TryHandleDroppedEvent(e, targetId);
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
                    IsRunning = false;
                    RaiseOnFailureEvent(ex);
                }
                finally
                {
                    if (actor.IsHalted)
                    {
                        ActorMap.TryRemove(actor.Id, out var _);
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
                IsRunning = false;
                RaiseOnFailureEvent(ex);
                return;
            }
        }

        /// <inheritdoc/>
        internal override void TryCreateMonitor(Type type)
        {
            lock (Monitors)
            {
                if (Monitors.Any(m => m.GetType() == type))
                {
                    // Idempotence: only one monitor per type can exist.
                    return;
                }
            }

            Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass of Monitor.", type.FullName);

            var monitor = (Monitor)Activator.CreateInstance(type);
            monitor.Initialize(this);
            monitor.InitializeStateInformation();

            lock (Monitors)
            {
                Monitors.Add(monitor);
            }

            LogWriter.LogCreateMonitor(type.FullName);

            monitor.GotoStartState();
        }

        /// <inheritdoc/>
        internal override bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType)
        {
            var result = false;
            if (ValueGenerator.Next(maxValue) == 0)
            {
                result = true;
            }

            LogWriter.LogRandom(result, callerName, callerType);
            return result;
        }

        /// <inheritdoc/>
        internal override int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
        {
            var result = ValueGenerator.Next(maxValue);
            LogWriter.LogRandom(result, callerName, callerType);
            return result;
        }

        /// <summary>
        /// Gets the actor of type <typeparamref name="TActor"/> with the specified id,
        /// or null if no such actor exists.
        /// </summary>
        private TActor GetActorWithId<TActor>(ActorId id)
            where TActor : Actor =>
            id != null && ActorMap.TryGetValue(id, out var value) &&
            value is TActor actor ? actor : null;

        /// <summary>
        /// Notifies that an actor invoked an action.
        /// </summary>
        internal virtual void NotifyInvokedAction(Actor actor, MethodInfo action, string handlingStateName,
            string currentStateName, Event receivedEvent)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                LogWriter.LogExecuteAction(actor.Id, handlingStateName, currentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that an actor dequeued an <see cref="Event"/>.
        /// </summary>
        internal virtual void NotifyDequeuedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                var stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                LogWriter.LogDequeueEvent(actor.Id, stateName, e);
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
            if (CheckerConfiguration.IsVerbose)
            {
                var stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                LogWriter.LogRaiseEvent(actor.Id, stateName, e);
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
            if (CheckerConfiguration.IsVerbose)
            {
                var stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: true);
            }
        }

        /// <summary>
        /// Notifies that an actor received an event without waiting because the event
        /// was already in the inbox when the actor invoked the receive statement.
        /// </summary>
        internal virtual void NotifyReceivedEventWithoutWaiting(Actor actor, Event e, EventInfo eventInfo)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                var stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: false);
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
            if (CheckerConfiguration.IsVerbose)
            {
                var stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                var eventWaitTypesArray = eventTypes.ToArray();
                if (eventWaitTypesArray.Length == 1)
                {
                    LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray[0]);
                }
                else
                {
                    LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray);
                }
            }
        }

        /// <summary>
        /// Notifies that a state machine entered a state.
        /// </summary>
        internal virtual void NotifyEnteredState(StateMachine stateMachine)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: true);
            }
        }

        /// <summary>
        /// Notifies that a state machine exited a state.
        /// </summary>
        internal virtual void NotifyExitedState(StateMachine stateMachine)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: false);
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
            if (CheckerConfiguration.IsVerbose)
            {
                LogWriter.LogExecuteAction(stateMachine.Id, stateMachine.CurrentStateName,
                    stateMachine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that a state machine invoked an action.
        /// </summary>
        internal virtual void NotifyInvokedOnExitAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                LogWriter.LogExecuteAction(stateMachine.Id, stateMachine.CurrentStateName,
                    stateMachine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        internal virtual void NotifyEnteredState(Monitor monitor)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                var monitorState = monitor.CurrentStateNameWithTemperature;
                LogWriter.LogMonitorStateTransition(monitor.GetType().FullName, monitorState,
                    true, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        internal virtual void NotifyExitedState(Monitor monitor)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                var monitorState = monitor.CurrentStateNameWithTemperature;
                LogWriter.LogMonitorStateTransition(monitor.GetType().FullName, monitorState,
                    false, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        internal virtual void NotifyInvokedAction(Monitor monitor, MethodInfo action, string stateName, Event receivedEvent)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                LogWriter.LogMonitorExecuteAction(monitor.GetType().FullName, action.Name, stateName);
            }
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        internal virtual void NotifyRaisedEvent(Monitor monitor, Event e)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                var monitorState = monitor.CurrentStateNameWithTemperature;
                LogWriter.LogMonitorRaiseEvent(monitor.GetType().FullName, monitorState, e);
            }
        }

        /// <summary>
        /// Notifies that a monitor found an error.
        /// </summary>
        internal void NotifyMonitorError(Monitor monitor)
        {
            if (CheckerConfiguration.IsVerbose)
            {
                var monitorState = monitor.CurrentStateNameWithTemperature;
                LogWriter.LogMonitorError(monitor.GetType().FullName, monitorState, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Tries to handle the specified dropped <see cref="Event"/>.
        /// </summary>
        internal void TryHandleDroppedEvent(Event e, ActorId id) => OnEventDropped?.Invoke(e, id);

        /// <inheritdoc/>
        public override TextWriter SetLogger(TextWriter logger) => LogWriter.SetLogger(logger);

        /// <summary>
        /// Sets the JsonLogger in LogWriter.cs
        /// </summary>
        /// <param name="jsonLogger">jsonLogger instance</param>
        public void SetJsonLogger(JsonWriter jsonLogger) => LogWriter.SetJsonLogger(jsonLogger);

        /// <summary>
        /// Use this method to register an <see cref="IActorRuntimeLog"/>.
        /// </summary>
        public void RegisterLog(IActorRuntimeLog log) => LogWriter.RegisterLog(log);

        /// <summary>
        /// Use this method to unregister a previously registered <see cref="IActorRuntimeLog"/>.
        /// </summary>
        public void RemoveLog(IActorRuntimeLog log) => LogWriter.RemoveLog(log);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Monitors.Clear();
                ActorMap.Clear();
            }

            base.Dispose(disposing);
        }
    }
}