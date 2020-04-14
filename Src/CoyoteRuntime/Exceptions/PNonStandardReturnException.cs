using System;
using System.Runtime.Serialization;

namespace Plang.PrtSharp.Exceptions
{
    public enum NonStandardReturn
    {
        Raise,
        Goto,
        Pop
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

        protected PNonStandardReturnException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public NonStandardReturn ReturnKind { get; set; }
    }
}