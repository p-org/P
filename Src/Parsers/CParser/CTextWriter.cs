namespace CParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;

    using Microsoft.Formula.API.Nodes;

    internal class CTextWriter
    {
        public TextWriter Writer
        {
            get;
            private set;
        }

        public bool PrintLineDirective
        {
            get;
            private set;
        }

        public void WriteLineDirective(int line, string indent)
        {
            if (!PrintLineDirective)
            {
                return;
            }

            Writer.WriteLine();

            if (line == 0)
            {
                Writer.WriteLine("//// No line information", line);
            }
            else
            {
                Writer.WriteLine("#line {0}", line);
            }

            Writer.Write(indent);
        }

        public void Write(string s)
        {
            Writer.Write(s);
        }

        public void Write(object o)
        {
            Writer.Write(o);
        }

        public void Write(string s, params object[] args)
        {
            Writer.Write(s, args);
        }

        public void WriteLine(string s)
        {
            Writer.WriteLine(s);
        }

        public void WriteLine(object o)
        {
            Writer.WriteLine(o);
        }

        public void WriteLine(string s, params object[] args)
        {
            Writer.WriteLine(s, args);
        }

        public CTextWriter(TextWriter writer, bool printLineDirective = false)
        {
            Contract.Requires(writer != null);
            Writer = writer;
            PrintLineDirective = printLineDirective;
        }
    }
}
