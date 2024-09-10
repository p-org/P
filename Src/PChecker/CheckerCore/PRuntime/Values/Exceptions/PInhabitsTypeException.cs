using System;

namespace PChecker.PRuntime.Exceptions
{
    public class PInhabitsTypeException : Exception
    {
        public PInhabitsTypeException()
        {
        }

        public PInhabitsTypeException(string message) : base(message)
        {
        }

        public PInhabitsTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}