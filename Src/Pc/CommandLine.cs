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

    class CommandLineOptions
    {
        public LivenessOption liveness;
        public string outputDir;
        public bool erase;
        public bool emitLineDirectives;
        public bool emitHeaderComment;

        public CommandLineOptions()
        {
            this.liveness = LivenessOption.None;
            this.outputDir = ".";
            this.erase = true;
            this.emitLineDirectives = false;
            this.emitHeaderComment = false;
        }
    }

    class CommandLine
    {
        static void Main(string[] args)
        {
            string inputFile = null;
            var options = new CommandLineOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string colonArg = null;
                if (arg.StartsWith("/"))
                {
                    var colonIndex = arg.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        arg = args[i].Substring(0, colonIndex);
                        colonArg = args[i].Substring(colonIndex + 1);
                    }

                    switch (arg)
                    {
                        case "/outputDir":
                            options.outputDir = colonArg;
                            break;
                        case "/doNotErase":
                            if (colonArg != null)
                                goto error;
                            options.erase = false;
                            break;
                        case "/emitLineDirectives":
                            if (colonArg != null)
                                goto error;
                            options.emitLineDirectives = true;
                            break;
                        case "/emitHeaderComment":
                            if (colonArg != null)
                                goto error;
                            options.emitHeaderComment = true;
                            break;
                        case "/liveness":
                            if (colonArg == null)
                                options.liveness = LivenessOption.Standard;
                            else if (colonArg == "mace")
                                options.liveness = LivenessOption.Mace;
                            else
                                goto error;
                            break;
                        default:
                            goto error;
                    }
                }
                else
                {
                    if (inputFile == null)
                    {
                        inputFile = arg;
                    }
                    else
                    {
                        goto error;
                    }
                }
            }

            var comp = new Compiler(args[0], options);
            List<Flag> flags;
            var result = comp.Compile(out flags);
            WriteFlags(flags);

            if (!result)
            {
                WriteMessageLine("Compilation failed", SeverityKind.Error);
            }
            return;

        error:
            {
                Console.WriteLine("USAGE: Pc.exe file.p [/outputDir:path] [/doNotErase] [/emitLineDirectives] [/emitHeaderComment] [/liveness[:mace]]");
                return;
            }
        }

        private static void WriteFlags(List<Flag> flags)
        {
            foreach (var f in flags)
            {
                WriteMessageLine(
                    string.Format("{0} ({1}, {2}): {3}",
                    f.ProgramName == null ? "?" : (f.ProgramName.Uri.IsFile ? f.ProgramName.Uri.AbsolutePath : f.ProgramName.ToString()),
                    f.Span.StartLine,
                    f.Span.StartCol,
                    f.Message), f.Severity);               
            }
        }

        private static void WriteMessageLine(string msg, SeverityKind severity)
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

            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
