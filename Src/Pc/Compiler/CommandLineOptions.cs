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
        public bool outputFormula { get; set; }
        public bool shortFileNames { get; set; }
        public bool printTypeInference { get; set; }
        public CompilerOutput compilerOutput { get; set; }
        public List<string> inputFileNames { get; set; }
        public bool eraseModel { get; set; } // set internally
        public bool generateSourceInfo { get; set; } // not supported currently
        public bool compilerService { get; set; } // whether to use the compiler service.

        public CommandLineOptions()
        {
        }

        public static bool ParseCompileString(IEnumerable<string> args, out CommandLineOptions options)
        {
            options = new CommandLineOptions();
            List<string> inputFileNames = new List<string>();
            bool profile = false;
            bool outputFormula = false;
            bool printTypeInference = false;
            string outputDir = null;
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
                            options.compilerService = true;
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
                                Console.WriteLine("Missing generation argument, expecting one of generate:C, C0, Zing, or C#");
                                return false;
                            }
                            else if (colonArg == "C0")
                            {
                                compilerOutput = CompilerOutput.C0;
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
                            else
                            {
                                Console.WriteLine("Unrecognized generate option '{0}', expecing C, C0, Zing, or C#", colonArg);
                                return false;
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

                        case "liveness":
                            if (string.IsNullOrEmpty(colonArg))
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
                    inputFileNames.Add(arg);
                }
            }

            if (inputFileNames.Count == 0)
            {
                Console.WriteLine("Must provide files to compile");
                return false;
            }
            List<string> fullInputFileNames = new List<string>();
            foreach (var inputFileName in inputFileNames)
            {
                if (!(inputFileName != null && inputFileName.Length > 2 && inputFileName.EndsWith(".p")))
                {
                    Console.WriteLine("Illegal source file name: {0}", inputFileName);
                    return false;
                }
                if (!File.Exists(inputFileName))
                {
                    Console.WriteLine("File does not exist: {0}", inputFileName);
                    return false;
                }
                fullInputFileNames.Add(Path.GetFullPath(inputFileName));
            }
            options.profile = profile;
            options.outputFormula = outputFormula;
            options.printTypeInference = printTypeInference;
            options.outputDir = outputDir;
            options.liveness = liveness;
            options.shortFileNames = shortFileNames;
            options.compilerOutput = compilerOutput;
            options.inputFileNames = fullInputFileNames;
            return true;
        }

        public static bool ParseLinkString(IEnumerable<string> args, out CommandLineOptions options)
        {
            options = new CommandLineOptions();
            options.compilerOutput = CompilerOutput.Link;
            List<string> inputFileNames = new List<string>();
            string outputDir = null;
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
                        case "shared":
                            options.compilerService = true;
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

                        default:
                            return false;
                    }
                }
                else
                {
                    inputFileNames.Add(arg);
                }
            }

            if (inputFileNames.Count == 0)
            {
                Console.WriteLine("Must provide files to link");
                return false;
            }
            List<string> fullInputFileNames = new List<string>();
            foreach (string inputFileName in inputFileNames)
            {
                if (!(inputFileName != null && inputFileName.Length > 4 && inputFileName.ToLowerInvariant().EndsWith(".4ml")))
                {
                    Console.WriteLine("Illegal source file name: {0}, expecting *.4ml file name", inputFileName);
                    return false;
                }
                if (!File.Exists(inputFileName))
                {
                    Console.WriteLine("File does not exist: {0}", inputFileName);
                    return false;
                }
                fullInputFileNames.Add(Path.GetFullPath(inputFileName));
            }
            options.inputFileNames = fullInputFileNames;
            options.outputDir = outputDir;
            return true;
        }
    }
}