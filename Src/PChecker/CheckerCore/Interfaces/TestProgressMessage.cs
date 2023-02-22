// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using PChecker.SmartSockets;

namespace PChecker.Interfaces
{
    [DataContract]
    internal class TestProgressMessage : SocketMessage
    {
        [DataMember]
        protected uint ProcessId { get; set; }

        [DataMember]
        private double Progress { get; set; }

        public TestProgressMessage(string id, string name, uint processId, double progress)
            : base(id, name)
        {
            ProcessId = processId;
            Progress = progress;
        }
    }
}
