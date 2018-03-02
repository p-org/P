using System.IO;
using Microsoft.Pc;

namespace UnitTestsCore
{
    public class CompilerTestOutputStream : StandardOutput
    {
        private readonly TextWriter writer;

        public CompilerTestOutputStream(TextWriter writer)
        {
            this.writer = writer;
        }

        public override void WriteMessage(string msg, SeverityKind severity)
        {
            writer.WriteLine("OUT: " + msg);
        }
    }
}
