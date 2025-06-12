using System;

namespace PChecker.Runtime.Exceptions
{
    public enum NonStandardReturn
    {
        Raise,
        Goto
    }

    public class PNonStandardReturnException : Exception
    {
        public PNonStandardReturnException()
        {
        }

        public PNonStandardReturnException(string message) : base(message)
        {
        }

        public PNonStandardReturnException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public NonStandardReturn ReturnKind { get; set; }
    }
}