// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using PChecker.StateMachines.Events;
using PChecker.StateMachines.Exceptions;
using PChecker.StateMachines.Logging;
using PChecker.Runtime;

namespace PChecker.StateMachines
{
    /// <summary>
    /// Interface that exposes runtime methods for creating and executing state machines.
    /// </summary>
    public interface IStateMachineRuntime : ICoyoteRuntime
    {
        /// <summary>
        /// Callback that is fired when an event is dropped.
        /// </summary>
        event OnEventDroppedHandler OnEventDropped;

        /// <summary>
        /// Creates a fresh state machine id that has not yet been bound to any state machine.
        /// </summary>
        /// <param name="type">Type of the state machine.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <returns>The result is the state machine id.</returns>
        StateMachineId CreateStateMachineId(Type type, string name = null);

        /// <summary>
        /// Creates a state machine id that is uniquely tied to the specified unique name. The
        /// returned state machine id can either be a fresh id (not yet bound to any state machine), or
        /// it can be bound to a previously created state machine. In the second case, this state machine
        /// id can be directly used to communicate with the corresponding state machine.
        /// </summary>
        /// <param name="type">Type of the state machine.</param>
        /// <param name="name">Unique name used to create or get the state machine id.</param>
        /// <returns>The result is the state machine id.</returns>
        StateMachineId CreateStateMachineIdFromName(Type type, string name);

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the state machine.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The result is the state machine id.</returns>
        StateMachineId CreateStateMachine(Type type, Event initialEvent = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the state machine.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The result is the state machine id.</returns>
        StateMachineId CreateStateMachine(Type type, string name, Event initialEvent = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new state machine of the specified type, using the specified <see cref="StateMachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new state machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="id">Unbound state machine id.</param>
        /// <param name="type">Type of the state machine.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The result is the state machine id.</returns>
        StateMachineId CreateStateMachine(StateMachineId id, Type type, Event initialEvent = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled. The method returns only when the state machine is initialized and
        /// the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the state machine.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the state machine id.</returns>
        Task<StateMachineId> CreateStateMachineAndExecuteAsync(Type type, Event initialEvent = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled. The method returns only when the state machine is
        /// initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the state machine.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the state machine id.</returns>
        Task<StateMachineId> CreateStateMachineAndExecuteAsync(Type type, string name, Event initialEvent = null,
            Guid opGroupId = default);

        /// <summary>
        /// Creates a new state machine of the specified <see cref="Type"/>, using the specified unbound
        /// state machine id, and passes the specified optional <see cref="Event"/>. This event can only
        /// be used to access its payload, and cannot be handled. The method returns only when
        /// the state machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        /// <param name="id">Unbound state machine id.</param>
        /// <param name="type">Type of the state machine.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the state machine id.</returns>
        Task<StateMachineId> CreateStateMachineAndExecuteAsync(StateMachineId id, Type type, Event initialEvent = null,
            Guid opGroupId = default);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an state machine.
        /// </summary>
        /// <param name="targetId">The id of the target.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        void SendEvent(StateMachineId targetId, Event e, Guid opGroupId = default);

        /// <summary>
        /// Sends an <see cref="Event"/> to an state machine. Returns immediately if the target was already
        /// running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        /// <param name="targetId">The id of the target.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is true if
        /// the event was handled, false if the event was only enqueued.</returns>
        Task<bool> SendEventAndExecuteAsync(StateMachineId targetId, Event e, Guid opGroupId = default);

        /// <summary>
        /// Returns the operation group id of the state machine with the specified id. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="StateMachineId"/> is not associated with this runtime. During
        /// testing, the runtime asserts that the specified state machine is currently executing.
        /// </summary>
        /// <param name="currentStateMachineId">The id of the currently executing state machine.</param>
        /// <returns>The unique identifier.</returns>
        Guid GetCurrentOperationGroupId(StateMachineId currentStateMachineId);

        /// <summary>
        /// Use this method to register an <see cref="IStateMachineRuntimeLog"/>.
        /// </summary>
        /// <param name="log">The log writer to register.</param>
        void RegisterLog(IStateMachineRuntimeLog log);

        /// <summary>
        /// Use this method to unregister a previously registered <see cref="IStateMachineRuntimeLog"/>.
        /// </summary>
        /// <param name="log">The previously registered log writer to unregister.</param>
        void RemoveLog(IStateMachineRuntimeLog log);
    }
}