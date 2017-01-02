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
        public string compilerId { get; set; } // for internal use only.
        //linker phase
        public bool isLinkerPhase { get; set; }
        public bool reBuild { get; set; }
        //get p file
        public List<string> PFiles {
            get
            {
                return inputFileNames.Where(f => f.EndsWith(".p")).ToList();
            }
        }

        public List<string> FormulaFiles
        {
            get
            {
                return inputFileNames.Where(f => f.EndsWith(".4ml")).ToList();
            }
        }

        public CommandLineOptions()
        {
            //default values
            profile = false;
            liveness = LivenessOption.None;
            outputDir = null;
            outputFormula = false;
            shortFileNames = false;
            printTypeInference = false;
            compilerOutput = CompilerOutput.None;
            inputFileNames = new List<string>();
            compilerService = false;
            isLinkerPhase = false;
            reBuild = false;
        }

        public bool ParseArguments(IEnumerable<string> args)
        {
            List<string> commandLineFileNames = new List<string>();
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
                            compilerService = true;
                            break;

                        case "dumpformulamodel":
                            outputFormula = true;
                            break;

                        case "printtypeinference":
                            printTypeInference = true;
                            break;
                        case "link":
                            isLinkerPhase = true;
                            break;
                        case "rebuild":
                            reBuild = true;
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
                            else if (colonArg == "sampling")
                                liveness = LivenessOption.Sampling;
                            else
                                return false;
                            break;

                        default:
                            return false;
                    }
                }
                else
                {
                    commandLineFileNames.Add(arg);
                }
            }

            if (commandLineFileNames.Count == 0)
            {
                Console.WriteLine("Must provide files to compile");
                return false;
            }
            //populate the input files
            foreach (var inputFileName in commandLineFileNames)
            {
                if (!(inputFileName != null && inputFileName.Length > 2 && (inputFileName.EndsWith(".p") || inputFileName.EndsWith(".4ml"))))
                {
                    Console.WriteLine("Illegal source file name: {0}", inputFileName);
                    return false;
                }
                if (!File.Exists(inputFileName))
                {
                    Console.WriteLine("File does not exist: {0}", inputFileName);
                    return false;
                }
                inputFileNames.Add(Path.GetFullPath(inputFileName));
            }

            //check if the files are correct with respect to the compiler phase
            if(!isLinkerPhase && FormulaFiles.Count() > 0)
            {
                Console.WriteLine(".4ml file not expected as input without /link option");
                return false;
            }
            else if(isLinkerPhase && !(PFiles.Count <= 1 && FormulaFiles.Count > 0))
            {
                Console.WriteLine("must provide atleast one .4ml file and atmost one .p file with /link option");
                return false;
            }
            return true;
        }
    }
}