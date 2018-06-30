using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Pc
{
    public class CommandLineOptions
    {
        public string outputDir { get; set; }
        public CompilerOutput compilerOutput { get; set; } = CompilerOutput.C;
        public List<string> inputFileNames { get; set; } = new List<string>();
        public string projectName { get; set; }

        public static bool ParseArguments(IEnumerable<string> args, out CommandLineOptions options)
        {
            var output = new StandardOutput();
            options = new CommandLineOptions();
            
            var commandLineFileNames = new List<string>();
            string targetName = null;
            foreach (string x in args)
            {
                string arg = x;
                string colonArg = null;
                if (arg[0] == '-')
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
                                output.WriteMessage("Missing target name", SeverityKind.Error);
                            }
                            else if (targetName == null)
                            {
                                targetName = colonArg;
                            }
                            else
                            {
                                output.WriteMessage("Only one target must be specified", SeverityKind.Error);
                            }

                            break;

                        case "g":
                        case "generate":
                            switch (colonArg)
                            {
                                case null:
                                    output.WriteMessage("Missing generation argument, expecting generate:[C,P#]", SeverityKind.Error);
                                    return false;
                                case "C":
                                    options.compilerOutput = CompilerOutput.C;
                                    break;
                                case "P#":
                                    options.compilerOutput = CompilerOutput.PSharp;
                                    break;
                                default:
                                    output.WriteMessage($"Unrecognized generate option '{colonArg}', expecting C or P#", SeverityKind.Error);
                                    return false;
                            }

                            break;

                        case "o":
                        case "outputdir":
                            if (colonArg == null)
                            {
                                output.WriteMessage("Must supply path for output directory", SeverityKind.Error);
                                return false;
                            }
                            //check if the path is valid
                            if (!Directory.Exists(Path.GetFullPath(colonArg)))
                            {
                                output.WriteMessage("Must supply path for output directory", SeverityKind.Error);
                                return false;
                            }
                            options.outputDir = Path.GetFullPath(colonArg);
                            break;

                        default:
                            commandLineFileNames.Add(arg);
                            output.WriteMessage($"Unknown Command {arg.Substring(1)}", SeverityKind.Error);
                            return false;
                    }
                }
                else
                {
                    commandLineFileNames.Add(arg);
                }
            }

            // Each command line file name must be a legal P file name
            foreach (string inputFileName in commandLineFileNames)
            {
                if (IsLegalPFile(inputFileName, out string fullPathName))
                {
                    options.inputFileNames.Add(fullPathName);
                }
                else
                {
                    output.WriteMessage($"Illegal P file name {fullPathName} or file {fullPathName} not found", SeverityKind.Error);
                }
            }

            if (options.inputFileNames.Count == 0)
            {
                output.WriteMessage("At least one .p file must be provided", SeverityKind.Error);
                return false;
            }

            string projectName = targetName ?? Path.GetFileNameWithoutExtension(options.inputFileNames[0]);
            if (!IsLegalUnitName(projectName))
            {
                output.WriteMessage($"{projectName} is not a legal protject name", SeverityKind.Error);
                return false;
            }
            options.projectName = projectName;

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
            if (fileName.Length <= 2 || !fileName.EndsWith(".p") || !File.Exists(Path.GetFullPath(fileName)))
            {
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
            var output = new StandardOutput();
            output.WriteMessage("USAGE: Pc.exe file1.p [file2.p ...] [-t:tfile] [options]", SeverityKind.Info);
            output.WriteMessage("-t:tfile           -- name of output file produced for this compilation unit; if not supplied then file1", SeverityKind.Info);
            output.WriteMessage("-outputDir:path    -- where to write the generated files", SeverityKind.Info);
            output.WriteMessage("-generate:[C,P#]", SeverityKind.Info);
            output.WriteMessage("    C   : generate C", SeverityKind.Info);
            output.WriteMessage("    P#  : generate P#", SeverityKind.Info);
        }
    }
}