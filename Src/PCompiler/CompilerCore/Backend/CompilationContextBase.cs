using System.IO;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler.Backend
{
    public abstract class CompilationContextBase
    {
        private bool lineHasBeenIndented;

        protected CompilationContextBase(ICompilerConfiguration job)
        {
            Job = job;
            Handler = job.Handler;
            ProjectName = job.ProjectName;
            LocationResolver = job.LocationResolver;
        }

        public ICompilerConfiguration Job { get; }
        private int IndentationLevel { get; set; }

        public string ProjectName { get; }
        public ITranslationErrorHandler Handler { get; }
        public ILocationResolver LocationResolver { get; }

        /// <summary>
        ///     Writes a line to the given output stream, taking curly brace indentation into account.
        ///     This function is called extremely frequently, so some care has been taken to optimize
        ///     it for performance. In particular, it does not allocate any memory that isn't part
        ///     of the output stream.
        /// </summary>
        /// <param name="output">The output stream to write to</param>
        /// <param name="format">The line to print</param>
        public void WriteLine(TextWriter output, string format = "")
        {
            // Unindent for every } at the beginning of the line, save the index
            // of one past the last leading }.
            int i;
            for (i = 0; i < format.Length; i++) { 
                if (format[i] == '}' || format[i] == ')') { 
                    IndentationLevel--;
                }
                else if (!char.IsWhiteSpace(format[i]))
                {
                    break;
                }
            }

            // Do not indent preprocessor lines.
            if (!(format.Length > 0 && format[0] == '#') && !lineHasBeenIndented)
            {
                for (var j = 0; j < 4 * IndentationLevel; j++)
                {
                    output.Write(' ');
                }
            }

            output.WriteLine(format);

            lineHasBeenIndented = false;

            // Compute indentation for future lines starting from after last leading }.
            for (; i < format.Length; i++)
                if (format[i] == '{' || format[i] == '(')
                    IndentationLevel++;
                else if (format[i] == '}' || format[i] == ')') IndentationLevel--;
        }

        public void Write(TextWriter output, string format)
        {
            // Unindent for every } at the beginning of the line, store the index
            // of one past the last leading }.
            int i;
            for (i = 0; i < format.Length; i++) { 
                if (format[i] == '}' || format[i] == ')') { 
                    IndentationLevel--;
                }
                else if (!char.IsWhiteSpace(format[i]))
                {
                    break;
                }
            }

            // Do not indent preprocessor lines.
            if (!format.StartsWith("#") && !lineHasBeenIndented)
            {
                for (var j = 0; j < 4 * IndentationLevel; j++)
                {
                    output.Write(' ');
                }
            }

            output.Write(format);

            lineHasBeenIndented = true;

            // Compute indentation for future lines starting from after last leading }.
            for (; i < format.Length; i++)
                if (format[i] == '{' || format[i] == '(')
                    IndentationLevel++;
                else if (format[i] == '}' || format[i] == ')') IndentationLevel--;
        }
    }
}