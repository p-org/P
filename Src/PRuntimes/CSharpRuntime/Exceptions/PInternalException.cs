using System;
using System.Runtime.Serialization;

namespace Plang.CSharpRuntime.Exceptions
{
    public class PInternalException : Exception
    {
        public PInternalException()
        {
        }

        public PInternalException(string message) : base(message)
        {
        }

        public PInternalException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PInternalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}