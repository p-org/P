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
        private static void Main(string[] args)
        {
            bool shortFileNames = false;
            bool server = false;
            string compileErrorMsgString = "USAGE: compile file.p [/shortFileNames] [/printTypeInference] [/dumpFormulaModel] [/outputDir:<dir>] [/generate[:C,:Zing]] [/liveness[:mace]] [/profile]";
            string linkErrorMsgString = "USAGE: link file_1.4ml ... file_n.4ml [file.p]";
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
                            Console.WriteLine("Pci: unexpected command line argument: {0}", arg);
                            goto error;
                    }
                }
                else
                {
                    Console.WriteLine("Pci: unexpected command line argument: {0}", arg);
                    goto error;
                }
            }
            DateTime currTime = DateTime.UtcNow;
            Compiler compiler;
            if (shortFileNames)
                compiler = new Compiler(true);
            else
                compiler = new Compiler(false);
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
                    Console.WriteLine("Pci: exiting");
                    return;
                }
                else if (inputArgs[0] == "compile")
                {
                    try
                    {
                        CommandLineOptions compilerOptions;
                        var success = CommandLineOptions.ParseCompileString(inputArgs.Skip(1), out compilerOptions);
                        if (!success || compilerOptions.compilerService)
                        {
                            Console.WriteLine(compileErrorMsgString);
                            continue;
                        }
                        compilerOptions.shortFileNames = shortFileNames;
                        var result = compiler.Compile(new StandardOutput(), compilerOptions);
                        if (server)
                        {
                            if (!result)
                            {
                                Console.WriteLine("Pci: command failed");
                            }
                            else
                            {
                                Console.WriteLine("Pci: command done");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine("Pci: command failed");
                    }
                }
                else if (inputArgs[0] == "link")
                {
                    try
                    {
                        CommandLineOptions options;
                        if (!CommandLineOptions.ParseLinkString(inputArgs.Skip(1), out options))
                        {
                            Console.WriteLine(linkErrorMsgString);
                            continue;
                        }
                        var b = compiler.Link(new StandardOutput(), options);
                        if (server)
                        {
                            if (b)
                            {
                                Console.WriteLine("Pci: command done");
                            }
                            else
                            {
                                Console.WriteLine("Pci: command failed");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine("Pci: command failed");
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
    }
}