using System.IO;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc.Backend
{
    public abstract class CompilationContextBase
    {
        private bool lineHasBeenIndented;
        private int IndentationLevel { get; set; }

        public string ProjectName { get; }
        public ITranslationErrorHandler Handler { get; }
        public ILocationResolver LocationResolver { get; }

        protected CompilationContextBase(ICompilationJob job)
        {
            Handler = job.Handler;
            ProjectName = job.ProjectName;
            LocationResolver = job.LocationResolver;
        }

        public void WriteLine(TextWriter output, string format = "")
        {
            // Unindent for every } at the beginning of the line, save the index 
            // of one past the last leading }.
            int i;
            for (i = 0; i < format.Length; i++)
            {
                if (format[i] == '}')
                {
                    IndentationLevel--;
                }
                else if (!char.IsWhiteSpace(format[i]))
                {
                    break;
                }
            }

            // Do not indent preprocessor lines.
            var indentation = new string(' ', 4 * IndentationLevel);
            if (format.StartsWith("#") || lineHasBeenIndented)
            {
                indentation = "";
            }

            output.WriteLine(indentation + format);
            lineHasBeenIndented = false;

            // Compute indentation for future lines starting from after last leading }.
            for (; i < format.Length; i++)
            {
                if (format[i] == '{')
                {
                    IndentationLevel++;
                }
                else if (format[i] == '}')
                {
                    IndentationLevel--;
                }
            }
        }

        public void Write(TextWriter output, string format)
        {
            // Unindent for every } at the beginning of the line, save the index 
            // of one past the last leading }.
            int i;
            for (i = 0; i < format.Length; i++)
            {
                if (format[i] == '}')
                {
                    IndentationLevel--;
                }
                else if (!char.IsWhiteSpace(format[i]))
                {
                    break;
                }
            }

            // Do not indent preprocessor lines.
            var indentation = new string(' ', 4 * IndentationLevel);
            if (format.StartsWith("#") || lineHasBeenIndented)
            {
                indentation = "";
            }

            output.Write(indentation + format);
            lineHasBeenIndented = true;

            // Compute indentation for future lines starting from after last leading }.
            for (; i < format.Length; i++)
            {
                if (format[i] == '{')
                {
                    IndentationLevel++;
                }
                else if (format[i] == '}')
                {
                    IndentationLevel--;
                }
            }
        }
    }
}
