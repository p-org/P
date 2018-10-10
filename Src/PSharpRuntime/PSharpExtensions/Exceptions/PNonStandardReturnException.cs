using System;
using System.Runtime.Serialization;

namespace PrtSharp
{
    public enum NonStandardReturn
    {
        Raise,
        Goto,
        Pop
    }

    class PNonStandardReturnException : Exception
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

        protected PNonStandardReturnException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public NonStandardReturn ReturnKind { get; set; }
    }
}