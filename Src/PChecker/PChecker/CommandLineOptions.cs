using System;
using System.Collections.Generic;
using System.Text;
/*
namespace Plang.PChecker
{
    /// <summary>
    /// Result of parsing commandline options for PChecker
    /// </summary>
    public enum CommandLineParseResult
    {
        Success,
        Failure,
        HelpRequested
    }

    class CommandLineOptions
    {
        public static CommandLineParseResult ParseArguments(IEnumerable<string> args, out PCheckerJobConfiguration job)
        {
            
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
                                if (ParseProjectFile(colonArg, out job))
                                {
                                    return Success;
                                }
                                else
                                {
                                    return Failure;
                                }
                            }
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
            job = new PCheckerJobConfiguration();
            return CommandLineParseResult.Success;
        }
    }
}*/
