using System;
using System.Runtime.Serialization;

namespace Plang.CSharpRuntime.Exceptions
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