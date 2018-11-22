using System;
using System.IO;
using Microsoft.Pc.Backend;

namespace Microsoft.Pc
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
            var outputPath = Path.Combine(outputDirectory.FullName, file.FileName);
            File.WriteAllText(outputPath, file.Contents);
        }
    }
}