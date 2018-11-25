using System;
using System.Runtime.Serialization;

namespace Plang.PrtSharp.Exceptions
{
    public class PIllegalCoercionException : Exception
    {
        public PIllegalCoercionException()
        {
        }

        public PIllegalCoercionException(string message) : base(message)
        {
        }

        public PIllegalCoercionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PIllegalCoercionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}