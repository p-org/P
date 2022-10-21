// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Coyote.SmartSockets;

namespace CoyoteTester.Interfaces
{
    [DataContract]
    public class TestTraceMessage : SocketMessage
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
            this.ProcessId = processId;
            this.FileName = fileName;
            this.Contents = textContents;
        }
    }
}
