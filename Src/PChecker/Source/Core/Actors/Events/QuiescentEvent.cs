// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Signals that an actor has reached quiescence.
    /// </summary>
    [DataContract]
    internal sealed class QuiescentEvent : Event
    {
        /// <summary>
        /// The id of the actor that has reached quiescence.
        /// </summary>
        public ActorId ActorId;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuiescentEvent"/> class.
        /// </summary>
        /// <param name="id">The id of the actor that has reached quiescence.</param>
        public QuiescentEvent(ActorId id)
        {
            this.ActorId = id;
        }
    }
}
