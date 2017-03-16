using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace P.Tester
{

    /// <summary>
    /// The main entry point for PTester
    /// </summary>
    public class PTester
    {
        internal enum PTesterResult
        {
            Success = 0,
            Canceled = 1,
            ErrorFound = 2,
            InternalRuntimeError = 3,
            InvalidParameters = 4,
        }
    }
}
