using System;

namespace PChecker.Runtime.Values.Exceptions
{
    public class PFrozenMutationException : Exception
    {
        public PFrozenMutationException()
        {
        }

        public PFrozenMutationException(string message) : base(message)
        {
        }
    }
}