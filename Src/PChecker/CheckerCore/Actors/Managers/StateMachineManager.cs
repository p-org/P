// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PChecker.Actors.Events;

namespace PChecker.Actors.Managers
{
    /// <summary>
    /// Manages a state machine in production.
    /// </summary>
    internal class StateMachineManager : IActorManager
    {
        /// <summary>
        /// The runtime that executes the state machine being managed.
        /// </summary>
        private readonly ActorRuntime Runtime;

        /// <summary>
        /// The state machine being managed.
        /// </summary>
        private readonly StateMachine Instance;

        /// <inheritdoc/>
        public bool IsEventHandlerRunning { get; set; }

        /// <inheritdoc/>
        public Guid OperationGroupId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineManager"/> class.
        /// </summary>
        internal StateMachineManager(ActorRuntime runtime, StateMachine instance, Guid operationGroupId)
        {
            Runtime = runtime;
            Instance = instance;
            IsEventHandlerRunning = true;
            OperationGroupId = operationGroupId;
        }

        /// <inheritdoc/>
        public int GetCachedState() => 0;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventIgnored(Event e, Guid opGroupId, EventInfo eventInfo) =>
            Instance.IsEventIgnoredInCurrentState(e);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventDeferred(Event e, Guid opGroupId, EventInfo eventInfo) =>
            Instance.IsEventDeferredInCurrentState(e);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDefaultHandlerAvailable() => Instance.IsDefaultHandlerInstalledInCurrentState();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnEnqueueEvent(Event e, Guid opGroupId, EventInfo eventInfo) =>
            Runtime.LogWriter.LogEnqueueEvent(Instance.Id, e);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRaiseEvent(Event e, Guid opGroupId, EventInfo eventInfo) =>
            Runtime.NotifyRaisedEvent(Instance, e, eventInfo);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnWaitEvent(IEnumerable<Type> eventTypes) =>
            Runtime.NotifyWaitEvent(Instance, eventTypes);

        /// <inheritdoc/>
        public void OnReceiveEvent(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            if (opGroupId != Guid.Empty)
            {
                // Inherit the operation group id of the receive operation, if it is non-empty.
                OperationGroupId = opGroupId;
            }

            Runtime.NotifyReceivedEvent(Instance, e, eventInfo);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnReceiveEventWithoutWaiting(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            if (opGroupId != Guid.Empty)
            {
                // Inherit the operation group id of the receive operation, if it is non-empty.
                OperationGroupId = opGroupId;
            }

            Runtime.NotifyReceivedEventWithoutWaiting(Instance, e, eventInfo);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDropEvent(Event e, Guid opGroupId, EventInfo eventInfo) =>
            Runtime.TryHandleDroppedEvent(e, Instance.Id);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0) => Runtime.Assert(predicate, s, arg0);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0, object arg1) => Runtime.Assert(predicate, s, arg0, arg1);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            Runtime.Assert(predicate, s, arg0, arg1, arg2);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, params object[] args) => Runtime.Assert(predicate, s, args);
    }
}