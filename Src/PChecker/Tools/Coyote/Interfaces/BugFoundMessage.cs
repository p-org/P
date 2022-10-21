// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Coyote.SmartSockets;

namespace CoyoteTester.Interfaces
{
    [DataContract]
    public class BugFoundMessage : SocketMessage
    {
        [DataMember]
        public uint ProcessId { get; set; }

        public BugFoundMessage(string id, string name, uint processId)
            : base(id, name)
        {
            this.ProcessId = processId;
        }
    }
}
