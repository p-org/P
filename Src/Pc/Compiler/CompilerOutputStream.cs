using System.IO;
using Microsoft.Formula.API;

namespace Microsoft.Pc
{
    public class CompilerOutputStream : ICompilerOutput
    {
        TextWriter writer;

        public CompilerOutputStream(TextWriter writer)
        {
            this.writer = writer;
        }

        public void WriteMessage(string msg, SeverityKind severity)
        {
            this.writer.WriteLine(msg);
        }
    }
}