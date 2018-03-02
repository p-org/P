using System;
using System.IO;
using Microsoft.Pc.Backend;

namespace Microsoft.Pc
{
    public class StandardOutput : ICompilerOutput
    {
        public virtual void WriteMessage(string msg, SeverityKind severity)
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

        public virtual void WriteFile(CompiledFile file)
        {
            File.WriteAllText(file.FileName, file.Contents);
        }
    }
}