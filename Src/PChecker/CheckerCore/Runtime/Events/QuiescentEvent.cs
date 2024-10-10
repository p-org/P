// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using PChecker.Runtime.StateMachines;

namespace PChecker.Runtime.Events
{
    /// <summary>
    /// Signals that an state machine has reached quiescence.
    /// </summary>
    [DataContract]
    internal sealed class QuiescentEvent : Event
    {
        /// <summary>
        /// The id of the state machine that has reached quiescence.
        /// </summary>
        public StateMachineId StateMachineId;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuiescentEvent"/> class.
        /// </summary>
        /// <param name="id">The id of the state machine that has reached quiescence.</param>
        public QuiescentEvent(StateMachineId id)
        {
            StateMachineId = id;
        }
    }
}