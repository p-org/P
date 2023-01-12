// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PChecker.Actors.Events;

namespace PChecker.Actors.SharedObjects.Events
{
    /// <summary>
    /// Event containing the value of a shared counter.
    /// </summary>
    internal class SharedCounterResponseEvent : Event
    {
        /// <summary>
        /// Value.
        /// </summary>
        internal int Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedCounterResponseEvent"/> class.
        /// </summary>
        internal SharedCounterResponseEvent(int value)
        {
            Value = value;
        }
    }
}
