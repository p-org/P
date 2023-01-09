using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PChecker;
using PChecker.IO;
using PChecker.Utilities;
using Plang.Compiler;

namespace Plang
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
            this.Parser = new CommandLineArgumentParser("p compile",
                "The P compiler compiles all the P files in the project together and generates the executable that can be checked for correctness by the P checker");

            
            var projectGroup = this.Parser.GetOrCreateGroup("project", "P Project: Compiling using `.pproj` file");
            projectGroup.AddArgument("pproj", "pp", "P project file to compile (*.pproj). + " +
                                                    "If this option is not passed the compiler searches for a `*.pproj` in the current folder and compiles it");

            var pfilesGroup = this.Parser.GetOrCreateGroup("commandline", "Compiling P files through commandline");
            pfilesGroup.AddArgument("pfiles", "pf", "List of P files to compile").IsMultiValue = true;
            pfilesGroup.AddArgument("generate", "g", "Generate output :: (csharp, symbolic, java, c). (default: csharp)");
            pfilesGroup.AddArgument("target", "t", "Target or name of the compiled output");
            pfilesGroup.AddArgument("outdir", "o", "Dump output to directory (absolute or relative path");
        }

        /// <summary>
        /// Parses the command line options and returns a checkerConfiguration.
        /// </summary>
        /// <returns>The CheckerConfiguration object populated with the parsed command line options.</returns>
        internal CompilerConfiguration Parse(string[] args)
        {
            var compilationJob = new CompilerConfiguration();
            try
            {
                var result = this.Parser.ParseArguments(args);
                foreach (var arg in result)
                {
                    UpdateConfigurationWithParsedArgument(compilationJob, arg);
                }

                SanitizeConfiguration(compilationJob);
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
                    this.Parser.PrintHelp(Console.Out);
                    Error.ReportAndExit(ex.Message);
                }
            }
            catch (Exception ex)
            {
                this.Parser.PrintHelp(Console.Out);
                Error.ReportAndExit(ex.Message);
            }

            return compilationJob;
        }

        /// <summary>
        /// Updates the checkerConfiguration with the specified parsed argument.
        /// </summary>
        private static void UpdateConfigurationWithParsedArgument(CompilerConfiguration compilerConfiguration, CommandLineArgument option)
        {
            switch (option.LongName)
            {
                case "outdir":
                    break;
                
                default:
                    throw new Exception(string.Format("Unhandled parsed argument: '{0}'", option.LongName));
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
                if (CheckFileValidity.IsLegalPFile(pfile, out FileInfo fullPathName))
                {
                    Console.Out.WriteLine($"....... includes p file: {fullPathName.FullName}");
                }
                else
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