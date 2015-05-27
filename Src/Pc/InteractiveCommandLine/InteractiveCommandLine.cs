using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Microsoft.Pc
{
    using Microsoft.Formula.API;

    class InteractiveCommandLine
    {
        static string inputFileName = null;

        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                goto error;
            }
            Compiler compiler;
            if (args.Length == 1 && args[0] == "/shortFileNames")
                compiler = new Compiler(true);
            else 
                compiler = new Compiler(false);
            List<Flag> flags;
            while (true)
            {
                Console.Write(">> ");
                var input = Console.ReadLine();
                var inputArgs = input.Split(' ');
                if (inputArgs.Length == 0) continue;
                if (inputArgs[0] == "exit")
                {
                    return;
                } 
                else if (inputArgs[0] == "load")
                {
                    var compilerOptions = ParseLoadString(inputArgs);
                    if (compilerOptions == null) continue;
                    compiler.Options = compilerOptions;
                    var result = compiler.Compile(inputFileName, out flags);
                    WriteFlags(flags, false);
                    if (!result)
                    {
                        inputFileName = null;
                    }
                }
                else if (inputArgs[0] == "test")
                {
                    var compilerOptions = ParseTestString(inputArgs);
                    if (compilerOptions == null) continue;
                    compiler.Options = compilerOptions;
                    var b = compiler.GenerateZing(new List<Flag>());
                    Debug.Assert(b);
                }
                else if (inputArgs[0] == "compile")
                {
                    var compilerOptions = ParseCompileString(inputArgs);
                    if (compilerOptions == null) continue;
                    compiler.Options = compilerOptions;
                    var b = compiler.GenerateC(new List<Flag>());
                    Debug.Assert(b);
                }
                else
                {
                    Console.WriteLine("Unexpected input");
                    continue;
                }
            }

            error:
            {
                Console.WriteLine("USAGE: Pci.exe [/shortFileNames]");
                return;
            }

        }

        static CommandLineOptions ParseLoadString(string[] args)
        {
            string fileName = null;
            var options = new CommandLineOptions();
            for (int i = 1; i < args.Length; i++)
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
                        case "/printTypeInference":
                            if (colonArg != null)
                                goto error;
                            options.printTypeInference = true;
                            break;
                        default:
                            goto error;
                    }
                }
                else if (fileName == null && arg.Length > 2 && arg.EndsWith(".p"))
                {
                    fileName = arg;
                }
                else
                {
                    goto error;
                }
            }

            options.analyzeOnly = true;
            inputFileName = fileName;
            return options;

        error:
            {
                Console.WriteLine("USAGE: load file.p [options]");
                Console.WriteLine("/outputDir:path");
                Console.WriteLine("/printTypeInference");
                Console.WriteLine("/dumpFormulaModel");
                return null;
            }
        }

        static CommandLineOptions ParseCompileString(string[] args)
        {
            var options = new CommandLineOptions();
            for (int i = 1; i < args.Length; i++)
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
                        default:
                            goto error;
                    }
                }
                else
                {
                        goto error;
                }
            }
            return options;

        error:
            {
                Console.WriteLine("USAGE: compile [options]");
                Console.WriteLine("/outputDir:path");
                Console.WriteLine("/doNotErase");
                return null;
            }
        }

        static CommandLineOptions ParseTestString(string[] args)
        {
            var options = new CommandLineOptions();
            for (int i = 1; i < args.Length; i++)
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
                    goto error;
                }
            }
            return options;

        error:
            {
                Console.WriteLine("USAGE: test [options]");
                Console.WriteLine("/outputDir:path");
                Console.WriteLine("/liveness[:mace]");
                return null;
            }
        }

        static void WriteFlags(List<Flag> flags, bool shortFileNames)
        {
            if (shortFileNames)
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

        static void WriteMessageLine(string msg, SeverityKind severity)
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
