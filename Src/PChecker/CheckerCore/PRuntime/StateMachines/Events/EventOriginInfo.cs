// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace PChecker.StateMachines.Events
{
    /// <summary>
    /// Contains the origin information of an <see cref="Event"/>.
    /// </summary>
    [DataContract]
    internal class EventOriginInfo
    {
        /// <summary>
        /// The sender state machine id.
        /// </summary>
        [DataMember]
        internal StateMachineId SenderStateMachineId { get; private set; }

        /// <summary>
        /// The sender state machine name.
        /// </summary>
        [DataMember]
        internal string SenderStateMachineName { get; private set; }

        /// <summary>
        /// The sender state name, if there is one.
        /// </summary>
        [DataMember]
        internal string SenderStateName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventOriginInfo"/> class.
        /// </summary>
        internal EventOriginInfo(StateMachineId senderStateMachineId, string senderMachineName, string senderStateName)
        {
            SenderStateMachineId = senderStateMachineId;
            SenderStateMachineName = senderMachineName;
            SenderStateName = senderStateName;
        }
    }
}