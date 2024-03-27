// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PChecker.Actors.Events;
using PChecker.SystematicTesting;

namespace PChecker.Actors.Managers.Mocks
{
    /// <summary>
    /// Implements a state machine manager that is used during testing.
    /// </summary>
    internal sealed class MockStateMachineManager : IActorManager
    {
        /// <summary>
        /// The runtime that executes the state machine being managed.
        /// </summary>
        private readonly ControlledRuntime Runtime;

        /// <summary>
        /// The state machine being managed.
        /// </summary>
        private readonly StateMachine Instance;

        /// <inheritdoc/>
        public bool IsEventHandlerRunning { get; set; }

        /// <inheritdoc/>
        public Guid OperationGroupId { get; set; }

        /// <summary>
        /// Program counter used for state-caching. Distinguishes
        /// scheduling from non-deterministic choices.
        /// </summary>
        internal int ProgramCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockStateMachineManager"/> class.
        /// </summary>
        internal MockStateMachineManager(ControlledRuntime runtime, StateMachine instance, Guid operationGroupId)
        {
            Runtime = runtime;
            Instance = instance;
            IsEventHandlerRunning = true;
            OperationGroupId = operationGroupId;
            ProgramCounter = 0;
        }

        /// <inheritdoc/>
        public int GetCachedState()
        {
            unchecked
            {
                var hash = 19;
                hash = (hash * 31) + IsEventHandlerRunning.GetHashCode();
                hash = (hash * 31) + ProgramCounter;
                return hash;
            }
        }

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
        public void OnDropEvent(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            Runtime.TryHandleDroppedEvent(e, Instance.Id);
        }

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