// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Coyote.Actors.SharedObjects
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
            this.Counter = 0;
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
                    this.SendEvent(opEvent.Sender, new SharedCounterResponseEvent(this.Counter));
                    this.Counter = opEvent.Value;
                    break;

                case SharedCounterEvent.OperationType.Get:
                    this.SendEvent(opEvent.Sender, new SharedCounterResponseEvent(this.Counter));
                    break;

                case SharedCounterEvent.OperationType.Increment:
                    this.Counter++;
                    break;

                case SharedCounterEvent.OperationType.Decrement:
                    this.Counter--;
                    break;

                case SharedCounterEvent.OperationType.Add:
                    this.Counter += opEvent.Value;
                    this.SendEvent(opEvent.Sender, new SharedCounterResponseEvent(this.Counter));
                    break;

                case SharedCounterEvent.OperationType.CompareExchange:
                    this.SendEvent(opEvent.Sender, new SharedCounterResponseEvent(this.Counter));
                    if (this.Counter == opEvent.Comparand)
                    {
                        this.Counter = opEvent.Value;
                    }

                    break;

                default:
                    throw new System.ArgumentOutOfRangeException("Unsupported SharedCounter operation: " + opEvent.Operation);
            }
        }
    }
}
