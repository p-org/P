// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Actors.SharedObjects
{
    /// <summary>
    /// A shared register modeled using an actor for testing.
    /// </summary>
    [OnEventDoAction(typeof(SharedRegisterEvent), nameof(ProcessEvent))]
    internal sealed class SharedRegisterActor<T> : Actor
        where T : struct
    {
        /// <summary>
        /// The value of the shared register.
        /// </summary>
        private T Value;

        /// <summary>
        /// Initializes the actor.
        /// </summary>
        protected override Task OnInitializeAsync(Event initialEvent)
        {
            this.Value = default;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        private void ProcessEvent(Event e)
        {
            var opEvent = e as SharedRegisterEvent;
            switch (opEvent.Operation)
            {
                case SharedRegisterEvent.OperationType.Set:
                    this.Value = (T)opEvent.Value;
                    break;

                case SharedRegisterEvent.OperationType.Get:
                    this.SendEvent(opEvent.Sender, new SharedRegisterResponseEvent<T>(this.Value));
                    break;

                case SharedRegisterEvent.OperationType.Update:
                    var func = (Func<T, T>)opEvent.Func;
                    this.Value = func(this.Value);
                    this.SendEvent(opEvent.Sender, new SharedRegisterResponseEvent<T>(this.Value));
                    break;
            }
        }
    }
}
