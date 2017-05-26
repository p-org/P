namespace Microsoft.Pc
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;

    public class CommandLineOptions
    {
        // XMLSerializer is used to serialize an instance of this class to communicate 
        // between pc.exe and pcompilerservice.exe.  Use XmlIgnore attribute if you do not want 
        // a field to be communicated across.
        public bool profile { get; set; }
        public LivenessOption liveness { get; set; }
        public string outputDir { get; set; }
        public bool outputFormula { get; set; }
        public bool shortFileNames { get; set; }
        public CompilerOutput compilerOutput { get; set; }
        public List<string> inputFileNames { get; set; }
        public List<string> dependencies { get; set; }
        public string unitName { get; set; }
        public bool eraseModel { get; set; } // set internally
        public bool compilerService { get; set; } // whether to use the compiler service.
        public string compilerId { get; set; } // for internal use only.

        public CommandLineOptions()
        {
            //default values
            profile = false;
            liveness = LivenessOption.None;
            outputDir = null;
            outputFormula = false;
            shortFileNames = false;
            compilerOutput = CompilerOutput.C0;
            inputFileNames = new List<string>();
            dependencies = new List<string>();
            unitName = null;
            compilerService = false;
        }

        public bool ParseArguments(IEnumerable<string> args)
        {
            List<string> commandLineFileNames = new List<string>();
            List<string> dependencyFileNames = new List<string>();
            string targetName = null;
            bool isLinkerPhase = false;
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

                        case "link":
                            isLinkerPhase = true;
                            break;

                        case "r":
                        case "reference":
                            if (colonArg == null)   
                            {
                                Console.WriteLine("Missing reference, expecting a .4ml file");
                                return false;
                            }
                            else
                            {
                                dependencyFileNames.Add(colonArg);
                            }
                            break;

                        case "t":
                        case "target":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Missing target name, expecting a .4ml file");
                            }
                            else if (targetName == null)
                            {
                                targetName = colonArg;
                            }
                            else
                            {
                                Console.WriteLine("Only one target must be specified");
                            }
                            break;

                        case "generate":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Missing generation argument, expecting generate:[C0,C#,Zing]");
                                return false;
                            }
                            else if (colonArg == "C0")
                            {
                                compilerOutput = CompilerOutput.C0;
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
                                Console.WriteLine("Unrecognized generate option '{0}', expecing C0, C#, or Zing", colonArg);
                                return false;
                            }
                            break;

                        case "outputdir":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Must supply path for output directory");
                                return false;
                            }
                            outputDir = Path.GetFullPath(colonArg);
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

            if (compilerOutput == CompilerOutput.Zing && dependencyFileNames.Count > 0)
            {
                Console.WriteLine("Compilation to Zing does not support dependencies");
                return false;
            }

            bool fileCheck = true;

            // target name should be legal .4ml file name
            if (targetName != null)
            {
                string fullPathName;
                if (IsLegal4mlFile(targetName, out fullPathName))
                {
                    targetName = fullPathName;
                }
                else
                {
                    fileCheck = false;
                }
            }

            // Each command line file name must be a legal P file name
            foreach (var inputFileName in commandLineFileNames)
            {
                string fullPathName;
                if (IsLegalPFile(inputFileName, out fullPathName))
                {
                    inputFileNames.Add(fullPathName);
                }
                else
                {
                    fileCheck = false;
                }
            }

            // Each dependency file name must be a legal .4ml file name
            foreach (var dependencyFileName in dependencyFileNames)
            {
                string fullPathName;
                if (IsLegal4mlFile(dependencyFileName, out fullPathName))
                {
                    dependencies.Add(fullPathName);
                }
                else
                {
                    fileCheck = false;
                }
            }

            // Check that all files exist
            foreach (var fileName in commandLineFileNames.Union(dependencyFileNames))
            {
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("File does not exist: {0}", fileName);
                    fileCheck = false;
                }
            }

            if (!fileCheck)
            {
                return false;
            }

            if (isLinkerPhase)
            {
                if (inputFileNames.Count > 1 || dependencies.Count == 0)
                {
                    Console.WriteLine("Linking requires at most one .p file and at least one .4ml dependency file");
                    return false;
                }
                compilerOutput = CompilerOutput.Link;
            }
            else
            {
                if (inputFileNames.Count == 0)
                {
                    Console.WriteLine("At least one .p file must be provided");
                    return false;
                }
                if (targetName == null)
                {
                    unitName = Path.ChangeExtension(inputFileNames.First(), ".4ml");
                }
                else
                {
                    unitName = targetName;
                }
                var unitFileName = Path.GetFileNameWithoutExtension(unitName);
                if (!IsLegalUnitName(unitFileName))
                {
                    Console.WriteLine("{0} is not a legal name for a compilation unit", unitFileName);
                    return false;
                }
            }

            if (outputDir == null)
            {
                outputDir = Directory.GetCurrentDirectory();
            }

            return true;
        }

        private bool IsLegalUnitName(string unitFileName)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(unitFileName, "^[A-Za-z_][A-Za-z_0-9]*$");
        }

        private bool IsLegalPFile(string fileName, out string fullPathName)
        {
            fullPathName = null;
            if (fileName.Length <= 2 || !fileName.EndsWith(".p"))
            {
                Console.WriteLine("Illegal file name: {0}", fileName);
                return false;
            }
            fullPathName = Path.GetFullPath(fileName);
            if (Compiler.IsFileSystemCaseInsensitive)
            {
                fullPathName = fullPathName.ToLowerInvariant();
            }
            return true;
        }

        private bool IsLegal4mlFile(string fileName, out string fullPathName)
        {
            fullPathName = null;
            if (fileName.Length <= 4 || !fileName.EndsWith(".4ml"))
            {
                Console.WriteLine("Illegal file name: {0}", fileName);
                return false;
            }
            fullPathName = Path.GetFullPath(fileName);
            if (Compiler.IsFileSystemCaseInsensitive)
            {
                fullPathName = fullPathName.ToLowerInvariant();
            }
            return true;
        }
    }
}