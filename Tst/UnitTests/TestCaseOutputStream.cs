using System;
using System.IO;
using Microsoft.Pc;
using Microsoft.Pc.Backend;

namespace UnitTests
{
    public class TestCaseOutputStream : ICompilerOutput
    {
        private readonly TextWriter stdout;
        private readonly TextWriter stderr;

        public TestCaseOutputStream(TextWriter stdout, TextWriter stderr)
        {
            this.stdout = stdout;
            this.stderr = stderr;
        }

        public void WriteMessage(string msg, SeverityKind severity)
        {
            switch (severity)
            {
                case SeverityKind.Info:
                    stdout.WriteLine(msg);
                    break;
                case SeverityKind.Warning:
                case SeverityKind.Error:
                    stderr.WriteLine(msg);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        }

        public void WriteFile(CompiledFile file)
        {
            int nameLength = file.FileName.Length;
            int headerWidth = Math.Max(40, nameLength + 4);
            var hdash = new string('=', headerWidth);
            stdout.WriteLine(hdash);
            int prePadding = (headerWidth - nameLength) / 2 - 1;
            int postPadding = headerWidth - prePadding - nameLength - 2;
            stdout.WriteLine($"={new string(' ', prePadding)}{file.FileName}{new string(' ', postPadding)}=");
            stdout.WriteLine(hdash);
            stdout.Write(file.Contents);
            if (!file.Contents.EndsWith(Environment.NewLine))
            {
                stdout.WriteLine();
            }
            stdout.WriteLine(hdash);
            stdout.WriteLine();
        }
    }
}