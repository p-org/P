// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PChecker.Actors.Events;

namespace PChecker.Actors.Timers
{
    /// <summary>
    /// Defines a timer elapsed event that is sent from a timer to the actor that owns the timer.
    /// </summary>
    public class TimerElapsedEvent : Event
    {
        /// <summary>
        /// Stores information about the timer.
        /// </summary>
        public TimerInfo Info { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerElapsedEvent"/> class.
        /// </summary>
        public TimerElapsedEvent()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerElapsedEvent"/> class.
        /// </summary>
        /// <param name="info">Stores information about the timer.</param>
        internal TimerElapsedEvent(TimerInfo info)
        {
            Info = info;
        }
    }
}
