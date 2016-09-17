namespace Microsoft.Pc
{
    using System;
    using System.IO;
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
        public bool profile { get; set; }
        public LivenessOption liveness { get; set; }
        public string outputDir { get; set; }
        public string outputFileName { get; set; }
        public bool outputFormula { get; set; }
        public bool test { get; set; }
        public bool shortFileNames { get; set; }
        public bool printTypeInference { get; set; }
        public CompilerOutput compilerOutput { get; set; }
        public string inputFileName { get; set; }
        public string pipeName { get; set; } // set internally
        public bool generateSourceInfo { get; set; } // not supported currently

        public CommandLineOptions()
        {
        }

        public static bool ParseCompileString(IEnumerable<string> args, out bool sharedCompiler, out CommandLineOptions options)
        {
            sharedCompiler = false;
            options = new CommandLineOptions();
            bool profile = false;
            string fileName = null;
            bool test = false;
            bool outputFormula = false;
            bool printTypeInference = false;
            string outputDir = null;
            string outputFileName = null;
            bool shortFileNames = false;
            CompilerOutput compilerOutput = CompilerOutput.None;
            LivenessOption liveness = LivenessOption.None;

            foreach (string x in args)
            {
                string arg = x;
                string colonArg = null;
                if (arg[0] == '-' || arg[0] == '/')
                {
                    var colonIndex = arg.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        arg = x.Substring(0, colonIndex);
                        colonArg = x.Substring(colonIndex + 1);
                    }
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "profile":
                            profile = true;
                            break;

                        case "shortfilenames":
                            shortFileNames = true;
                            break;

                        case "shared":
                            sharedCompiler = true;
                            break;

                        case "test":
                            test = true;
                            break;

                        case "dumpformulamodel":
                            outputFormula = true;
                            break;

                        case "printtypeinference":
                            printTypeInference = true;
                            break;

                        case "generate":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Must supply type of output desired");
                                return false;
                            }
                            else if (colonArg == "C")
                            {
                                compilerOutput = CompilerOutput.C;
                            }
                            else if (colonArg == "Zing")
                            {
                                compilerOutput = CompilerOutput.Zing;
                            }
                            else if (colonArg == "C#")
                            {
                                compilerOutput = CompilerOutput.CSharp;
                            }
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
            options.profile = profile;
            options.inputFileName = fileName;
            options.outputFormula = outputFormula;
            options.printTypeInference = printTypeInference;
            options.outputDir = outputDir;
            options.outputFileName = outputFileName;
            options.test = test;
            options.liveness = liveness;
            options.shortFileNames = shortFileNames;
            options.compilerOutput = compilerOutput;
            return true;
        }

        public static bool ParseLinkString(IEnumerable<string> args)
        {
            if (args.Count() == 0)
            {
                Console.WriteLine("Must provide files to link");
                return false;
            }
            bool ok = true;
            foreach (string arg in args)
            {
                if (File.Exists(arg)) continue;
                Console.WriteLine("File {0} does not exist", arg);
                ok = false;
            }
            return ok;
        }
    }
}
