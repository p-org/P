// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using PChecker.SystematicTesting;
using PChecker.SmartSockets;

namespace PChecker.Interfaces
{
    [DataContract]
    internal class TestReportMessage : SocketMessage
    {
        [DataMember]
        public uint ProcessId { get; set; }

        [DataMember]
        public TestReport TestReport { get; set; }

        public TestReportMessage(string id, string name, uint processId, TestReport testReport)
            : base(id, name)
        {
            ProcessId = processId;
            TestReport = testReport;
        }
    }
}
