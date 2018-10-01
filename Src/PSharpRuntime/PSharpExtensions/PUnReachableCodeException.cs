using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PSharpExtensions
{
    public class PUnreachableCodeException : Exception
    {
        public PUnreachableCodeException()
        {
        }

        public PUnreachableCodeException(string message) : base(message)
        {
        }

        public PUnreachableCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PUnreachableCodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
