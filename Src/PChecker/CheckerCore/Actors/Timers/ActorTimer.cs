// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;

namespace PChecker.Actors.Timers
{
    /// <summary>
    /// A timer that can send timeout events to its owner actor.
    /// </summary>
    internal sealed class ActorTimer : IActorTimer
    {
        /// <inheritdoc/>
        public TimerInfo Info { get; private set; }

        /// <summary>
        /// The actor that owns this timer.
        /// </summary>
        private readonly Actor Owner;

        /// <summary>
        /// The internal timer.
        /// </summary>
        private readonly Timer InternalTimer;

        /// <summary>
        /// The timeout event.
        /// </summary>
        private readonly TimerElapsedEvent TimeoutEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorTimer"/> class.
        /// </summary>
        /// <param name="info">Stores information about this timer.</param>
        /// <param name="owner">The actor that owns this timer.</param>
        public ActorTimer(TimerInfo info, Actor owner)
        {
            Info = info;
            Owner = owner;

            TimeoutEvent = Info.CustomEvent;
            if (TimeoutEvent is null)
            {
                TimeoutEvent = new TimerElapsedEvent(Info);
            }
            else
            {
                TimeoutEvent.Info = Info;
            }

            // To avoid a race condition between assigning the field of the timer
            // and HandleTimeout accessing the field before the assignment happens,
            // we first create a timer that cannot get triggered, then assign it to
            // the field, and finally we start the timer.
            InternalTimer = new Timer(HandleTimeout, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            InternalTimer.Change(Info.DueTime, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Handles the timeout.
        /// </summary>
        private void HandleTimeout(object state)
        {
            // Send a timeout event.
            Owner.Runtime.SendEvent(Owner.Id, TimeoutEvent);

            if (Info.Period.TotalMilliseconds >= 0)
            {
                // The timer is periodic, so schedule the next timeout.
                try
                {
                    // Start the next timeout period.
                    InternalTimer.Change(Info.Period, Timeout.InfiniteTimeSpan);
                }
                catch (ObjectDisposedException)
                {
                    // Benign race condition while disposing the timer.
                }
            }
        }

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is ActorTimer timer)
            {
                return Info == timer.Info;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => Info.GetHashCode();

        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        public override string ToString() => Info.ToString();

        /// <summary>
        /// Indicates whether the specified <see cref="ActorId"/> is equal
        /// to the current <see cref="ActorId"/>.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(ActorTimer other)
        {
            return Equals((object)other);
        }

        /// <summary>
        /// Disposes the resources held by this timer.
        /// </summary>
        public void Dispose()
        {
            InternalTimer.Dispose();
        }
    }
}
