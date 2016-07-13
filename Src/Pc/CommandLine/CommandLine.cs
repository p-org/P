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
            bool sharedCompiler = false;
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
                        case "profile":
                            if (colonArg != null)
                                goto error;
                            options.profile = true;
                            break;

                        case "dumpformulamodel":
                            if (colonArg != null)
                                goto error;
                            options.outputFormula = true;
                            break;

                        case "outputdir":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Must supply path for output directory");
                                goto error;
                            }
                            if (!Directory.Exists(colonArg))
                            {
                                Console.WriteLine("Output directory {0} does not exist", colonArg);
                                goto error;
                            }
                            options.outputDir = colonArg;
                            break;

                        case "outputfilename":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Must supply name for output files");
                                goto error;
                            }
                            options.outputFileName = colonArg;
                            break;

                        case "test":
                            if (colonArg != null)
                                goto error;
                            options.test = true;
                            break;

                        case "shortfilenames":
                            if (colonArg != null)
                                goto error;
                            options.shortFileNames = true;
                            break;

                        case "printtypeinference":
                            if (colonArg != null)
                                goto error;
                            options.printTypeInference = true;
                            break;

                        case "liveness":
                            if (colonArg == null)
                                options.liveness = LivenessOption.Standard;
                            else if (colonArg == "mace")
                                options.liveness = LivenessOption.Mace;
                            else
                                goto error;
                            break;

                        case "noc":
                            if (colonArg != null)
                                goto error;
                            options.noCOutput = true;
                            break;

                        case "nosourceinfo":
                            if (colonArg != null)
                                goto error;
                            options.noSourceInfo = true;
                            break;

                        case "shared":
                            sharedCompiler = true;
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
            if (inputFileName != null && inputFileName.Length > 2 && inputFileName.EndsWith(".p"))
            {
                bool result = false;
                if (sharedCompiler)
                {
                    // compiler service requires full path.
                    inputFileName = Path.GetFullPath(inputFileName);
                    // use separate process that contains pre-compiled P compiler.
                    CompilerServiceClient svc = new CompilerServiceClient();
                    options.inputFileName = inputFileName;
                    if (string.IsNullOrEmpty(options.outputDir))
                    {
                        options.outputDir = Directory.GetCurrentDirectory();
                    }
                    result = svc.Compile(options);
                }
                else
                {
                    var compiler = new Compiler(options);
                    result = compiler.Compile(inputFileName);
                    return 0;
                }
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
                Console.WriteLine("/outputFileName:name");
                Console.WriteLine("/test");
                Console.WriteLine("/liveness[:mace]");
                Console.WriteLine("/shortFileNames");
                Console.WriteLine("/printTypeInference");
                Console.WriteLine("/dumpFormulaModel");
                Console.WriteLine("/profile");
                Console.WriteLine("/noC");
                Console.WriteLine("/noSourceInfo");
                Console.WriteLine("/shared");
                return 0;
            }
        }
    }
}