using System;
using System.Runtime.Serialization;

namespace PrtSharp.Exceptions
{
    public class PFrozenMutationException : Exception
    {
        public PFrozenMutationException()
        {
        }

        public PFrozenMutationException(string message) : base(message)
        {
        }

        public PFrozenMutationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PFrozenMutationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}