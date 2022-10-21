// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The wild card event.
    /// </summary>
    [DataContract]
    public sealed class WildCardEvent : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WildCardEvent"/> class.
        /// </summary>
        public WildCardEvent()
            : base()
        {
        }
    }
}
