using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using static Plang.Compiler.CommandLineParseResult;

namespace Plang.Compiler
{
    /// <summary>
    /// Result of parsing the command line arguments of P Compiler
    /// </summary>
    public enum CommandLineParseResult
    {
        Success,
        Failure,
        HelpRequested
    }

    public static class CommandLineOptions
    {
        /// <summary>
        /// Command line output: default is on console.
        /// </summary>
        private static readonly DefaultCompilerOutput CommandlineOutput =
            new DefaultCompilerOutput(new DirectoryInfo(Directory.GetCurrentDirectory()));

        /// <summary>
        /// Parse commandline arguments
        /// </summary>
        /// <param name="args">list of commandline inputs</param>
        /// <param name="job">P's compilation job</param>
        /// <returns></returns>
        public static CommandLineParseResult ParseArguments(IEnumerable<string> args, out CompilationJob job)
        {
            job = null;

            CompilerOutput outputLanguage = CompilerOutput.C;
            DirectoryInfo outputDirectory = null;

            List<string> commandLineFileNames = new List<string>();
            List<FileInfo> inputFiles = new List<FileInfo>();
            string targetName = null;
            bool generateSourceMaps = false;

            // enforce the argument prority
            // proj takes priority over everything else and no other arguments should be allowed
            if (args.Where(a => a.ToLowerInvariant().Contains("-proj:")).Any() && args.Count() > 1)
            {
                CommandlineOutput.WriteMessage("-proj cannot be combined with other commandline options", SeverityKind.Error);
                return Failure;
            }

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
                        case "proj":
                        if (colonArg == null)
                        {
                            CommandlineOutput.WriteMessage("Must supply project file for compilation",
                                SeverityKind.Error);
                            return Failure;
                        }
                        else
                        {
                            // Parse the project file and generate the compilation job, ignore all other arguments passed
                            var projectParser = new ParseCommandlineOptions(CommandlineOutput);
                            if (projectParser.ParseProjectFile(colonArg, out job))
                            {
                                return Success;
                            }
                            else
                            {
                                return Failure;
                            }
                        }
                        case "t":
                        case "target":
                            if (colonArg == null)
                            {
                                CommandlineOutput.WriteMessage("Missing target name", SeverityKind.Error);
                            }
                            else if (targetName == null)
                            {
                                targetName = colonArg;
                            }
                            else
                            {
                                CommandlineOutput.WriteMessage("Only one target must be specified", SeverityKind.Error);
                            }

                            break;

                        case "g":
                        case "generate":
                            switch (colonArg?.ToLowerInvariant())
                            {
                                case null:
                                    CommandlineOutput.WriteMessage(
                                        "Missing generation argument, expecting generate:[C,CSharp]", SeverityKind.Error);
                                    return Failure;

                                case "c":
                                    outputLanguage = CompilerOutput.C;
                                    break;

                                case "csharp":
                                    outputLanguage = CompilerOutput.CSharp;
                                    break;

                                default:
                                    CommandlineOutput.WriteMessage(
                                        $"Unrecognized generate option '{colonArg}', expecting C or CSharp",
                                        SeverityKind.Error);
                                    return Failure;
                            }

                            break;

                        case "o":
                        case "outputdir":
                            if (colonArg == null)
                            {
                                CommandlineOutput.WriteMessage("Must supply path for output directory",
                                    SeverityKind.Error);
                                return Failure;
                            }

                            outputDirectory = Directory.CreateDirectory(colonArg);
                            break;

                        
                        case "s":
                        case "sourcemaps":
                            switch (colonArg?.ToLowerInvariant())
                            {
                                case null:
                                case "true":
                                    generateSourceMaps = true;
                                    break;

                                case "false":
                                    generateSourceMaps = false;
                                    break;

                                default:
                                    CommandlineOutput.WriteMessage(
                                        "sourcemaps argument must be either 'true' or 'false'", SeverityKind.Error);
                                    return Failure;
                            }

                            break;

                        case "h":
                        case "help":
                        case "-help":
                            return HelpRequested;

                        default:
                            commandLineFileNames.Add(arg);
                            CommandlineOutput.WriteMessage($"Unknown Command {arg.Substring(1)}", SeverityKind.Error);
                            return Failure;
                    }
                }
                else
                {
                    commandLineFileNames.Add(arg);
                }
            }

            // We are here so no project file supplied lets create a compilation job with other arguments

            // Each command line file name must be a legal P file name
            foreach (string inputFileName in commandLineFileNames)
            {
                if (IsLegalPFile(inputFileName, out FileInfo fullPathName))
                {
                    inputFiles.Add(fullPathName);
                }
                else
                {
                    CommandlineOutput.WriteMessage(
                        $"Illegal P file name {inputFileName} or file {fullPathName.FullName} not found", SeverityKind.Error);
                }
            }

            if (inputFiles.Count == 0)
            {
                CommandlineOutput.WriteMessage("At least one .p file must be provided", SeverityKind.Error);
                return Failure;
            }

            string projectName = targetName ?? Path.GetFileNameWithoutExtension(inputFiles[0].FullName);
            if (!IsLegalUnitName(projectName))
            {
                CommandlineOutput.WriteMessage($"{projectName} is not a legal project name", SeverityKind.Error);
                return Failure;
            }

            if (outputDirectory == null)
            {
                outputDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            }

            job = new CompilationJob(output: new DefaultCompilerOutput(outputDirectory), outputLanguage: outputLanguage, inputFiles: inputFiles, projectName: projectName, generateSourceMaps: generateSourceMaps);
            return Success;
        }

        


        public static void PrintUsage()
        {
            CommandlineOutput.WriteMessage("USAGE: Pc.exe file1.p [file2.p ...] [-t:tfile] [options]", SeverityKind.Info);
            CommandlineOutput.WriteMessage("USAGE: Pc.exe -proj:<.pproj file>", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -t:[tfileName]             -- name of output file produced for this compilation unit; if not supplied then file1", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -outputDir:[path]          -- where to write the generated files", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -generate:[C,CSharp]       -- select a target language to generate", SeverityKind.Info);
            CommandlineOutput.WriteMessage("        C   : generate C code", SeverityKind.Info);
            CommandlineOutput.WriteMessage("        CSharp  : generate C# code ", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -proj:[.pprojfile]         -- the p project to be compiled", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -sourcemaps[:(true|false)] -- enable or disable generating source maps", SeverityKind.Info);
            CommandlineOutput.WriteMessage("                                  in the compiled C output. may confuse some", SeverityKind.Info);
            CommandlineOutput.WriteMessage("                                  debuggers.", SeverityKind.Info);
            CommandlineOutput.WriteMessage("    -h, -help, --help          -- display this help message", SeverityKind.Info);
        }
    }
}