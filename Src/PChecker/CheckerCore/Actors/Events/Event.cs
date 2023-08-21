// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using PChecker.SystematicTesting;

namespace PChecker.Actors.Events
{
    /// <summary>
    /// Abstract class representing an event.
    /// </summary>
    [DataContract]
    public abstract class Event
    {
        /// <summary>
        /// Delay distribution of the event.
        /// </summary>
        public string DelayDistribution = null;

        /// <summary>
        /// Flag indicating whether the dequeue time of the event should be ordered with the other delayed events sent
        /// to the same actor.
        /// </summary>
        public bool IsOrdered = true;

        /// <summary>
        /// Enqueue timestamp of the event. We use this field when logging.
        /// </summary>
        public readonly Timestamp EnqueueTime = new();

        /// <summary>
        /// Dequeue timestamp of the event. When the event is enqueued, this field is set to the earliest timestamp
        /// the event can be dequeued. When the event is actually dequeued, this field is updated with the current
        /// timestamp. This is important because, for example, if the actor is blocked on a receive statement and
        /// waiting for an event with greater timestamp than an event waiting to be dequeued, we increment the time to
        /// the dequeue time of the blocking event to unblock the machine. Therefore, the actual dequeue time of the
        /// event can be greater than its planned dequeue time. We also use this field when logging.
        /// </summary>
        public readonly Timestamp DequeueTime = new();
    }
}
