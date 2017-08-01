using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Pc
{
    public class CommandLineOptions
    {
        // XMLSerializer is used to serialize an instance of this class to communicate 
        // between pc.exe and pcompilerservice.exe.  Use XmlIgnore attribute if you do not want 
        // a field to be communicated across.
        public bool profile { get; set; }

        public LivenessOption liveness { get; set; } = LivenessOption.None;
        public string outputDir { get; set; }
        public bool outputFormula { get; set; }
        public bool shortFileNames { get; set; }
        public bool isLinkerPhase { get; set; }
        public CompilerOutput compilerOutput { get; set; } = CompilerOutput.C;
        public List<string> inputFileNames { get; set; } = new List<string>();
        public List<string> dependencies { get; set; } = new List<string>();
        public string unitName { get; set; }

        /// <summary>
        ///     whether to use the compiler service.
        /// </summary>
        public bool compilerService { get; set; }

        /// <summary>
        ///     for internal use only.
        /// </summary>
        public string compilerId { get; set; }

        public static bool ParseArguments(IEnumerable<string> args, out CommandLineOptions options)
        {
            options = new CommandLineOptions();
            var commandLineFileNames = new List<string>();
            var dependencyFileNames = new List<string>();
            string targetName = null;
            foreach (string x in args)
            {
                string arg = x;
                string colonArg = null;
                if (arg[0] == '-' || arg[0] == '/')
                {
                    int colonIndex = arg.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        arg = x.Substring(0, colonIndex);
                        colonArg = x.Substring(colonIndex + 1);
                    }
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "profile":
                            options.profile = true;
                            break;

                        case "shortfilenames":
                            options.shortFileNames = true;
                            break;

                        case "shared":
                            options.compilerService = true;
                            break;

                        case "dumpformulamodel":
                            options.outputFormula = true;
                            break;

                        case "link":
                            options.isLinkerPhase = true;
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
                            switch (colonArg)
                            {
                                case null:
                                    Console.WriteLine("Missing generation argument, expecting generate:[C,C#,P#,P3,Zing]");
                                    return false;
                                case "C":
                                    options.compilerOutput = CompilerOutput.C;
                                    break;
                                case "Zing":
                                    options.compilerOutput = CompilerOutput.Zing;
                                    break;
                                case "C#":
                                    options.compilerOutput = CompilerOutput.CSharp;
                                    break;
                                case "P#":
                                    options.compilerOutput = CompilerOutput.PSharp;
                                    break;
                                case "P3":
                                    options.compilerOutput = CompilerOutput.PThree;
                                    break;
                                default:
                                    Console.WriteLine("Unrecognized generate option '{0}', expecting C, C#, P#, P3, or Zing", colonArg);
                                    return false;
                            }

                            break;

                        case "outputdir":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Must supply path for output directory");
                                return false;
                            }

                            options.outputDir = Path.GetFullPath(colonArg);
                            break;

                        case "liveness":
                            if (string.IsNullOrEmpty(colonArg))
                            {
                                options.liveness = LivenessOption.Standard;
                            }
                            else if (colonArg == "sampling")
                            {
                                options.liveness = LivenessOption.Sampling;
                            }
                            else
                            {
                                return false;
                            }

                            break;

                        default: return false;
                    }
                }
                else
                {
                    commandLineFileNames.Add(arg);
                }
            }

            if (options.compilerOutput == CompilerOutput.Zing && dependencyFileNames.Count > 0)
            {
                Console.WriteLine("Compilation to Zing does not support dependencies");
                return false;
            }

            var fileCheck = true;

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
            foreach (string inputFileName in commandLineFileNames)
            {
                string fullPathName;
                if (IsLegalPFile(inputFileName, out fullPathName))
                {
                    options.inputFileNames.Add(fullPathName);
                }
                else
                {
                    fileCheck = false;
                }
            }

            // Each dependency file name must be a legal .4ml file name
            foreach (string dependencyFileName in dependencyFileNames)
            {
                string fullPathName;
                if (IsLegal4mlFile(dependencyFileName, out fullPathName))
                {
                    options.dependencies.Add(fullPathName);
                }
                else
                {
                    fileCheck = false;
                }
            }

            // Check that all files exist
            foreach (string fileName in commandLineFileNames.Union(dependencyFileNames))
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

            if (options.isLinkerPhase)
            {
                if (options.inputFileNames.Count > 1 || options.dependencies.Count == 0)
                {
                    Console.WriteLine("Linking requires at most one .p file and at least one .4ml dependency file");
                    return false;
                }
            }
            else
            {
                if (options.inputFileNames.Count == 0)
                {
                    Console.WriteLine("At least one .p file must be provided");
                    return false;
                }

                options.unitName = targetName ?? Path.ChangeExtension(options.inputFileNames.First(), ".4ml");
                string unitFileName = Path.GetFileNameWithoutExtension(options.unitName);
                if (!IsLegalUnitName(unitFileName))
                {
                    Console.WriteLine("{0} is not a legal name for a compilation unit", unitFileName);
                    return false;
                }
            }

            if (options.outputDir == null)
            {
                options.outputDir = Directory.GetCurrentDirectory();
            }

            return true;
        }

        private static bool IsLegalUnitName(string unitFileName)
        {
            return Regex.IsMatch(unitFileName, "^[A-Za-z_][A-Za-z_0-9]*$");
        }

        private static bool IsLegalPFile(string fileName, out string fullPathName)
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

        private static bool IsLegal4mlFile(string fileName, out string fullPathName)
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

        public static void PrintUsage()
        {
            Console.WriteLine(" ------------ Compiler Phase ------------");
            Console.WriteLine("USAGE: Pc.exe file1.p [file2.p ...] [/t:tfile.4ml] [/r:rfile.4ml ...] [options]");
            Console.WriteLine("Compiles *.p programs and produces .4ml summary file which can then be passed to PLink.exe");
            Console.WriteLine(
                "/t:file.4ml             -- name of summary file produced for this compilation unit; if not supplied then file1.4ml");
            Console.WriteLine("/r:file.4ml             -- refer to another summary file");
            Console.WriteLine("/outputDir:path         -- where to write the generated files");
            Console.WriteLine("/shared                 -- use the compiler service");
            Console.WriteLine("/profile                -- print detailed timing information");
            Console.WriteLine("/generate:[C,C#,Zing]");
            Console.WriteLine("    C   : generate C without model functions");
            Console.WriteLine("    C#  : generate C# with model functions");
            Console.WriteLine("    Zing: generate Zing with model functions");
            Console.WriteLine("/liveness[:sampling]    -- controls compilation for Zinger");
            Console.WriteLine("/shortFileNames         -- print only file names in error messages");
            Console.WriteLine("/dumpFormulaModel       -- write the entire formula model to a file named 'output.4ml'");
            Console.WriteLine(" ------------ Linker Phase ------------");
            Console.WriteLine("USAGE: Pc.exe [linkfile.p] /link /r:file1.4ml [/r:file2.4ml ...] [options]");
            Console.WriteLine("Links *.4ml summary files against an optional linkfile.p and generates linker.{h,c,dll}");
            Console.WriteLine("/outputDir:path  -- where to write the generated files");
            Console.WriteLine("/shared          -- use the compiler service");
            Console.WriteLine("/profile         -- print detailed timing information");
            Console.WriteLine("Profiling can also be enabled by setting the environment variable PCOMPILE_PROFILE=1");
        }
    }
}