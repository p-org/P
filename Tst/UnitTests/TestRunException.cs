using System;
using System.Runtime.Serialization;

namespace UnitTests
{
    [Serializable]
    public class TestRunException : Exception
    {
        public TestRunException(TestCaseError reason)
        {
            Reason = reason;
        }

        public TestRunException(TestCaseError reason, string message) : base(message)
        {
            Reason = reason;
        }

        public TestRunException(TestCaseError reason, string message, Exception inner) : base(message, inner)
        {
            Reason = reason;
        }

        protected TestRunException(
            TestCaseError reason,
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            Reason = reason;
        }

        public TestCaseError Reason { get; }
    }
}