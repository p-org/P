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

    public class CommandLineOptions
    {
        public bool analyzeOnly;
        public LivenessOption liveness;
        public string outputDir;
        public bool outputFormula;
        public bool erase;
        public bool shortFilenames;
        public bool printTypeInference;

        public CommandLineOptions()
        {
            this.analyzeOnly = false;
            this.liveness = LivenessOption.None;
            this.outputDir = null;
            this.outputFormula = false;
            this.erase = true;
            this.shortFilenames = false;
            this.printTypeInference = false;
        }
    }

    public class CommandLine
    {
        static int Main(string[] args)
        {
            string inputFileName = null;
            var options = new CommandLineOptions();
            if (args.Length == 0)
            {
                goto error;
            }

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
                        case "/dumpFormulaModel":
                            if (colonArg != null)
                                goto error;
                            options.outputFormula = true;
                            break;
                        case "/outputDir":
                            options.outputDir = colonArg;
                            break;
                        case "/doNotErase":
                            if (colonArg != null)
                                goto error;
                            options.erase = false;
                            break;
                        case "/shortFileNames":
                            if (colonArg != null)
                                goto error;
                            options.shortFilenames = true;
                            break;
                        case "/printTypeInference":
                            if (colonArg != null)
                                goto error;
                            options.printTypeInference = true;
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
                    if (inputFileName == null)
                    {
                        inputFileName = arg;
                    }
                    else
                    {
                        goto error;
                    }
                }
            }
            if (inputFileName.Length > 2 && inputFileName.EndsWith(".p"))
            {
                var comp = new Compiler(options);
                List<Flag> flags;
                var result = comp.Compile(inputFileName, out flags);
                WriteFlags(flags, options);

                if (!result)
                {
                    WriteMessageLine("Compilation failed", SeverityKind.Error);
                    return -1;
                }
                return 0;
            }
            else
            {
                Console.WriteLine("Illegal input file name");
            }
        error:
            {
                Console.WriteLine("USAGE: Pc.exe file.p [options]");
                Console.WriteLine("/outputDir:path");
                Console.WriteLine("/doNotErase");
                Console.WriteLine("/liveness[:mace]");
                Console.WriteLine("/shortFileNames");
                Console.WriteLine("/printTypeInference");
                Console.WriteLine("/dumpFormulaModel");
                return 0;
            }
        }

        public static void WriteFlags(List<Flag> flags, CommandLineOptions options)
        {
            if (options.shortFilenames)
            {
                var envParams = new EnvParams(
                    new Tuple<EnvParamKind, object>(EnvParamKind.Msgs_SuppressPaths, true));
                foreach (var f in flags)
                {
                    WriteMessageLine(
                        string.Format("{0} ({1}, {2}): {3}",
                        f.ProgramName == null ? "?" : f.ProgramName.ToString(envParams),
                        f.Span.StartLine,
                        f.Span.StartCol,
                        f.Message), f.Severity);
                }
            }
            else
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
        }

        public static void WriteMessageLine(string msg, SeverityKind severity)
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
