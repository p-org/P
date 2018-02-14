using System.IO;

namespace Microsoft.Pc
{
    public class CompilerOutputStream : ICompilerOutput
    {
        private readonly TextWriter writer;

        public CompilerOutputStream(TextWriter writer)
        {
            this.writer = writer;
        }

        public void WriteMessage(string msg, SeverityKind severity)
        {
            writer.WriteLine(msg);
        }
    }
}