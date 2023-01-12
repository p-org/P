// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using PChecker.Actors.Events;
using PChecker.Actors.SharedObjects.Events;

namespace PChecker.Actors.SharedObjects
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
            Value = default;
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
                    Value = (T)opEvent.Value;
                    break;

                case SharedRegisterEvent.OperationType.Get:
                    SendEvent(opEvent.Sender, new SharedRegisterResponseEvent<T>(Value));
                    break;

                case SharedRegisterEvent.OperationType.Update:
                    var func = (Func<T, T>)opEvent.Func;
                    Value = func(Value);
                    SendEvent(opEvent.Sender, new SharedRegisterResponseEvent<T>(Value));
                    break;
            }
        }
    }
}
