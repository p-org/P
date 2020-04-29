using System;
using System.IO;

namespace Plang.Compiler
{
    public class SourceLocation
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public FileInfo File { get; set; }

        public override string ToString()
        {
            return File == null ? throw new ArgumentException() : $"{File.Name}:{Line},{Column}";
        }
    }
}