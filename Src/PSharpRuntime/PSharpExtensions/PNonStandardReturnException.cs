using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PSharpExtensions
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