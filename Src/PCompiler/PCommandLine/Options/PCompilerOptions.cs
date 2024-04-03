using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PChecker.IO.Debugging;
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
                "The P compiler compiles all the P files in the project together and generates the executable that can be checked for correctness by the P checker."
                // + "\n\nCompiler modes :: (default: bugfinding)\n" +
                // "  --mode bugfinding   : for bug finding through stratified random search\n" +
                // "  --mode verification : for verification through exhaustive symbolic exploration\n" +
                // "  --mode coverage     : for achieving state-space coverage through exhaustive explicit-state search\n" +
                // "  --mode pobserve     : for runtime monitoring of P specs against implementation logs"
            );

            var projectGroup = Parser.GetOrCreateGroup("project", "Compiling using `.pproj` file");
            projectGroup.AddArgument("pproj", "pp", "P project file to compile (*.pproj)." +
                                                    " If this option is not passed, the compiler searches for a *.pproj in the current folder");

            var pfilesGroup = Parser.GetOrCreateGroup("commandline", "Compiling P files directly through commandline");
            pfilesGroup.AddArgument("pfiles", "pf", "List of P files to compile").IsMultiValue = true;
            pfilesGroup.AddArgument("projname", "pn", "Project name for the compiled output");
            pfilesGroup.AddArgument("outdir", "o", "Dump output to directory (absolute or relative path)");

            var modes = Parser.AddArgument("mode", "md", "Compilation mode :: (bugfinding, verification, coverage, pobserve, stately). (default: bugfinding)");
            modes.AllowedValues = new List<string>() { "bugfinding", "verification", "coverage", "pobserve", "stately" };
            modes.IsHidden = true;

            Parser.AddArgument("pobserve-package", "po", "PObserve package name").IsHidden = true;

            Parser.AddArgument("debug", "d", "Enable debug logs", typeof(bool)).IsHidden = true;
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
                // if there are no --pproj or --pfiles arguments, then search for a pproj file locally and load it
                FindLocalPProject(result);

                // load pproj file first
                UpdateConfigurationWithPProjectFile(compilerConfiguration, result);

                // load parsed arguments that can override pproj configuration
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
            foreach (var arg in result)
            {
                if (arg.LongName.Equals("pproj") || arg.LongName.Equals("pfiles"))
                    return;
            }

            CommandLineOutput.WriteInfo(".. Searching for a P project file *.pproj locally in the current folder");
            var filtered =
                from file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.pproj")
                let info = new FileInfo(file)
                where (((info.Attributes & FileAttributes.Hidden) ==0)& ((info.Attributes & FileAttributes.System)==0))
                select file;
            var files = filtered.ToArray();
            if (files.Length == 0)
            {
                CommandLineOutput.WriteInfo(
                    $".. No P project file *.pproj found in the current folder: {Directory.GetCurrentDirectory()}");
            }
            else
            {
                var commandlineArg = new CommandLineArgument();
                commandlineArg.Value = files.First();
                commandlineArg.LongName = "pproj";
                commandlineArg.ShortName = "pp";
                CommandLineOutput.WriteInfo($".. Found P project file: {commandlineArg.Value}");
                result.Add(commandlineArg);
            }
        }

        /// <summary>
        /// Updates the checkerConfiguration with the specified P project file.
        /// </summary>
        private static void UpdateConfigurationWithPProjectFile(CompilerConfiguration compilerConfiguration, List<CommandLineArgument> result)
        {
            foreach (var option in result)
            {
                switch (option.LongName)
                {
                    case "pproj":
                    {
                        new ParsePProjectFile().ParseProjectFileForCompiler((string)option.Value, out var parsedConfig);
                        compilerConfiguration.Copy(parsedConfig);
                    }
                        return;
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
                    compilerConfiguration.Output = new DefaultCompilerOutput(compilerConfiguration.OutputDirectory);
                    break;
                case "projname":
                    compilerConfiguration.ProjectName = (string)option.Value;
                    break;
                case "debug":
                    compilerConfiguration.Debug = true;
                    break;
                case "mode":
                    compilerConfiguration.OutputLanguages = new List<CompilerOutput>();
                    switch (((string)option.Value).ToLowerInvariant())
                    {
                        case "bugfinding":
                        case "csharp":
                            compilerConfiguration.OutputLanguages.Add(CompilerOutput.CSharp);
                            break;
                        case "verification":
                        case "coverage":
                        case "symbolic":
                        case "psym":
                        case "pcover":
                            compilerConfiguration.OutputLanguages.Add(CompilerOutput.Symbolic);
                            break;
                        case "pobserve":
                        case "java":
                            compilerConfiguration.OutputLanguages.Add(CompilerOutput.Java);
                            break;
                        case "stately":
                            compilerConfiguration.OutputLanguages.Add(CompilerOutput.Stately);
                            break;
                        default:
                            throw new Exception($"Unexpected mode: '{option.Value}'");
                    }
                    break;
                case "pobserve-package":
                    compilerConfiguration.PObservePackageName = (string)option.Value;
                    break;
                case "pfiles":
                {
                    var files = (string[])option.Value;
                    foreach (var file in files.Distinct())
                    {
                        if (file.EndsWith(".p"))
                        {
                            compilerConfiguration.InputPFiles.Add(file);
                        }
                        else
                        {
                            compilerConfiguration.InputForeignFiles.Add(file);
                        }
                    }
                }
                    break;
                case "pproj":
                {
                    // do nothing, since already configured through UpdateConfigurationWithPProjectFile
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
            FindLocalPFiles(compilerConfiguration);
            if (compilerConfiguration.InputPFiles.Count == 0)
            {
                Error.CompilerReportAndExit("Could not find any *.p file.");
            }

            foreach (var pfile in compilerConfiguration.InputPFiles)
            {
                if (!CheckFileValidity.IsLegalPFile(pfile, out var fullPathName))
                {
                    Error.CompilerReportAndExit($"Illegal P file name {fullPathName.FullName} (file name cannot have special characters) or file not found.");
                }
            }

            if (!CheckFileValidity.IsLegalProjectName(compilerConfiguration.ProjectName))
            {
                Error.CompilerReportAndExit($"{compilerConfiguration.ProjectName} is not a legal project name");
            }

            if (compilerConfiguration.OutputDirectory == null)
            {
                compilerConfiguration.OutputDirectory = Directory.CreateDirectory("PGenerated");
                compilerConfiguration.Output = new DefaultCompilerOutput(compilerConfiguration.OutputDirectory);
            }
        }


        private static void FindLocalPFiles(CompilerConfiguration compilerConfiguration)
        {
            if (compilerConfiguration.InputPFiles.Count == 0)
            {
                CommandLineOutput.WriteInfo(".. Searching for P files locally in the current folder");

                var enumerationOptions = new EnumerationOptions();
                enumerationOptions.RecurseSubdirectories = true;
                enumerationOptions.MaxRecursionDepth = 1;

                var filtered =
                    from file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.p", enumerationOptions)
                    let info = new FileInfo(file)
                    where (((info.Attributes & FileAttributes.Hidden) ==0)& ((info.Attributes & FileAttributes.System)==0))
                    select file;
                foreach (var fileName in filtered)
                {
                    CommandLineOutput.WriteInfo($".. Adding P file: {fileName}");
                    compilerConfiguration.InputPFiles.Add(fileName);
                }
            }
        }

    }
}