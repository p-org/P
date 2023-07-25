﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PChecker.Actors.Events;
using PChecker.Actors.Managers;
using PChecker.SystematicTesting;
using PChecker.SystematicTesting.Strategies;

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
        /// The scheduling strategy used for program exploration.
        /// </summary>
        private readonly ISchedulingStrategy Strategy;

        private readonly Dictionary<ActorId, Timestamp> MaxDequeueTimestampMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockEventQueue"/> class.
        /// </summary>
        internal MockEventQueue(IActorManager actorManager, Actor actor, ISchedulingStrategy strategy)
        {
            ActorManager = actorManager;
            Actor = actor;
            Strategy = strategy;
            Queue = new LinkedList<(Event, Guid, EventInfo)>();
            EventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            IsClosed = false;
            MaxDequeueTimestampMap = Strategy is null ? null : new Dictionary<ActorId, Timestamp>();
        }

        /// <inheritdoc/>
        public EnqueueStatus Enqueue(Event e, Guid opGroupId, EventInfo info)
        {
            e.EnqueueTime.SetTime(ControlledRuntime.GlobalTime.GetTime());
            e.DequeueTime.SetTime(e.EnqueueTime.GetTime());
            if (Strategy is not null && Strategy.GetSampleFromDistribution(e.DelayDistribution, out var delay))
            {
                var isFirstEvent = !MaxDequeueTimestampMap.ContainsKey(info.OriginInfo.SenderActorId);
                if (e.IsOrdered && !isFirstEvent)
                {
                    var maxDequeueTimestamp = MaxDequeueTimestampMap[info.OriginInfo.SenderActorId];
                    if (maxDequeueTimestamp > e.EnqueueTime)
                    {
                        e.DequeueTime.SetTime(maxDequeueTimestamp.GetTime());
                    }
                }

                e.DequeueTime.IncrementTime(delay);

                if (e.IsOrdered)
                {
                    if (isFirstEvent)
                    {
                        MaxDequeueTimestampMap.Add(info.OriginInfo.SenderActorId, e.DequeueTime);
                    }
                    else
                    {
                        MaxDequeueTimestampMap[info.OriginInfo.SenderActorId] = e.DequeueTime;
                    }
                }
            }

            if (IsClosed)
            {
                return EnqueueStatus.Dropped;
            }

            if (e.DequeueTime <= ControlledRuntime.GlobalTime)
            {
                if (EventWaitTypes.TryGetValue(e.GetType(), out var predicate) &&
                    (predicate is null || predicate(e)))
                {
                    EventWaitTypes.Clear();
                    ActorManager.OnReceiveEvent(e, opGroupId, info);
                    ReceiveCompletionSource.SetResult(e);
                    return EnqueueStatus.EventHandlerRunning;
                }
            }

            ActorManager.OnEnqueueEvent(e, opGroupId, info);
            Queue.AddLast((e, opGroupId, info));

            if (info.Assert >= 0)
            {
                var eventCount = Queue.Count(val => val.e.GetType().Equals(e.GetType()));
                ActorManager.Assert(eventCount <= info.Assert,
                    "There are more than {0} instances of '{1}' in the input queue of {2}.",
                    info.Assert, info.EventName, Actor.Id);
            }

            if (!ActorManager.IsEventHandlerRunning)
            {
                ((Event nextEvent, _, _), bool isDelayed) = TryDequeueEvent(true);
                if (nextEvent is null || isDelayed)
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
        public (DequeueStatus status, Event e, Guid opGroupId, EventInfo info) Dequeue(bool checkOnly = false)
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
            var ((e, opGroupId, info), isDelayed) = TryDequeueEvent(checkOnly);
            if (isDelayed)
            {
                if (e == null)
                {
                    return (DequeueStatus.NotAvailable, null, Guid.Empty, null);
                }
                else
                {
                    ActorManager.IsEventHandlerRunning = false;
                    return (DequeueStatus.Delayed, e, Guid.Empty, null);
                }

            }

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
        private ((Event e, Guid opGroupId, EventInfo info), bool isDelayed) TryDequeueEvent(bool checkOnly = false)
        {
            if (EventWaitTypes.Count > 0)
            {
                // We cannot dequeue anything. The actor is blocked on a receive. Being blocked on a receive and calling
                // this function means that the actor is waiting for a delayed event. Therefore, this event is
                // inherently delayed, but we cannot return the event from the queue because it cannot be dequeued,
                // i.e.,it has to be received through a different path in the code.
                return (default, true);
            }

            (Event, Guid, EventInfo) nextAvailableEvent = default;
            bool isDelayed = false;

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

                if (currentEvent.e.DequeueTime <= ControlledRuntime.GlobalTime)
                {
                    if (!ActorManager.IsEventDeferred(currentEvent.e, currentEvent.opGroupId, currentEvent.info))
                    {
                        nextAvailableEvent = currentEvent;
                        isDelayed = false;

                        if (!checkOnly)
                        {
                            Queue.Remove(node);
                        }

                        break;
                    }
                }

                if (currentEvent.e.DequeueTime > ControlledRuntime.GlobalTime)
                {
                    if (!ActorManager.IsEventDeferred(currentEvent.e, currentEvent.opGroupId, currentEvent.info))
                    {
                        if (nextAvailableEvent == default || nextAvailableEvent.Item1.DequeueTime > currentEvent.e.DequeueTime)
                        {
                            nextAvailableEvent = currentEvent;
                            isDelayed = true;
                        }
                    }
                }

                node = nextNode;
            }
            return (nextAvailableEvent, isDelayed);
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
                if (node.Value.e.DequeueTime <= ControlledRuntime.GlobalTime)
                {
                    // Dequeue the first event that the caller waits to receive, if there is one in the queue.
                    if (eventWaitTypes.TryGetValue(node.Value.e.GetType(), out var predicate) &&
                        (predicate is null || predicate(node.Value.e)))
                    {
                        receivedEvent = node.Value;
                        Queue.Remove(node);
                        break;
                    }
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

        public bool ReceiveDelayedWaitEvents()
        {
            (Event e, Guid opGroupId, EventInfo info) receivedEvent = default;
            var node = Queue.First;
            while (node != null)
            {
                if (node.Value.e.DequeueTime <= ControlledRuntime.GlobalTime)
                {
                    // Dequeue the first event that the caller waits to receive, if there is one in the queue.
                    if (EventWaitTypes.TryGetValue(node.Value.e.GetType(), out var predicate) &&
                        (predicate is null || predicate(node.Value.e)))
                    {
                        receivedEvent = node.Value;
                        Queue.Remove(node);
                        break;
                    }
                }

                node = node.Next;
            }

            if (receivedEvent != default)
            {
                EventWaitTypes.Clear();
                ActorManager.OnReceiveEvent(receivedEvent.e, receivedEvent.opGroupId, receivedEvent.info);
                ReceiveCompletionSource.SetResult(receivedEvent.e);
                return true;
            }

            return false;
        }

        public Event GetDelayedWaitEvent()
        {
            var node = Queue.First;
            Event minTimestampWaitEvent = null;
            while (node != null)
            {
                // Dequeue the first event that the caller waits to receive, if there is one in the queue.
                if (EventWaitTypes.TryGetValue(node.Value.e.GetType(), out var predicate) &&
                    (predicate is null || predicate(node.Value.e)))
                {
                    if (minTimestampWaitEvent is null || node.Value.e.DequeueTime < minTimestampWaitEvent.DequeueTime)
                    {
                        minTimestampWaitEvent = node.Value.e;
                    }
                }

                node = node.Next;
            }

            return minTimestampWaitEvent;
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
