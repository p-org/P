using System;
using System.Runtime.Serialization;

namespace UnitTests.Core
{
    /// <inheritdoc />
    /// <summary>
    ///     An exception to be thrown when something fails while creating or executing a compiler test.
    /// </summary>
    [Serializable]
    public class CompilerTestException : Exception
    {
        public CompilerTestException(TestCaseError reason)
        {
            Reason = reason;
        }

        public CompilerTestException(TestCaseError reason, string message) : base(message)
        {
            Reason = reason;
        }

        public CompilerTestException(TestCaseError reason, string message, Exception inner) : base(message, inner)
        {
            Reason = reason;
        }

        protected CompilerTestException(
            TestCaseError reason,
            SerializationInfo info,
            StreamingContext context)
        {
            Reason = reason;
        }

        public TestCaseError Reason { get; }
    }
}