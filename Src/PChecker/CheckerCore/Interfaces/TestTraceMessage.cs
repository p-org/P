// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using PChecker.SmartSockets;

namespace PChecker.Interfaces
{
    [DataContract]
    internal class TestTraceMessage : SocketMessage
    {
        [DataMember]
        public uint ProcessId { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public string Contents { get; set; }

        public TestTraceMessage(string id, string name, uint processId, string fileName, string textContents)
            : base(id, name)
        {
            ProcessId = processId;
            FileName = fileName;
            Contents = textContents;
        }
    }
}
