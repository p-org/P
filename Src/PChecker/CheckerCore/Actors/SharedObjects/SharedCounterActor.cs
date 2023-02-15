// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using PChecker.Actors.Events;
using PChecker.Actors.SharedObjects.Events;

namespace PChecker.Actors.SharedObjects
{
    /// <summary>
    /// A shared counter modeled using an actor for testing.
    /// </summary>
    [OnEventDoAction(typeof(SharedCounterEvent), nameof(ProcessEvent))]
    internal sealed class SharedCounterActor : Actor
    {
        /// <summary>
        /// The value of the shared counter.
        /// </summary>
        private int Counter;

        /// <summary>
        /// Initializes the actor.
        /// </summary>
        protected override Task OnInitializeAsync(Event initialEvent)
        {
            Counter = 0;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        private void ProcessEvent(Event e)
        {
            var opEvent = e as SharedCounterEvent;
            switch (opEvent.Operation)
            {
                case SharedCounterEvent.OperationType.Set:
                    SendEvent(opEvent.Sender, new SharedCounterResponseEvent(Counter));
                    Counter = opEvent.Value;
                    break;

                case SharedCounterEvent.OperationType.Get:
                    SendEvent(opEvent.Sender, new SharedCounterResponseEvent(Counter));
                    break;

                case SharedCounterEvent.OperationType.Increment:
                    Counter++;
                    break;

                case SharedCounterEvent.OperationType.Decrement:
                    Counter--;
                    break;

                case SharedCounterEvent.OperationType.Add:
                    Counter += opEvent.Value;
                    SendEvent(opEvent.Sender, new SharedCounterResponseEvent(Counter));
                    break;

                case SharedCounterEvent.OperationType.CompareExchange:
                    SendEvent(opEvent.Sender, new SharedCounterResponseEvent(Counter));
                    if (Counter == opEvent.Comparand)
                    {
                        Counter = opEvent.Value;
                    }

                    break;

                default:
                    throw new System.ArgumentOutOfRangeException("Unsupported SharedCounter operation: " + opEvent.Operation);
            }
        }
    }
}
