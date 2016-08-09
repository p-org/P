using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P.PRuntime
{
    /// <summary>
    /// This class denotes a contiguous fragment of P source code.
    /// </summary>
    public class PSourceContext
    {
        public PSourceContext()
            : this(0, 0, 0)
        {
        }

        /// <summary>
        /// Construct a PSourceContext for the given docIndex, line number, and  start column.
        /// </summary>
        /// <param name="docIndex">The zero-based index of the source file referenced.</param>
        /// <param name="lineNumber">The line number of the source context.</param>
        /// <param name="endColumn">The starting column number (not inclusive)</param>
        public PSourceContext(int docIndex, int lineNumber, int startColumn)
        {
            this.docIndex = docIndex;
            this.lineNumber = lineNumber;
            this.startColumn = startColumn;
        }

        /// <summary>
        /// Returns the index of the source file referenced.
        /// </summary>
        public int DocIndex { get { return docIndex; } }

        private int docIndex;

        /// <summary>
        /// Returns the line number of the source fragment.
        /// </summary>
        public int LineNumber { get { return lineNumber; } }

        private int lineNumber;

        /// <summary>
        /// Returns the starting column number of the source fragment.
        /// </summary>
        public int StartColumn { get { return startColumn; } }

        private int startColumn;

        public void CopyTo(PSourceContext other)
        {
            other.docIndex = this.docIndex;
            other.lineNumber = this.lineNumber;
            other.startColumn = this.startColumn;
        }
    }
}
