// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace PChecker.Actors.Events
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
        /// User-defined hash of the event. The default value is 0. Override to
        /// improve the accuracy of stateful techniques during testing.
        /// </summary>
        internal int HashedState { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventInfo"/> class.
        /// </summary>
        internal EventInfo(Event e)
        {
            EventName = e.GetType().FullName;
            HashedState = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventInfo"/> class.
        /// </summary>
        internal EventInfo(Event e, EventOriginInfo originInfo)
            : this(e)
        {
            OriginInfo = originInfo;
        }
    }
}