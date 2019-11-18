using System;
using System.Runtime.Serialization;

namespace Plang.PrtSharp.Exceptions
{
    public class PUnreachableCodeException : Exception
    {
        public PUnreachableCodeException()
        {
        }

        public PUnreachableCodeException(string message) : base(message)
        {
        }

        public PUnreachableCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PUnreachableCodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}