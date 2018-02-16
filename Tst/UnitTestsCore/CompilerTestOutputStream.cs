using System.IO;
using Microsoft.Pc;

namespace UnitTestsCore
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