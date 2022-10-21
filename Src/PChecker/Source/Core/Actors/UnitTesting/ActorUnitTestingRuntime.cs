// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.Actors.UnitTesting
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
        internal ActorUnitTestingRuntime(Configuration configuration, Type actorType, IRandomValueGenerator valueGenerator)
            : base(configuration, valueGenerator)
        {
            if (!actorType.IsSubclassOf(typeof(Actor)))
            {
                this.Assert(false, "Type '{0}' is not an actor.", actorType.FullName);
            }

            var id = new ActorId(actorType, null, this);
            this.Instance = ActorFactory.Create(actorType);
            IActorManager actorManager;
            if (this.Instance is StateMachine stateMachine)
            {
                actorManager = new StateMachineManager(this, stateMachine, Guid.Empty);
            }
            else
            {
                actorManager = new ActorManager(this, this.Instance, Guid.Empty);
            }

            this.ActorInbox = new EventQueue(actorManager);
            this.Instance.Configure(this, id, actorManager, this.ActorInbox);
            this.Instance.SetupEventHandlers();
            if (this.Instance is StateMachine)
            {
                this.LogWriter.LogCreateStateMachine(this.Instance.Id, null, null);
            }
            else
            {
                this.LogWriter.LogCreateActor(this.Instance.Id, null, null);
            }

            this.IsActorWaitingToReceiveEvent = false;
        }

        /// <summary>
        /// Starts executing the actor-under-test by transitioning it to its initial state
        /// and passing an optional initialization event.
        /// </summary>
        internal Task StartAsync(Event initialEvent)
        {
            this.RunActorEventHandlerAsync(this.Instance, initialEvent, true);
            return this.QuiescenceCompletionSource.Task;
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
                this.LogWriter.LogCreateStateMachine(id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                this.LogWriter.LogCreateActor(id, creator?.Id.Name, creator?.Id.Type);
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
                this.LogWriter.LogCreateStateMachine(id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                this.LogWriter.LogCreateActor(id, creator?.Id.Name, creator?.Id.Type);
            }

            return Task.FromResult(id);
        }

        /// <inheritdoc/>
        internal override void SendEvent(ActorId targetId, Event e, Actor sender, Guid opGroupId, SendOptions options)
        {
            this.Assert(sender is null || this.Instance.Id.Equals(sender.Id),
                string.Format("Only {0} can send an event during this test.", this.Instance.Id.ToString()));
            this.Assert(e != null, string.Format("{0} is sending a null event.", this.Instance.Id.ToString()));
            this.Assert(targetId != null, string.Format("{0} is sending event {1} to a null actor.", this.Instance.Id.ToString(), e.ToString()));

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified send operation group id, if it is non-empty.
            // (2) To the operation group id of the sender actor, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && sender != null)
            {
                opGroupId = sender.OperationGroupId;
            }

            if (this.Instance.IsHalted)
            {
                this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: true);
                return;
            }

            this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: false);

            if (!targetId.Equals(this.Instance.Id))
            {
                // Drop all events sent to an actor other than the actor-under-test.
                return;
            }

            EnqueueStatus enqueueStatus = this.Instance.Enqueue(e, opGroupId, null);
            if (enqueueStatus == EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunActorEventHandlerAsync(this.Instance, null, false);
            }
        }

        /// <inheritdoc/>
        internal override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Actor sender,
            Guid opGroupId, SendOptions options)
        {
            this.SendEvent(targetId, e, sender, opGroupId, options);
            return this.QuiescenceCompletionSource.Task;
        }

        /// <summary>
        /// Runs a new asynchronous actor event handler.
        /// </summary>
        private Task RunActorEventHandlerAsync(Actor actor, Event initialEvent, bool isFresh)
        {
            this.QuiescenceCompletionSource = new TaskCompletionSource<bool>();

            return Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await actor.InitializeAsync(initialEvent);
                    }

                    await actor.RunEventHandlerAsync();
                    this.QuiescenceCompletionSource.SetResult(true);
                }
                catch (Exception ex)
                {
                    this.IsRunning = false;
                    this.RaiseOnFailureEvent(ex);
                    this.QuiescenceCompletionSource.SetException(ex);
                }
            });
        }

        /// <inheritdoc/>
        internal override IActorTimer CreateActorTimer(TimerInfo info, Actor owner) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        internal override void NotifyReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            base.NotifyReceivedEvent(actor, e, eventInfo);
            this.IsActorWaitingToReceiveEvent = false;
            this.QuiescenceCompletionSource = new TaskCompletionSource<bool>();
        }

        /// <inheritdoc/>
        internal override void NotifyWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            base.NotifyWaitEvent(actor, eventTypes);
            this.IsActorWaitingToReceiveEvent = true;
            this.QuiescenceCompletionSource.SetResult(true);
        }
    }
}
