// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PChecker.Actors.Events;
using PChecker.Actors.Managers;

namespace PChecker.Actors.EventQueues.Mocks
{
    /// <summary>
    /// Implements a queue of events that is used during testing.
    /// </summary>
    internal sealed class MockEventQueue : IEventQueue
    {
        /// <summary>
        /// Manages the actor that owns this queue.
        /// </summary>
        private readonly IActorManager ActorManager;

        /// <summary>
        /// The actor that owns this queue.
        /// </summary>
        private readonly Actor Actor;

        /// <summary>
        /// The internal queue that contains events with their metadata.
        /// </summary>
        private readonly LinkedList<(Event e, Guid opGroupId, EventInfo info)> Queue;

        /// <summary>
        /// The raised event and its metadata, or null if no event has been raised.
        /// </summary>
        private (Event e, Guid opGroupId, EventInfo info) RaisedEvent;

        /// <summary>
        /// Map from the types of events that the owner of the queue is waiting to receive
        /// to an optional predicate. If an event of one of these types is enqueued, then
        /// if there is no predicate, or if there is a predicate and evaluates to true, then
        /// the event is received, else the event is deferred.
        /// </summary>
        private Dictionary<Type, Func<Event, bool>> EventWaitTypes;

        /// <summary>
        /// Task completion source that contains the event obtained using an explicit receive.
        /// </summary>
        private TaskCompletionSource<Event> ReceiveCompletionSource;

        /// <summary>
        /// Checks if the queue is accepting new events.
        /// </summary>
        private bool IsClosed;

        /// <summary>
        /// The size of the queue.
        /// </summary>
        public int Size => Queue.Count;

        /// <summary>
        /// Checks if an event has been raised.
        /// </summary>
        public bool IsEventRaised => RaisedEvent != default;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockEventQueue"/> class.
        /// </summary>
        internal MockEventQueue(IActorManager actorManager, Actor actor)
        {
            ActorManager = actorManager;
            Actor = actor;
            Queue = new LinkedList<(Event, Guid, EventInfo)>();
            EventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            IsClosed = false;
        }

        /// <inheritdoc/>
        public EnqueueStatus Enqueue(Event e, Guid opGroupId, EventInfo info)
        {
            if (IsClosed)
            {
                return EnqueueStatus.Dropped;
            }

            if (EventWaitTypes.TryGetValue(e.GetType(), out var predicate) &&
                (predicate is null || predicate(e)))
            {
                EventWaitTypes.Clear();
                ActorManager.OnReceiveEvent(e, opGroupId, info);
                ReceiveCompletionSource.SetResult(e);
                return EnqueueStatus.EventHandlerRunning;
            }

            ActorManager.OnEnqueueEvent(e, opGroupId, info);
            Queue.AddLast((e, opGroupId, info));

            if (!ActorManager.IsEventHandlerRunning)
            {
                if (TryDequeueEvent(true).e is null)
                {
                    return EnqueueStatus.NextEventUnavailable;
                }
                else
                {
                    ActorManager.IsEventHandlerRunning = true;
                    return EnqueueStatus.EventHandlerNotRunning;
                }
            }

            return EnqueueStatus.EventHandlerRunning;
        }

        /// <inheritdoc/>
        public (DequeueStatus status, Event e, Guid opGroupId, EventInfo info) Dequeue()
        {
            // Try to get the raised event, if there is one. Raised events
            // have priority over the events in the inbox.
            if (RaisedEvent != default)
            {
                if (ActorManager.IsEventIgnored(RaisedEvent.e, RaisedEvent.opGroupId, RaisedEvent.info))
                {
                    // TODO: should the user be able to raise an ignored event?
                    // The raised event is ignored in the current state.
                    RaisedEvent = default;
                }
                else
                {
                    var raisedEvent = RaisedEvent;
                    RaisedEvent = default;
                    return (DequeueStatus.Raised, raisedEvent.e, raisedEvent.opGroupId, raisedEvent.info);
                }
            }

            var hasDefaultHandler = ActorManager.IsDefaultHandlerAvailable();
            if (hasDefaultHandler)
            {
                Actor.Runtime.NotifyDefaultEventHandlerCheck(Actor);
            }

            // Try to dequeue the next event, if there is one.
            var (e, opGroupId, info) = TryDequeueEvent();
            if (e != null)
            {
                // Found next event that can be dequeued.
                return (DequeueStatus.Success, e, opGroupId, info);
            }

            // No event can be dequeued, so check if there is a default event handler.
            if (!hasDefaultHandler)
            {
                // There is no default event handler installed, so do not return an event.
                ActorManager.IsEventHandlerRunning = false;
                return (DequeueStatus.NotAvailable, null, Guid.Empty, null);
            }

            // TODO: check op-id of default event.
            // A default event handler exists.
            var stateName = Actor is StateMachine stateMachine ?
                NameResolver.GetStateNameForLogging(stateMachine.CurrentState) : string.Empty;
            var eventOrigin = new EventOriginInfo(Actor.Id, Actor.GetType().FullName, stateName);
            return (DequeueStatus.Default, DefaultEvent.Instance, Guid.Empty, new EventInfo(DefaultEvent.Instance, eventOrigin));
        }

