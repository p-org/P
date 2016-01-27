using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Pc
{
    public class CommandLine
    {
        private static int Main(string[] args)
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
                        case "/profile":
                            if (colonArg != null)
                                goto error;
                            options.profile = true;
                            break;

                        case "/dumpFormulaModel":
                            if (colonArg != null)
                                goto error;
                            options.outputFormula = true;
                            break;

                        case "/outputDir":
                            options.outputDir = colonArg;
                            if (!Directory.Exists(options.outputDir))
                            {
                                Console.WriteLine("Output directory {0} does not exist", options.outputDir);
                                goto error;
                            }
                            break;

                        case "/doNotErase":
                            if (colonArg != null)
                                goto error;
                            options.erase = false;
                            break;

                        case "/shortFileNames":
                            if (colonArg != null)
                                goto error;
                            options.shortFileNames = true;
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
                var compiler = new Compiler(options);
                var result = compiler.Compile(inputFileName);
                if (!result)
                {
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
                Console.WriteLine("/profile");
                return 0;
            }
        }
    }
}