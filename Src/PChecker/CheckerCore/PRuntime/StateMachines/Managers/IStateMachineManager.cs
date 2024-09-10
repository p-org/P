// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using PChecker.StateMachines.Events;

namespace PChecker.StateMachines.Managers
{
    /// <summary>
    /// Interface for managing a state machine.
    /// </summary>
    internal interface IStateMachineManager
    {
        /// <summary>
        /// True if the event handler of the state machine is running, else false.
        /// </summary>
        bool IsEventHandlerRunning { get; set; }
        
        /// <summary>
        /// Returns the cached state of the state machine.
        /// </summary>
        int GetCachedState();

        /// <summary>
        /// Checks if the specified event is currently ignored.
        /// </summary>
        bool IsEventIgnored(Event e, EventInfo eventInfo);

        /// <summary>
        /// Checks if the specified event is currently deferred.
        /// </summary>
        bool IsEventDeferred(Event e, EventInfo eventInfo);

        /// <summary>
        /// Checks if a default handler is currently available.
        /// </summary>
        bool IsDefaultHandlerAvailable();

        /// <summary>
        /// Notifies the state machine that an event has been enqueued.
        /// </summary>
        void OnEnqueueEvent(Event e, EventInfo eventInfo);

        /// <summary>
        /// Notifies the state machine that an event has been raised.
        /// </summary>
        void OnRaiseEvent(Event e, EventInfo eventInfo);

        /// <summary>
        /// Notifies the state machine that it is waiting to receive an event of one of the specified types.
        /// </summary>
        void OnWaitEvent(IEnumerable<Type> eventTypes);

        /// <summary>
        /// Notifies the state machine that an event it was waiting to receive has been enqueued.
        /// </summary>
        void OnReceiveEvent(Event e, EventInfo eventInfo);

        /// <summary>
        /// Notifies the state machine that an event it was waiting to receive was already in the
        /// event queue when the state machine invoked the receiving statement.
        /// </summary>
        void OnReceiveEventWithoutWaiting(Event e, EventInfo eventInfo);

        /// <summary>
        /// Notifies the state machine that an event has been dropped.
        /// </summary>
        void OnDropEvent(Event e, EventInfo eventInfo);

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