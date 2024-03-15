// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using PChecker.Actors.Events;

namespace PChecker.Actors.Managers
{
    /// <summary>
    /// Interface for managing an actor.
    /// </summary>
    internal interface IActorManager
    {
        /// <summary>
        /// True if the event handler of the actor is running, else false.
        /// </summary>
        bool IsEventHandlerRunning { get; set; }

        /// <summary>
        /// Id used to identify subsequent operations performed by the actor.
        /// </summary>
        Guid OperationGroupId { get; set; }

        /// <summary>
        /// Returns the cached state of the actor.
        /// </summary>
        int GetCachedState();

        /// <summary>
        /// Checks if the specified event is currently ignored.
        /// </summary>
        bool IsEventIgnored(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Checks if the specified event is currently deferred.
        /// </summary>
        bool IsEventDeferred(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Checks if a default handler is currently available.
        /// </summary>
        bool IsDefaultHandlerAvailable();

        /// <summary>
        /// Notifies the actor that an event has been enqueued.
        /// </summary>
        void OnEnqueueEvent(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Notifies the actor that an event has been raised.
        /// </summary>
        void OnRaiseEvent(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Notifies the actor that it is waiting to receive an event of one of the specified types.
        /// </summary>
        void OnWaitEvent(IEnumerable<Type> eventTypes);

        /// <summary>
        /// Notifies the actor that an event it was waiting to receive has been enqueued.
        /// </summary>
        void OnReceiveEvent(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Notifies the actor that an event it was waiting to receive was already in the
        /// event queue when the actor invoked the receive statement.
        /// </summary>
        void OnReceiveEventWithoutWaiting(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Notifies the actor that an event has been dropped.
        /// </summary>
        void OnDropEvent(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        void Assert(bool predicate, string s, object arg0);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        void Assert(bool predicate, string s, object arg0, object arg1);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        void Assert(bool predicate, string s, object arg0, object arg1, object arg2);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        void Assert(bool predicate, string s, params object[] args);
    }
}