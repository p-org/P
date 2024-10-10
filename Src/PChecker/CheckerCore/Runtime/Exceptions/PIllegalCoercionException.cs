using System;

namespace PChecker.Runtime.Exceptions
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
    }
}