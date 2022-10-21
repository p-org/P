// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Coyote.SmartSockets;
using Microsoft.Coyote.SystematicTesting;

namespace CoyoteTester.Interfaces
{
    [DataContract]
    public class TestReportMessage : SocketMessage
    {
        [DataMember]
        public uint ProcessId { get; set; }

        [DataMember]
        public TestReport TestReport { get; set; }

        public TestReportMessage(string id, string name, uint processId, TestReport testReport)
            : base(id, name)
        {
            this.ProcessId = processId;
            this.TestReport = testReport;
        }
    }
}
