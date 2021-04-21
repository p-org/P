using Plang.Compiler.Backend;
using System;
using System.IO;

namespace Plang.Compiler
{
    public class DefaultCompilerOutput : ICompilerOutput
    {
        private readonly DirectoryInfo outputDirectory;
        private readonly DirectoryInfo aspectjOutputDirectory;

        public DefaultCompilerOutput(DirectoryInfo outputDirectory, DirectoryInfo aspectjOutputDirectory = null)
        {
            this.outputDirectory = outputDirectory;
            this.aspectjOutputDirectory = aspectjOutputDirectory;
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
            if (Path.GetExtension(file.FileName) == ".aj"){
                string outputPath = Path.Combine(aspectjOutputDirectory.FullName, file.FileName);
                File.WriteAllText(outputPath, file.Contents);
            } else {
                string outputPath = Path.Combine(outputDirectory.FullName, file.FileName);
                File.WriteAllText(outputPath, file.Contents);
            }
        }

        public void WriteError(string msg)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ForegroundColor = defaultColor;
        }

        public void WriteInfo(string msg)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(msg);
            Console.ForegroundColor = defaultColor;
        }

        public void WriteWarning(string msg)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ForegroundColor = defaultColor;
        }
    }
}