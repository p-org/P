// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace PChecker.Actors.Events
{
    /// <summary>
    /// Contains the origin information of an <see cref="Event"/>.
    /// </summary>
    [DataContract]
    internal class EventOriginInfo
    {
        /// <summary>
        /// The sender actor id.
        /// </summary>
        [DataMember]
        internal ActorId SenderActorId { get; private set; }

        /// <summary>
        /// The sender actor name.
        /// </summary>
        [DataMember]
        internal string SenderActorName { get; private set; }

        /// <summary>
        /// The sender state name, if there is one.
        /// </summary>
        [DataMember]
        internal string SenderStateName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventOriginInfo"/> class.
        /// </summary>
        internal EventOriginInfo(ActorId senderActorId, string senderMachineName, string senderStateName)
        {
            SenderActorId = senderActorId;
            SenderActorName = senderMachineName;
            SenderStateName = senderStateName;
        }
    }
}