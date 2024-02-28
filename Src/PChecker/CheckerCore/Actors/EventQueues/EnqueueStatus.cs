// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PChecker.Actors.EventQueues
{
    /// <summary>
    /// The status returned as the result of an enqueue operation.
    /// </summary>
    internal enum EnqueueStatus
    {
        /// <summary>
        /// The event handler is already running.
        /// </summary>
        EventHandlerRunning = 0,

        /// <summary>
        /// The event handler is not running.
        /// </summary>
        EventHandlerNotRunning,

        /// <summary>
        /// The event was consumed at a receive statement.
        /// </summary>
        Received,

        /// <summary>
        /// There is no next event available to dequeue and handle.
        /// </summary>
        NextEventUnavailable,

        /// <summary>
        /// The event was dropped.
        /// </summary>
        Dropped
    }
}