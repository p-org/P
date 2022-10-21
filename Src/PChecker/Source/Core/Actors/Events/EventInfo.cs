// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Contains an <see cref="Event"/>, and its associated metadata.
    /// </summary>
    [DataContract]
    internal class EventInfo
    {
        /// <summary>
        /// Event name.
        /// </summary>
        [DataMember]
        internal string EventName { get; private set; }

        /// <summary>
        /// Information regarding the event origin.
        /// </summary>
        [DataMember]
        internal EventOriginInfo OriginInfo { get; private set; }

        /// <summary>
        /// True if this event must always be handled, else false.
        /// </summary>
        internal bool MustHandle { get; set; }

        /// <summary>
        /// Asserts that there must not be more than N instances of the
        /// event type in the inbox queue of the receiver.
        /// </summary>
        internal int Assert { get; set; }

        /// <summary>
        /// User-defined hash of the event. The default value is 0. Override to
        /// improve the accuracy of stateful techniques during testing.
        /// </summary>
        internal int HashedState { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventInfo"/> class.
        /// </summary>
        internal EventInfo(Event e)
        {
            this.EventName = e.GetType().FullName;
            this.MustHandle = false;
            this.Assert = -1;
            this.HashedState = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventInfo"/> class.
        /// </summary>
        internal EventInfo(Event e, EventOriginInfo originInfo)
            : this(e)
        {
            this.OriginInfo = originInfo;
        }
    }
}
