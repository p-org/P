namespace Plang
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
    /*
    public static class PCheckerOptions
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
        public static CommandLineParseResult ParseArguments(IEnumerable<string> args, out CompilerConfiguration job)
        {
            job = null;

            var commandlineParser = new ParsePProjectFile(CommandlineOutput);
            // enforce the argument priority
            var commandlineArgs = args.ToList();
            if (commandlineArgs.Any(a => a.ToLowerInvariant().Contains("-h")))
            {
                return HelpRequested;
            }
            // generate takes priority over everything else
            CompilerOutput? generateLang = null;
            var generateArg = commandlineArgs.FirstOrDefault(a => a.ToLowerInvariant().Contains("-generate:"));
            if (generateArg != null)
            {
                commandlineArgs.Remove(generateArg);
                if (!commandlineParser.ParseCommandLineGenerateOption(generateArg, ref generateLang))
                {
                    return Failure;
                }
            }
            // proj takes priority over everything else and no other arguments should be allowed
            if (commandlineArgs.Any(a => a.ToLowerInvariant().Contains("-proj:")))
            {
                if (commandlineArgs.Count() > 1)
                {
                    CommandlineOutput.WriteMessage("-proj option cannot be combined with other commandline options (except -generate)", SeverityKind.Error);
                    return Failure;
                }
                else
                {
                    var option = commandlineArgs.First();
                    var projectPath = option.Substring(option.IndexOf(":", StringComparison.Ordinal) + 1);
                    // Parse the project file and generate the compilation job
                    return commandlineParser.ParseProjectFile(projectPath, generateLang, out job) ? Success : Failure;
                }
            }
            else
            {
                // parse command line options and generate the compilation job
                return commandlineParser.ParseCommandLineOptions(commandlineArgs, generateLang, out job) ? Success : Failure;
            }
        }

        /// <summary>
        /// Print the P compiler commandline usage information
        /// </summary>
        public static void PrintUsage()
        {
            CommandlineOutput.WriteInfo("------------------------------------------");
            CommandlineOutput.WriteInfo("Recommended usage:\n");
            CommandlineOutput.WriteInfo(">> pc -proj:<.pproj file>\n");
            CommandlineOutput.WriteInfo("------------------------------------------");
            CommandlineOutput.WriteInfo("Optional usage:\n");
            CommandlineOutput.WriteInfo(">> pc file1.p [file2.p ...] [-t:tfile] [options]");
            CommandlineOutput.WriteInfo("    -t:[target project name]     -- project name (as well as the generated file)");
            CommandlineOutput.WriteInfo("                                    if not supplied, use file1");
            CommandlineOutput.WriteInfo("    -outputDir:[path]            -- where to write the generated files");
            CommandlineOutput.WriteInfo("    -aspectOutputDir:[path]      -- where to write the generated aspectj files");
            CommandlineOutput.WriteInfo("                                    if not supplied, use outputDir");
            CommandlineOutput.WriteInfo("    -generate:[C,CSharp,RVM,...] -- select a target language to generate");
            CommandlineOutput.WriteInfo("        C       : generate C code");
            CommandlineOutput.WriteInfo("        CSharp  : generate C# code ");
            CommandlineOutput.WriteInfo("        Java    : generate Java code (WIP)");
            CommandlineOutput.WriteInfo("        RVM     : generate Monitor code");
            CommandlineOutput.WriteInfo("        PSym    : generate code for PSym");
            CommandlineOutput.WriteInfo("    -h, -help, --help            -- display this help message");
            CommandlineOutput.WriteInfo("------------------------------------------");
        }
    }*/
}
