using System.IO;
using Microsoft.Formula.API;
using Microsoft.Pc;

namespace UnitTests
{
    public class CompilerTestOutputStream : ICompilerOutput
    {
        private readonly TextWriter writer;

        public CompilerTestOutputStream(TextWriter writer)
        {
            this.writer = writer;
        }

        public void WriteMessage(string msg, SeverityKind severity)
        {
            writer.WriteLine("OUT: " + msg);
        }
    }
}