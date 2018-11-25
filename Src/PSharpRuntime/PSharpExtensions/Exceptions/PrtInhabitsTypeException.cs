using System;
using System.Runtime.Serialization;

namespace Plang.PrtSharp.Exceptions
{
    public class PrtInhabitsTypeException : Exception
    {
        public PrtInhabitsTypeException()
        {
        }

        public PrtInhabitsTypeException(string message) : base(message)
        {
        }

        public PrtInhabitsTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PrtInhabitsTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}