using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Pc
{
    public static class CommandLineOptions
    {
        private static readonly Lazy<bool> isFileSystemCaseInsensitive = new Lazy<bool>(() =>
        {
            var file = Path.GetTempPath() + Guid.NewGuid().ToString().ToLower() + "-lower";
            File.CreateText(file).Close();
            var isCaseInsensitive = File.Exists(file.ToUpper());
            File.Delete(file);
            return isCaseInsensitive;
        });

        private static bool IsFileSystemCaseInsensitive => isFileSystemCaseInsensitive.Value;

        public static bool ParseArguments(IEnumerable<string> args, out CompilationJob job)
        {
            job = null;

            var outputLanguage = CompilerOutput.C;
            DirectoryInfo outputDirectory = null;
            var commandLineFileNames = new List<string>();
            var inputFiles = new List<FileInfo>();
            string targetName = null;

            foreach (var x in args)
            {
                var arg = x;
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
                            switch (colonArg?.ToLowerInvariant())
                            {
                                case null:
                                    Console.WriteLine(
                                        "Missing generation argument, expecting generate:[C,P#]");
                                    return false;
                                case "c":
                                    outputLanguage = CompilerOutput.C;
                                    break;
                                case "p#":
                                    outputLanguage = CompilerOutput.PSharp;
                                    break;
                                default:
                                    Console.WriteLine($"Unrecognized generate option '{colonArg}', expecting C or P#");
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

                            outputDirectory = Directory.CreateDirectory(colonArg);
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
            foreach (var inputFileName in commandLineFileNames)
            {
                if (IsLegalPFile(inputFileName, out FileInfo fullPathName))
                {
                    inputFiles.Add(fullPathName);
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

            if (inputFiles.Count == 0)
            {
                Console.WriteLine("At least one .p file must be provided");
                return false;
            }

            var unitFileName = targetName ?? Path.GetFileNameWithoutExtension(inputFiles[0].FullName);
            if (!IsLegalUnitName(unitFileName))
            {
                Console.WriteLine("{0} is not a legal name for a compilation unit", unitFileName);
                return false;
            }

            if (outputDirectory == null)
            {
                outputDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            }

            job = new CompilationJob(new DefaultCompilerOutput(outputDirectory), outputLanguage, inputFiles,
                unitFileName);
            return true;
        }

        private static bool IsLegalUnitName(string unitFileName)
        {
            return Regex.IsMatch(unitFileName, "^[A-Za-z_][A-Za-z_0-9]*$");
        }

        private static bool IsLegalPFile(string fileName, out FileInfo file)
        {
            file = null;
            if (fileName.Length <= 2 || !fileName.EndsWith(".p"))
            {
                Console.WriteLine("Illegal file name: {0}", fileName);
                return false;
            }

            var path = Path.GetFullPath(fileName);
            if (IsFileSystemCaseInsensitive)
            {
                path = path.ToLowerInvariant();
            }

            file = new FileInfo(path);

            return true;
        }

        public static void PrintUsage()
        {
            Console.WriteLine("USAGE: Pc.exe file1.p [file2.p ...] [/t:tfile] [options]");
            Console.WriteLine(
                "/t:tfile             -- name of output file produced for this compilation unit; if not supplied then file1");
            Console.WriteLine("/outputDir:path         -- where to write the generated files");
            Console.WriteLine("/generate:[C,P#]");
            Console.WriteLine("    C   : generate C");
            Console.WriteLine("    P#  : generate P#");
        }
    }
}