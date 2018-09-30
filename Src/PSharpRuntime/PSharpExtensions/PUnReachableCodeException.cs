using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PSharpExtensions
{
    public class PUnReachableCodeException : Exception
    {
        public PUnReachableCodeException()
        {
        }

        public PUnReachableCodeException(string message) : base(message)
        {
        }

        public PUnReachableCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PUnReachableCodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
