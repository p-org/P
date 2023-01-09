using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PChecker.IO;
using Plang.Compiler;
using Plang.Parser;

namespace Plang.Options
{
    internal sealed class PCompilerOptions
    {
        /// <summary>
        /// The command line parser to use.
        /// </summary>
        private readonly CommandLineArgumentParser Parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="PCompilerOptions"/> class.
        /// </summary>
        internal PCompilerOptions()
        {
            Parser = new CommandLineArgumentParser("p compile",
                "The P compiler compiles all the P files in the project together and generates the executable that can be checked for correctness by the P checker");

            
            var projectGroup = Parser.GetOrCreateGroup("project", "P Project: Compiling using `.pproj` file");
            projectGroup.AddArgument("pproj", "pp", "P project file to compile (*.pproj)." +
                                                    "If this option is not passed the compiler searches for a `*.pproj` in the current folder and compiles it");

            var pfilesGroup = Parser.GetOrCreateGroup("commandline", "Compiling P files through commandline");
            pfilesGroup.AddArgument("pfiles", "pf", "List of P files to compile").IsMultiValue = true;
            pfilesGroup.AddArgument("generate", "g",
                    "Generate output :: (csharp, symbolic, java, c). (default: csharp)").AllowedValues =
                new List<string>() { "csharp", "symbolic", "c", "java" };
            pfilesGroup.AddArgument("target", "t", "Target or name of the compiled output");
            pfilesGroup.AddArgument("outdir", "o", "Dump output to directory (absolute or relative path");
        }

        /// <summary>
        /// Parses the command line options and returns a checkerConfiguration.
        /// </summary>
        /// <returns>The CheckerConfiguration object populated with the parsed command line options.</returns>
        internal CompilerConfiguration Parse(string[] args)
        {
            var compilerConfiguration = new CompilerConfiguration();
            try
            {
                var result = Parser.ParseArguments(args);
                // if there are no arguments then search for a pproj file locally and load it
                FindLocalPProject(result);
                
                foreach (var arg in result)
                {
                    UpdateConfigurationWithParsedArgument(compilerConfiguration, arg);
                }

                SanitizeConfiguration(compilerConfiguration);
            }
            catch (CommandLineException ex)
            {
                if ((from arg in ex.Result where arg.LongName == "version" select arg).Any())
                {
                    WriteVersion();
                    Environment.Exit(1);
                }
                else
                {
                    Parser.PrintHelp(Console.Out);
                    Error.ReportAndExit(ex.Message);
                }
            }
            catch (Exception ex)
            {
                Parser.PrintHelp(Console.Out);
                Error.ReportAndExit(ex.Message);
            }

            return compilerConfiguration;
        }

        private static void FindLocalPProject(List<CommandLineArgument> result)
        {
            if (result.Count == 0)
            {
                CommandLineOutput.WriteInfo(".. Searching for a pproj file locally in the current folder");
                var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.pproj");
                if (files.Length == 0)
                {
                    CommandLineOutput.WriteInfo(
                        $".. Could not find any P project file in the current directory: {Directory.GetCurrentDirectory()}");
                }
                else
                {
                    var commandlineArg = new CommandLineArgument();
                    commandlineArg.Value = files.First();
                    commandlineArg.LongName = "pproj";
                    commandlineArg.ShortName = "pp";
                    CommandLineOutput.WriteInfo($"Compiling project: {commandlineArg.Value}");
                    result.Add(commandlineArg);
                }
            }
        }

        /// <summary>
        /// Updates the checkerConfiguration with the specified parsed argument.
        /// </summary>
        private static void UpdateConfigurationWithParsedArgument(CompilerConfiguration compilerConfiguration, CommandLineArgument option)
        {
            switch (option.LongName)
            {
                case "outdir":
                    compilerConfiguration.OutputDirectory = Directory.CreateDirectory((string)option.Value);
                    break;
                case "target":
                    compilerConfiguration.ProjectName = (string)option.Value;
                    break;
                case "generate":
                    {
                        compilerConfiguration.OutputLanguage = (string)option.Value switch
                        {
                            "csharp" => CompilerOutput.CSharp,
                            "c" => CompilerOutput.C,
                            "symbolic" => CompilerOutput.Symbolic,
                            "java" => CompilerOutput.Java,
                            _ => compilerConfiguration.OutputLanguage
                        };
                    }
                    break;
                case "pfiles":
                    {
                        var files = (string[])option.Value;
                        foreach (var file in files.Distinct())
                        {
                            compilerConfiguration.InputFiles.Add(file);
                        }
                    }
                    break;
                case "pproj":
                    {
                        new ParsePProjectFile().ParseProjectFile((string)option.Value, out var parsedConfig);
                        compilerConfiguration.Copy(parsedConfig);
                    }
                    break;
                default:
                    throw new Exception($"Unhandled parsed argument: '{option.LongName}'");
            }
        }

        private static void WriteVersion()
        {
            Console.WriteLine("Version: {0}", typeof(PCheckerOptions).Assembly.GetName().Version);
        }

        /// <summary>
        /// Checks the checkerConfiguration for errors and performs post-processing updates.
        /// </summary>
        private static void SanitizeConfiguration(CompilerConfiguration compilerConfiguration)
        {
            if (compilerConfiguration.InputFiles.Count == 0)
            {
                Error.ReportAndExit("Provide at least one input p file");
            }

            foreach (var pfile in compilerConfiguration.InputFiles)
            {
                if (!CheckFileValidity.IsLegalPFile(pfile, out var fullPathName))
                {
                    Error.ReportAndExit($"Illegal P file name {fullPathName.FullName} (file name cannot have special characters) or file not found.");
                }
            }
            
            if (!CheckFileValidity.IsLegalProjectName(compilerConfiguration.ProjectName))
            {
                Error.ReportAndExit($"{compilerConfiguration.ProjectName} is not a legal project name");
            }
        }
    }
}