using 
namespace Plang
{
    public class ParserCompilerOptions
    {
        /// <summary>
        /// Parse the commandline arguments to construct the compilation job
        /// </summary>
        /// <param name="args">Commandline arguments</param>
        /// <param name="job">Generated Compilation job</param>
        /// <returns></returns>
        internal bool ParseCommandLineOptions(IEnumerable<string> args, out CompilationJob job)
        {
            string targetName = null;
            CompilerOutput outputLanguage = CompilerOutput.CSharp;
            DirectoryInfo outputDirectory = null;
            DirectoryInfo aspectjOutputDirectory = null;
            HashSet<string> inputFiles = new HashSet<string>();
            commandlineOutput.WriteInfo($"----------------------------------------");
            job = null;
            try
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
                                    throw new CommandlineParsingError(
                                        "Missing target project name (-t:<project name>)");
                                }
                                else if (targetName == null)
                                {
                                    targetName = colonArg;
                                }
                                else
                                {
                                    throw new CommandlineParsingError("Only one target must be specified with (-t)");
                                }

                                break;

                            case "g":
                            case "generate":
                                switch (colonArg?.ToLowerInvariant())
                                {
                                    case null:
                                        throw new CommandlineParsingError(
                                            "Missing generation argument, expecting generate:[C,CSharp,Java,RVM,Symbolic]");
                                    case "c":
                                        outputLanguage = CompilerOutput.C;
                                        break;
                                    case "csharp":
                                        outputLanguage = CompilerOutput.CSharp;
                                        break;
                                    case "java":
                                        outputLanguage = CompilerOutput.Java;
                                        break;
                                    case "rvm":
                                        outputLanguage = CompilerOutput.Rvm;
                                        break;
                                    case "symbolic":
                                        outputLanguage = CompilerOutput.Symbolic;
                                        break;
                                    default:
                                        throw new CommandlineParsingError(
                                            $"Unrecognized generate option '{colonArg}', expecting one of C, CSharp, Java, RVM, Symbolic.");
                                }

                                break;

                            case "o":
                            case "outputdir":
                                if (colonArg == null)
                                {
                                    throw new CommandlineParsingError(
                                        "Must supply path for output directory (-o:<output directory>)");
                                }

                                outputDirectory = Directory.CreateDirectory(colonArg);
                                break;

                            case "a":
                            case "aspectoutputdir":
                                if (colonArg == null)
                                {
                                    throw new CommandlineParsingError(
                                        "Must supply path for aspectj output directory (-a:<aspectj output directory>)");
                                }

                                aspectjOutputDirectory = Directory.CreateDirectory(colonArg);
                                break;

                            default:
                                CommandLineOptions.PrintUsage();
                                throw new CommandlineParsingError($"Illegal Command {arg.Substring(1)}");
                        }
                    }
                    else
                    {
                        if (IsLegalPFile(arg, out FileInfo fullPathName))
                        {
                            inputFiles.Add(fullPathName.FullName);
                            commandlineOutput.WriteInfo($"....... includes p file: {fullPathName.FullName}");
                        }
                        else
                        {
                            throw new CommandlineParsingError(
                                $"Illegal P file name {arg} (file name cannot have special characters) or file not found.");
                        }
                    }
                }

                if (inputFiles.Count == 0)
                {
                    commandlineOutput.WriteError("At least one .p file must be provided");
                    return false;
                }

                string projectName = targetName ?? Path.GetFileNameWithoutExtension(inputFiles.FirstOrDefault());
                if (!IsLegalProjectName(projectName))
                {
                    commandlineOutput.WriteError($"{projectName} is not a legal project name");
                    return false;
                }

                if (outputDirectory == null)
                {
                    outputDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
                }

                if (aspectjOutputDirectory == null)
                {
                    aspectjOutputDirectory = outputDirectory;
                }

                job = new CompilationJob(output: new DefaultCompilerOutput(outputDirectory, aspectjOutputDirectory),
                    outputDirectory,
                    outputLanguage: outputLanguage, inputFiles: inputFiles.ToList(), projectName: projectName,
                    projectRoot: outputDirectory,
                    aspectjOutputDir: aspectjOutputDirectory);
                commandlineOutput.WriteInfo($"----------------------------------------");
                return true;
            }
            catch (CommandlineParsingError ex)
            {
                commandlineOutput.WriteError($"<Error parsing commandline>:\n {ex.Message}");
                return false;
            }
            catch (Exception other)
            {
                commandlineOutput.WriteError(
                    $"<Internal Error>:\n {other.Message}\n <Please report to the P team (p-devs@amazon.com) or create an issue on GitHub, Thanks!>");
                commandlineOutput.WriteError($"{other.StackTrace}\n");
                return false;
            }
        }
    }
}