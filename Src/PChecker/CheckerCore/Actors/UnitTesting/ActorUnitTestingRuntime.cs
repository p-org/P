// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PChecker.Actors.EventQueues;
using PChecker.Actors.Events;
using PChecker.Actors.Managers;
using PChecker.Random;

namespace PChecker.Actors.UnitTesting
{
    /// <summary>
    /// Runtime for testing an actor in isolation.
    /// </summary>
    internal sealed class ActorUnitTestingRuntime : ActorRuntime
    {
        /// <summary>
        /// The actor being tested.
        /// </summary>
        internal readonly Actor Instance;

        /// <summary>
        /// The inbox of the actor being tested.
        /// </summary>
        internal readonly EventQueue ActorInbox;

        /// <summary>
        /// Task completion source that completes when the actor being tested reaches quiescence.
        /// </summary>
        private TaskCompletionSource<bool> QuiescenceCompletionSource;

        /// <summary>
        /// True if the actor is waiting to receive and event, else false.
        /// </summary>
        internal bool IsActorWaitingToReceiveEvent { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorUnitTestingRuntime"/> class.
        /// </summary>
        internal ActorUnitTestingRuntime(CheckerConfiguration checkerConfiguration, Type actorType, IRandomValueGenerator valueGenerator)
            : base(checkerConfiguration, valueGenerator)
        {
            if (!actorType.IsSubclassOf(typeof(Actor)))
            {
                Assert(false, "Type '{0}' is not an actor.", actorType.FullName);
            }

            var id = new ActorId(actorType, null, this);
            Instance = ActorFactory.Create(actorType);
            IActorManager actorManager;
            if (Instance is StateMachine stateMachine)
            {
                actorManager = new StateMachineManager(this, stateMachine, Guid.Empty);
            }
            else
            {
                actorManager = new ActorManager(this, Instance, Guid.Empty);
            }

            ActorInbox = new EventQueue(actorManager);
            Instance.Configure(this, id, actorManager, ActorInbox);
            Instance.SetupEventHandlers();
            if (Instance is StateMachine)
            {
                LogWriter.LogCreateStateMachine(Instance.Id, null, null);
            }
            else
            {
                LogWriter.LogCreateActor(Instance.Id, null, null);
            }

            IsActorWaitingToReceiveEvent = false;
        }

        /// <summary>
        /// Starts executing the actor-under-test by transitioning it to its initial state
        /// and passing an optional initialization event.
        /// </summary>
        internal Task StartAsync(Event initialEvent)
        {
            RunActorEventHandlerAsync(Instance, initialEvent, true);
            return QuiescenceCompletionSource.Task;
        }

        /// <inheritdoc/>
        public override ActorId CreateActorIdFromName(Type type, string name) => new ActorId(type, name, this, true);

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, Event initialEvent = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, string name, Event initialEvent = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        public override ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null,
            Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null,
            Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event e = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        public override void SendEvent(ActorId targetId, Event e, Guid opGroupId = default, SendOptions options = null) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        public override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Guid opGroupId = default, SendOptions options = null) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        public override Guid GetCurrentOperationGroupId(ActorId currentActorId) => Guid.Empty;

        /// <inheritdoc/>
        internal override ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent,
            Actor creator, Guid opGroupId)
        {
            id = id ?? new ActorId(type, null, this);
            if (typeof(StateMachine).IsAssignableFrom(type))
            {
                LogWriter.LogCreateStateMachine(id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                LogWriter.LogCreateActor(id, creator?.Id.Name, creator?.Id.Type);
            }

            return id;
        }

        /// <inheritdoc/>
        internal override Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name,
            Event initialEvent, Actor creator, Guid opGroupId)
        {
            id = id ?? new ActorId(type, null, this);
            if (typeof(StateMachine).IsAssignableFrom(type))
            {
                LogWriter.LogCreateStateMachine(id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                LogWriter.LogCreateActor(id, creator?.Id.Name, creator?.Id.Type);
            }

            return Task.FromResult(id);
        }

        /// <inheritdoc/>
        internal override void SendEvent(ActorId targetId, Event e, Actor sender, Guid opGroupId, SendOptions options)
        {
            Assert(sender is null || Instance.Id.Equals(sender.Id),
                string.Format("Only {0} can send an event during this test.", Instance.Id.ToString()));
            Assert(e != null, string.Format("{0} is sending a null event.", Instance.Id.ToString()));
            Assert(targetId != null, string.Format("{0} is sending event {1} to a null actor.", Instance.Id.ToString(), e.ToString()));

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified send operation group id, if it is non-empty.
            // (2) To the operation group id of the sender actor, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && sender != null)
            {
                opGroupId = sender.OperationGroupId;
            }

            if (Instance.IsHalted)
            {
                LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: true);
                return;
            }

            LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: false);

            if (!targetId.Equals(Instance.Id))
            {
                // Drop all events sent to an actor other than the actor-under-test.
                return;
            }

            var enqueueStatus = Instance.Enqueue(e, opGroupId, null);
            if (enqueueStatus == EnqueueStatus.EventHandlerNotRunning)
            {
                RunActorEventHandlerAsync(Instance, null, false);
            }
        }

        /// <inheritdoc/>
        internal override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Actor sender,
            Guid opGroupId, SendOptions options)
        {
            SendEvent(targetId, e, sender, opGroupId, options);
            return QuiescenceCompletionSource.Task;
        }

        /// <summary>
        /// Runs a new asynchronous actor event handler.
        /// </summary>
        private Task RunActorEventHandlerAsync(Actor actor, Event initialEvent, bool isFresh)
        {
            QuiescenceCompletionSource = new TaskCompletionSource<bool>();

            return Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await actor.InitializeAsync(initialEvent);
                    }

                    await actor.RunEventHandlerAsync();
                    QuiescenceCompletionSource.SetResult(true);
                }
                catch (Exception ex)
                {
                    IsRunning = false;
                    RaiseOnFailureEvent(ex);
                    QuiescenceCompletionSource.SetException(ex);
                }
            });
        }
        
        /// <inheritdoc/>
        internal override void NotifyReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            base.NotifyReceivedEvent(actor, e, eventInfo);
            IsActorWaitingToReceiveEvent = false;
            QuiescenceCompletionSource = new TaskCompletionSource<bool>();
        }

        /// <inheritdoc/>
        internal override void NotifyWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            base.NotifyWaitEvent(actor, eventTypes);
            IsActorWaitingToReceiveEvent = true;
            QuiescenceCompletionSource.SetResult(true);
        }
    }
}