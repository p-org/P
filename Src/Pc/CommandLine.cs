namespace Microsoft.Pc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Domains;
    using Microsoft.Formula.API;
    using Microsoft.Formula.API.Generators;
    using Microsoft.Formula.API.Nodes;

    class CommandLine
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("USAGE: Pc.exe file.p");
                return;
            }

            List<Flag> flags;
            var comp = new Compiler(args[0]);
            var result = comp.Compile(out flags);
            WriteFlags(flags);

            if (!result)
            {
                WriteMessage("Compilation failed", SeverityKind.Error);
            }
        }

        private static void WriteFlags(List<Flag> flags)
        {
            foreach (var f in flags)
            {
                WriteMessage(
                    string.Format("{0} ({1}, {2}): {3}",
                    f.ProgramName == null ? "?" : f.ProgramName.ToString(),
                    f.Span.StartLine,
                    f.Span.StartCol,
                    f.Message), f.Severity);               
            }
        }

        private static void WriteMessage(string msg, SeverityKind severity)
        {
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

            Console.Write(msg);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
