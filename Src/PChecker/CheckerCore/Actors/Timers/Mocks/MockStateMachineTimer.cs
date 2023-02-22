// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PChecker.Actors.Events;

namespace PChecker.Actors.Timers.Mocks
{
    /// <summary>
    /// A mock timer that replaces <see cref="ActorTimer"/> during testing.
    /// It is implemented as a state machine.
    /// </summary>
    internal class MockStateMachineTimer : StateMachine, IActorTimer
    {
        /// <summary>
        /// Stores information about this timer.
        /// </summary>
        private TimerInfo TimerInfo;

        /// <summary>
        /// Stores information about this timer.
        /// </summary>
        TimerInfo IActorTimer.Info => TimerInfo;

        /// <summary>
        /// The actor that owns this timer.
        /// </summary>
        private Actor Owner;

        /// <summary>
        /// The timeout event.
        /// </summary>
        private TimerElapsedEvent TimeoutEvent;

        /// <summary>
        /// Adjusts the probability of firing a timeout event.
        /// </summary>
        private uint Delay;

        [Start]
        [OnEntry(nameof(Setup))]
        [OnEventDoAction(typeof(DefaultEvent), nameof(HandleTimeout))]
        private class Active : State
        {
        }

        /// <summary>
        /// Initializes the timer with the specified checkerConfiguration.
        /// </summary>
        private void Setup(Event e)
        {
            TimerInfo = (e as TimerSetupEvent).Info;
            Owner = (e as TimerSetupEvent).Owner;
            Delay = (e as TimerSetupEvent).Delay;
            TimeoutEvent = TimerInfo.CustomEvent;
            if (TimeoutEvent is null)
            {
                TimeoutEvent = new TimerElapsedEvent(TimerInfo);
            }
            else
            {
                TimeoutEvent.Info = TimerInfo;
            }
        }

        /// <summary>
        /// Handles the timeout.
        /// </summary>
        private void HandleTimeout()
        {
            // Try to send the next timeout event.
            var isTimeoutSent = false;
            var delay = (int)Delay > 0 ? (int)Delay : 1;

            // TODO: do we need some normalization of delay here ... ?
            if ((RandomInteger(delay) == 0) && RandomBoolean())
            {
                // The probability of sending a timeout event is at most 1/N.
                SendEvent(Owner.Id, TimeoutEvent);
                isTimeoutSent = true;
            }

            // If non-periodic, and a timeout was successfully sent, then become
            // inactive until disposal. Else retry.
            if (isTimeoutSent && TimerInfo.Period.TotalMilliseconds < 0)
            {
                RaiseGotoStateEvent<Inactive>();
            }
        }

        private class Inactive : State
        {
        }

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is MockStateMachineTimer timer)
            {
                return Id == timer.Id;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        public override string ToString() => Id.Name;

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
            Runtime.SendEvent(Id, HaltEvent.Instance);
        }
    }
}
