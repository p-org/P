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
            bool server = false;
            string loadErrorMsgString = "USAGE: load file.p [/test] [/printTypeInference] [/dumpFormulaModel] [/outputDir:<dir>] [/outputFileName:<name>]";
            string compileErrorMsgString = "USAGE: compile [/outputDir:<dir>] [/noSourceInfo]";
            string testErrorMsgString = "USAGE: test [/liveness[:mace]] [/outputDir:<dir>]";
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "shortfilenames":
                            shortFileNames = true;
                            break;
                        case "server":
                            server = true;
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
            DateTime currTime = DateTime.UtcNow;
            Compiler compiler;
            if (shortFileNames)
                compiler = new Compiler(true);
            else
                compiler = new Compiler(false);
            CommandLineOptions compilerOptions = new CommandLineOptions();
            compilerOptions.shortFileNames = shortFileNames;
            compilerOptions.test = false;
            compilerOptions.analyzeOnly = true;
            if (server)
            {
                Console.WriteLine("Pci: initialization succeeded");
            }
            while (true)
            {
                if (!server)
                {
                    Console.WriteLine("{0}s", DateTime.UtcNow.Subtract(currTime).TotalSeconds);
                    Console.Write(">> ");
                }
                var input = Console.ReadLine();
                currTime = DateTime.UtcNow;
                var inputArgs = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                        Console.WriteLine(loadErrorMsgString);
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
                        Console.WriteLine(loadErrorMsgString);
                        continue;
                    }
                    var success = ParseTestString(inputArgs, compilerOptions);
                    if (!success)
                    {
                        Console.WriteLine(testErrorMsgString);
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
                        Console.WriteLine(loadErrorMsgString);
                        continue;
                    }
                    var success = ParseCompileString(inputArgs, compilerOptions);
                    if (!success)
                    {
                        Console.WriteLine(compileErrorMsgString);
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
                Console.WriteLine("USAGE: Pci.exe [/shortFileNames] [/server]");
                return;
            }
        }

        private static bool ParseLoadString(string[] args, CommandLineOptions compilerOptions)
        {
            string fileName = null;
            bool test = false;
            bool outputFormula = false;
            bool printTypeInference = false;
            string outputDir = null;
            string outputFileName = null;
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                string colonArg = null;
                if (arg[0] == '-' || arg[0] == '/')
                {
                    var colonIndex = arg.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        arg = args[i].Substring(0, colonIndex);
                        colonArg = args[i].Substring(colonIndex + 1);
                    }
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "test":
                            test = true;
                            break;

                        case "dumpformulamodel":
                            outputFormula = true;
                            break;

                        case "printtypeinference":
                            printTypeInference = true;
                            break;

                        case "outputdir":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Must supply path for output directory");
                                return false;
                            }
                            if (!Directory.Exists(colonArg))
                            {
                                Console.WriteLine("Output directory {0} does not exist", colonArg);
                                return false;
                            }
                            outputDir = colonArg;
                            break;

                        case "outputfilename":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Must supply name for output files");
                                return false;
                            }
                            outputFileName = colonArg;
                            break;

                        default:
                            return false;
                    }
                }
                else
                {
                    if (fileName == null)
                    {
                        fileName = arg;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (fileName == null)
                return false;
            inputFileName = fileName;
            compilerOptions.outputFormula = outputFormula;
            compilerOptions.printTypeInference = printTypeInference;
            compilerOptions.outputDir = outputDir;
            compilerOptions.outputFileName = outputFileName;
            compilerOptions.test = test;
            return true;
        }

        private static bool ParseCompileString(string[] args, CommandLineOptions compilerOptions)
        {
            string outputDir = null;
            bool noSourceInfo = false;
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                string colonArg = null;
                if (arg[0] == '-' || arg[0] == '/')
                {
                    var colonIndex = arg.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        arg = args[i].Substring(0, colonIndex);
                        colonArg = args[i].Substring(colonIndex + 1);
                    }
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "outputdir":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Must supply path for output directory");
                                return false;
                            }
                            if (!Directory.Exists(colonArg))
                            {
                                Console.WriteLine("Output directory {0} does not exist", colonArg);
                                return false;
                            }
                            outputDir = colonArg;
                            break;

                        case "nosourceinfo":
                            noSourceInfo = true;
                            break;

                        default:
                            return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            compilerOptions.outputDir = outputDir;
            compilerOptions.noSourceInfo = noSourceInfo;
            return true;
        }

        private static bool ParseTestString(string[] args, CommandLineOptions compilerOptions)
        {
            LivenessOption liveness = LivenessOption.None;
            string outputDir = null;
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                string colonArg = null;
                if (arg[0] == '-' || arg[0] == '/')
                {
                    var colonIndex = arg.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        arg = args[i].Substring(0, colonIndex);
                        colonArg = args[i].Substring(colonIndex + 1);
                    }
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "outputdir":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Must supply path for output directory");
                                return false;
                            }
                            if (!Directory.Exists(colonArg))
                            {
                                Console.WriteLine("Output directory {0} does not exist", colonArg);
                                return false;
                            }
                            outputDir = colonArg;
                            break;

                        case "liveness":
                            if (colonArg == null)
                                liveness = LivenessOption.Standard;
                            else if (colonArg == "mace")
                                liveness = LivenessOption.Mace;
                            else
                                return false;
                            break;

                        default:
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