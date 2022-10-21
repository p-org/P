// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The halt event.
    /// </summary>
    [DataContract]
    public sealed class HaltEvent : Event
    {
        /// <summary>
        /// Gets a <see cref="HaltEvent"/> instance.
        /// </summary>
        public static HaltEvent Instance { get; } = new HaltEvent();

        /// <summary>
        /// Initializes a new instance of the <see cref="HaltEvent"/> class.
        /// </summary>
        private HaltEvent()
            : base()
        {
        }
    }
}
