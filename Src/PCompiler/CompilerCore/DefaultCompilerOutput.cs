using System;
using System.IO;
using Plang.Compiler.Backend;

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
            var defaultColor = Console.ForegroundColor;
            switch (severity)
            {
                case SeverityKind.Info:
                    Console.ForegroundColor = ConsoleColor.White;
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
            var outputPath = Path.Combine(outputDirectory.FullName, file.FileName);
            File.WriteAllText(outputPath, file.Contents);
        }

        public void WriteError(string msg)
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ForegroundColor = defaultColor;
        }

        public void WriteInfo(string msg)
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(msg);
            Console.ForegroundColor = defaultColor;
        }

        public void WriteWarning(string msg)
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ForegroundColor = defaultColor;
        }
    }
}