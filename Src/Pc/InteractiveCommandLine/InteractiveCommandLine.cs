using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Pc
{
    internal class InteractiveCommandLine
    {
        private static string inputFileName = null;

        private static void Main(string[] args)
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
            var currTime = DateTime.UtcNow;
            Compiler compiler;
            if (shortFileNames)
                compiler = new Compiler(true);
            else
                compiler = new Compiler(false);
            CommandLineOptions compilerOptions = new CommandLineOptions();
            compilerOptions.shortFileNames = shortFileNames;
            compilerOptions.erase = !doNotErase;
            compilerOptions.analyzeOnly = true;
            if (server)
            {
                Console.WriteLine("Pci: initialization succeeded");
            }
            while (true)
            {
                if (!server)
                {
                    var nextTime = DateTime.UtcNow;
                    Console.WriteLine("{0}s", nextTime.Subtract(currTime).Seconds);
                    currTime = nextTime;
                    Console.Write(">> ");
                }
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
                    if (!success)
                    {
                        Console.WriteLine("USAGE: load file.p [/printTypeInference] [/dumpFormulaModel] [/outputDir:<dir>]");
                        continue;
                    }
                    compiler.Options = compilerOptions;
                    var result = compiler.Compile(inputFileName);
                    if (!result)
                    {
                        inputFileName = null;
                        if (server)
                        {
                            Console.WriteLine("Pci: load failed");
                        }
                    }
                    else
                    {
                        if (server)
                        {
                            Console.WriteLine("Pci: command done");
                        }
                    }
                }
                else if (inputArgs[0] == "test")
                {
                    if (inputFileName == null)
                    {
                        Console.WriteLine("USAGE: load file.p [/printTypeInference] [/dumpFormulaModel] [/outputDir:<dir>]");
                        continue;
                    }
                    var success = ParseTestString(inputArgs, compilerOptions);
                    if (!success)
                    {
                        Console.WriteLine("USAGE: test [/liveness[:mace]] [/outputDir:<dir>]");
                        continue;
                    }
                    compiler.Options = compilerOptions;
                    var b = compiler.GenerateZing();
                    Debug.Assert(b);
                    if (server)
                    {
                        Console.WriteLine("Pci: command done");
                    }
                }
                else if (inputArgs[0] == "compile")
                {
                    if (inputFileName == null)
                    {
                        Console.WriteLine("USAGE: load file.p [/printTypeInference] [/dumpFormulaModel] [/outputDir:<dir>]");
                        continue;
                    }
                    var success = ParseCompileString(inputArgs, compilerOptions);
                    if (!success)
                    {
                        Console.WriteLine("USAGE: compile [/outputDir:<dir>]");
                        continue;
                    }
                    compiler.Options = compilerOptions;
                    var b = compiler.GenerateC();
                    Debug.Assert(b);
                    if (server)
                    {
                        Console.WriteLine("Pci: command done");
                    }
                }
                else
                {
                    Console.WriteLine("Unexpected input");
                }
            }

        error:
            {
                Console.WriteLine("USAGE: Pci.exe [/profile] [/shortFileNames] [/doNotErase] [/server]");
                return;
            }
        }

        private static bool ParseLoadString(string[] args, CommandLineOptions compilerOptions)
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
                    if (!Directory.Exists(outputDir))
                    {
                        Console.WriteLine("Output directory {0} does not exist", outputDir);
                        return false;
                    }
                }
                else if (fileName == null && arg.Length > 2 && arg.EndsWith(".p"))
                {
                    fileName = arg;
                }
                else
                {
                    return false;
                }
            }
            if (fileName == null)
                return false;

            inputFileName = fileName;
            compilerOptions.outputFormula = outputFormula;
            compilerOptions.printTypeInference = printTypeInference;
            compilerOptions.outputDir = outputDir;
            return true;
        }

        private static bool ParseCompileString(string[] args, CommandLineOptions compilerOptions)
        {
            string outputDir = null;
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                if (outputDir == null && arg.StartsWith("/outputDir:"))
                {
                    var colonIndex = arg.IndexOf(':');
                    outputDir = arg.Substring(colonIndex + 1);
                    if (!Directory.Exists(outputDir))
                    {
                        Console.WriteLine("Output directory {0} does not exist", outputDir);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            compilerOptions.outputDir = outputDir;
            return true;
        }

        private static bool ParseTestString(string[] args, CommandLineOptions compilerOptions)
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
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (outputDir == null && arg.StartsWith("/outputDir:"))
                {
                    var colonIndex = arg.IndexOf(':');
                    outputDir = arg.Substring(colonIndex + 1);
                    if (!Directory.Exists(outputDir))
                    {
                        Console.WriteLine("Output directory {0} does not exist", outputDir);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            compilerOptions.liveness = liveness;
            compilerOptions.outputDir = outputDir;
            return true;
        }
    }
}