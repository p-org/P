// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using PChecker.SmartSockets;

#pragma warning disable CS1591
namespace PChecker.Interfaces
{
    [DataContract]

    internal class BugFoundMessage : SocketMessage
    {
        [DataMember]
        public uint ProcessId { get; set; }

        public BugFoundMessage(string id, string name, uint processId)
            : base(id, name)
        {
            ProcessId = processId;
        }
    }
}
