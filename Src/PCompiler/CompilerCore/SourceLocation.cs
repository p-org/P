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
            // File can be null when a diagnostic is synthesized from a location
            // without a backing source file (e.g. EmptyContext, internal errors).
            // Throwing ArgumentException here would partial-flush the user's
            // collected diagnostics with a confusing stack trace mid-loop —
            // fall back to a clear sentinel instead.
            if (File == null) return $"<no source>:{Line}:{Column}";
            return $"{Path.GetRelativePath(Directory.GetCurrentDirectory(), File.FullName)}:{Line}:{Column}";
        }
    }
}