using Plang.Compiler.Backend;
using System;
using System.IO;

namespace Plang.Compiler
{
    public class DefaultCompilerOutput : ICompilerOutput
    {
        private readonly DirectoryInfo outputDirectory;

        public DefaultCompilerOutput(DirectoryInfo outputDirectory)
        {
            this.outputDirectory = outputDirectory;
        }

        public void WriteMessage(string msg, SeverityKind severity)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            switch (severity)
            {
                case SeverityKind.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;

                case SeverityKind.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case SeverityKind.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine(msg);
            Console.ForegroundColor = defaultColor;
        }

        public void WriteFile(CompiledFile file)
        {
            string outputPath = Path.Combine(outputDirectory.FullName, file.FileName);
            File.WriteAllText(outputPath, file.Contents);
        }
    }
}