        /// <summary>
        /// Dequeues the next event and its metadata, if there is one available, else returns null.
        /// </summary>
        private (Event e, Guid opGroupId, EventInfo info) TryDequeueEvent(bool checkOnly = false)
        {
            (Event, Guid, EventInfo) nextAvailableEvent = default;

            // Iterates through the events and metadata in the inbox.
            var node = Queue.First;
            while (node != null)
            {
                var nextNode = node.Next;
                var currentEvent = node.Value;

                if (ActorManager.IsEventIgnored(currentEvent.e, currentEvent.opGroupId, currentEvent.info))
                {
                    if (!checkOnly)
                    {
                        // Removes an ignored event.
                        Queue.Remove(node);
                    }

                    node = nextNode;
                    continue;
                }

                // Skips a deferred event.
                if (!ActorManager.IsEventDeferred(currentEvent.e, currentEvent.opGroupId, currentEvent.info))
                {
                    nextAvailableEvent = currentEvent;
                    if (!checkOnly)
                    {
                        Queue.Remove(node);
                    }

                    break;
                }

                node = nextNode;
            }

            return nextAvailableEvent;
        }

        /// <inheritdoc/>
        public void RaiseEvent(Event e, Guid opGroupId)
        {
            var stateName = Actor is StateMachine stateMachine ?
                NameResolver.GetStateNameForLogging(stateMachine.CurrentState) : string.Empty;
            var eventOrigin = new EventOriginInfo(Actor.Id, Actor.GetType().FullName, stateName);
            var info = new EventInfo(e, eventOrigin);
            RaisedEvent = (e, opGroupId, info);
            ActorManager.OnRaiseEvent(e, opGroupId, info);
        }

        /// <inheritdoc/>
        public Task<Event> ReceiveEventAsync(Type eventType, Func<Event, bool> predicate = null)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>
            {
                { eventType, predicate }
            };

            return ReceiveEventAsync(eventWaitTypes);
        }

        /// <inheritdoc/>
        public Task<Event> ReceiveEventAsync(params Type[] eventTypes)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            foreach (var type in eventTypes)
            {
                eventWaitTypes.Add(type, null);
            }

            return ReceiveEventAsync(eventWaitTypes);
        }

        /// <inheritdoc/>
        public Task<Event> ReceiveEventAsync(params Tuple<Type, Func<Event, bool>>[] events)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            foreach (var e in events)
            {
                eventWaitTypes.Add(e.Item1, e.Item2);
            }

            return ReceiveEventAsync(eventWaitTypes);
        }

        /// <summary>
        /// Waits for an event to be enqueued.
        /// </summary>
        private Task<Event> ReceiveEventAsync(Dictionary<Type, Func<Event, bool>> eventWaitTypes)
        {
            Actor.Runtime.NotifyReceiveCalled(Actor);

            (Event e, Guid opGroupId, EventInfo info) receivedEvent = default;
            var node = Queue.First;
            while (node != null)
            {
                // Dequeue the first event that the caller waits to receive, if there is one in the queue.
                if (eventWaitTypes.TryGetValue(node.Value.e.GetType(), out var predicate) &&
                    (predicate is null || predicate(node.Value.e)))
                {
                    receivedEvent = node.Value;
                    Queue.Remove(node);
                    break;
                }

                node = node.Next;
            }

            if (receivedEvent == default)
            {
                ReceiveCompletionSource = new TaskCompletionSource<Event>();
                EventWaitTypes = eventWaitTypes;
                ActorManager.OnWaitEvent(EventWaitTypes.Keys);
                return ReceiveCompletionSource.Task;
            }

            ActorManager.OnReceiveEventWithoutWaiting(receivedEvent.e, receivedEvent.opGroupId, receivedEvent.info);
            return Task.FromResult(receivedEvent.e);
        }

        /// <inheritdoc/>
        public int GetCachedState()
        {
            unchecked
            {
                var hash = 19;
                foreach (var (_, _, info) in Queue)
                {
                    hash = (hash * 31) + info.EventName.GetHashCode();
                    if (info.HashedState != 0)
                    {
                        // Adds the user-defined hashed event state.
                        hash = (hash * 31) + info.HashedState;
                    }
                }

                return hash;
            }
        }

        /// <inheritdoc/>
        public void Close()
        {
            IsClosed = true;
        }

        /// <summary>
        /// Disposes the queue resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            foreach (var (e, opGroupId, info) in Queue)
            {
                ActorManager.OnDropEvent(e, opGroupId, info);
            }

            Queue.Clear();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}