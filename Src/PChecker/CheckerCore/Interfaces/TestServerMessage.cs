// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using PChecker.SmartSockets;

namespace PChecker.Interfaces
{
    [DataContract]
    internal class TestServerMessage : SocketMessage
    {
        [DataMember]
        public bool Stop { get; set; }

        public TestServerMessage(string id, string name)
            : base(id, name)
        {
        }
    }
}
