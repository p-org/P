using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Pc
{
    public class CommandLineOptions
    {
        public DirectoryInfo OutputDirectory { get; set; }
        public CompilerOutput OutputLanguage { get; set; } = CompilerOutput.C;
        public List<string> InputFileNames { get; set; } = new List<string>();
        public string ProjectName { get; set; }

        public static bool ParseArguments(IEnumerable<string> args, out CommandLineOptions options)
        {
            options = new CommandLineOptions();

            var commandLineFileNames = new List<string>();
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
                        case "t":
                        case "target":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Missing target name");
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

                        case "g":
                        case "generate":
                            switch (colonArg)
                            {
                                case null:
                                    Console.WriteLine(
                                        "Missing generation argument, expecting generate:[C,P#]");
                                    return false;
                                case "C":
                                    options.OutputLanguage = CompilerOutput.C;
                                    break;
                                case "P#":
                                    options.OutputLanguage = CompilerOutput.PSharp;
                                    break;
                                default:
                                    Console.WriteLine(
                                        "Unrecognized generate option '{0}', expecting C or P#",
                                        colonArg);
                                    return false;
                            }

                            break;

                        case "o":
                        case "outputdir":
                            if (colonArg == null)
                            {
                                Console.WriteLine("Must supply path for output directory");
                                return false;
                            }

                            options.OutputDirectory = Directory.CreateDirectory(colonArg);
                            break;

                        default: return false;
                    }
                }
                else
                {
                    commandLineFileNames.Add(arg);
                }
            }

            var fileCheck = true;

            // Each command line file name must be a legal P file name
            foreach (string inputFileName in commandLineFileNames)
            {
                if (IsLegalPFile(inputFileName, out string fullPathName))
                {
                    options.InputFileNames.Add(fullPathName);
                }
                else
                {
                    fileCheck = false;
                }
            }

            if (!fileCheck)
            {
                return false;
            }

            if (options.InputFileNames.Count == 0)
            {
                Console.WriteLine("At least one .p file must be provided");
                return false;
            }

            string unitFileName = targetName ?? Path.GetFileNameWithoutExtension(options.InputFileNames[0]);
            if (!IsLegalUnitName(unitFileName))
            {
                Console.WriteLine("{0} is not a legal name for a compilation unit", unitFileName);
                return false;
            }
            options.ProjectName = unitFileName;

            if (options.OutputDirectory == null)
            {
                options.OutputDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
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
            if (IsFileSystemCaseInsensitive)
            {
                fullPathName = fullPathName.ToLowerInvariant();
            }

            return true;
        }

        private static readonly Lazy<bool> isFileSystemCaseInsensitive = new Lazy<bool>(() =>
        {
            string file = Path.GetTempPath() + Guid.NewGuid().ToString().ToLower() + "-lower";
            File.CreateText(file).Close();
            bool isCaseInsensitive = File.Exists(file.ToUpper());
            File.Delete(file);
            return isCaseInsensitive;
        });

        private static bool IsFileSystemCaseInsensitive => isFileSystemCaseInsensitive.Value;

        public static void PrintUsage()
        {
            Console.WriteLine("USAGE: Pc.exe file1.p [file2.p ...] [/t:tfile] [options]");
            Console.WriteLine("/t:tfile             -- name of output file produced for this compilation unit; if not supplied then file1");
            Console.WriteLine("/outputDir:path         -- where to write the generated files");
            Console.WriteLine("/generate:[C,P#]");
            Console.WriteLine("    C   : generate C");
            Console.WriteLine("    P#  : generate P#");
        }
    }
}