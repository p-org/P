using System;
using System.IO;
using Microsoft.Pc;
using Microsoft.Pc.Backend;

namespace UnitTests
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

        public void WriteFile(CompiledFile file)
        {
            int nameLength = file.FileName.Length;
            int headerWidth = Math.Max(40, nameLength + 4);
            var hdash = new string('=', headerWidth);
            writer.WriteLine(hdash);
            int prePadding = (headerWidth - nameLength) / 2 - 1;
            int postPadding = headerWidth - prePadding - nameLength - 2;
            writer.WriteLine($"={new string(' ', prePadding)}{file.FileName}{new string(' ', postPadding)}=");
            writer.WriteLine(hdash);
            writer.Write(file.Contents);
            if (!file.Contents.EndsWith(Environment.NewLine))
            {
                writer.WriteLine();
            }
            writer.WriteLine(hdash);
            writer.WriteLine();
        }
    }
}