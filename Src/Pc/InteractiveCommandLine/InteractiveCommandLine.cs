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
            bool shortFileNames = false;
            bool doNotErase = false;
            bool server = false;
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg == "/shortFileNames")
                {
                    shortFileNames = true;
                }
                else if (arg == "/doNotErase")
                {
                    doNotErase = true;
                }
                else if (arg == "/server")
                {
                    server = true;
                }
                else
                {
                    goto error;
                }
            }
            Compiler compiler;
            if (shortFileNames)
                compiler = new Compiler(true);
            else 
                compiler = new Compiler(false);
            List<Flag> flags;
            CommandLineOptions compilerOptions = new CommandLineOptions();
            compilerOptions.shortFileNames = shortFileNames;
            compilerOptions.erase = !doNotErase;
            compilerOptions.analyzeOnly = true;
            if (server)
                Console.WriteLine("Pci: Command done");
            else
                Console.Write(">> ");
            while (true)
            {
                var input = Console.ReadLine();
                var inputArgs = input.Split(' ');
                if (inputArgs.Length == 0) continue;
                if (inputArgs[0] == "exit")
                {
                    return;
                }
                else if (inputArgs[0] == "load")
                {
                    var success = ParseLoadString(inputArgs, compilerOptions);
                    if (!success) continue;
                    compiler.Options = compilerOptions;
                    var result = compiler.Compile(inputFileName, out flags);
                    WriteFlags(flags, shortFileNames);
                    if (!result)
                    {
                        inputFileName = null;
                        Console.WriteLine("Compilation failed");
                    }
                }
                else if (inputArgs[0] == "test")
                {
                    var success = ParseTestString(inputArgs, compilerOptions);
                    if (!success) continue;
                    compiler.Options = compilerOptions;
                    var b = compiler.GenerateZing(new List<Flag>());
                    Debug.Assert(b);
                }
                else if (inputArgs[0] == "compile")
                {
                    var success = ParseCompileString(inputArgs, compilerOptions);
                    if (!success) continue;
                    compiler.Options = compilerOptions;
                    var b = compiler.GenerateC(new List<Flag>());
                    Debug.Assert(b);
                }
                else
                {
                    Console.WriteLine("Unexpected input");
                }
                if (server)
                {
                    if (inputFileName == null)
                        Console.WriteLine("Pci: Load failed");
                    else
                        Console.WriteLine("Pci: Command done");
                }
                else
                {
                    Console.Write(">> ");
                }
            }

            error:
            {
                Console.WriteLine("USAGE: Pci.exe [/shortFileNames] [/doNotErase] [/server]");
                return;
            }

        }

        static bool ParseLoadString(string[] args, CommandLineOptions compilerOptions)
        {
            string fileName = null;
            bool outputFormula = false;
            bool printTypeInference = false;
            string outputDir = null;
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "/dumpFormulaModel")
                {
                    outputFormula = true;
                }
                else if (arg == "/printTypeInference")
                {
                    printTypeInference = true;
                }
                else if (outputDir == null && arg.StartsWith("/outputDir:"))
                {
                    var colonIndex = arg.IndexOf(':');
                    outputDir = arg.Substring(colonIndex + 1);
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
            if (fileName == null) goto error;
            inputFileName = fileName;
            compilerOptions.outputFormula = outputFormula;
            compilerOptions.printTypeInference = printTypeInference;
            compilerOptions.outputDir = outputDir;
            return true;

        error:
            {
                Console.WriteLine("USAGE: load file.p [/printTypeInference] [/dumpFormulaModel] [/outputDir:<dir>]");
                return false;
            }
        }

        static bool ParseCompileString(string[] args, CommandLineOptions compilerOptions)
        {
            string outputDir = null;
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                if (outputDir == null && arg.StartsWith("/outputDir:"))
                {
                    var colonIndex = arg.IndexOf(':');
                    outputDir = arg.Substring(colonIndex + 1);
                }
                else
                {
                    goto error;
                }
            }
            compilerOptions.outputDir = outputDir;
            return true;

        error:
            {
                Console.WriteLine("USAGE: compile [/outputDir:<dir>]");
                return false;
            }
        }

        static bool ParseTestString(string[] args, CommandLineOptions compilerOptions)
        {
            LivenessOption liveness = LivenessOption.None;
            string outputDir = null;
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                if (liveness == LivenessOption.None && arg.StartsWith("/liveness"))
                {
                    if (arg == "/liveness")
                    {
                        liveness = LivenessOption.Standard;
                    }
                    else if (arg.StartsWith("/liveness:"))
                    {
                        var colonIndex = arg.IndexOf(':');
                        var colonArg = arg.Substring(colonIndex + 1);
                        if (colonArg == "mace")
                            liveness = LivenessOption.Mace;
                        else
                            goto error;
                    }
                    else
                    {
                        goto error;
                    }
                }
                else if (outputDir == null && arg.StartsWith("/outputDir:"))
                {
                    var colonIndex = arg.IndexOf(':');
                    outputDir = arg.Substring(colonIndex + 1);
                }
                else
                {
                    goto error;
                }
            }
            compilerOptions.liveness = liveness;
            compilerOptions.outputDir = outputDir;
            return true;

        error:
            {
                Console.WriteLine("USAGE: test [/liveness[:mace]] [/outputDir:<dir>]");
                return false;
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
