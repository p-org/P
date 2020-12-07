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

            var commandlineParser = new ParseCommandlineOptions(CommandlineOutput);
            // enforce the argument prority
            if (args.Where(a => a.ToLowerInvariant().Contains("-h")).Any())
            {
                PrintUsage();
                return HelpRequested;
            }
            // proj takes priority over everything else and no other arguments should be allowed
            if (args.Where(a => a.ToLowerInvariant().Contains("-proj:")).Any())
            {
                if (args.Count() > 1)
                {
                    CommandlineOutput.WriteMessage("-proj option cannot be combined with other commandline options", SeverityKind.Error);
                    return Failure;
                }
                else
                {
                    var option = args.First();
                    var projectPath = option.Substring(option.IndexOf(":") + 1);
                    // Parse the project file and generate the compilation job
                    return commandlineParser.ParseProjectFile(projectPath, out job) ? Success : Failure;
                }
            }
            else
            {
                // parse command line options and generate the compilation job
                return commandlineParser.ParseCommandLineOptions(args, out job) ? Success : Failure;
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
            CommandlineOutput.WriteInfo("    -t:[target project name]   -- name of project (as well as the generated file); if not supplied then file1");
            CommandlineOutput.WriteInfo("    -outputDir:[path]          -- where to write the generated files");
            CommandlineOutput.WriteInfo("    -generate:[C,CSharp,RVM]       -- select a target language to generate");
            CommandlineOutput.WriteInfo("        C       : generate C code");
            CommandlineOutput.WriteInfo("        CSharp  : generate C# code ");
            CommandlineOutput.WriteInfo("        RVM     : generate Monitor code");
            CommandlineOutput.WriteInfo("    -h, -help, --help          -- display this help message");
            CommandlineOutput.WriteInfo("------------------------------------------");
        }
    }
}
