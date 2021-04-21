using System;
using static Plang.Compiler.CommandLineParseResult;

namespace Plang.Compiler
{
    public static class CommandLine
    {
        public static int Main(string[] args)
        {
            switch (CommandLineOptions.ParseArguments(args, out CompilationJob job))
            {
                case Failure:
                    CommandLineOptions.PrintUsage();
                    return 1;

                case HelpRequested:
                    CommandLineOptions.PrintUsage();
                    return 0;

                default:
                    try
                    {
                        ICompiler compiler = new Compiler();
                        compiler.Compile(job);
                        return 0;
                    }
                    catch (TranslationException e)
                    {
                        job.Output.WriteError("Error:\n" + e.Message);
                        return 1;
                    }
                    catch (Exception ex)
                    {
                        job.Output.WriteError($"<Internal Error>:\n {ex.Message}\n<Please report to the P team (p-devs@amazon.com) or create an issue on GitHub, Thanks!>");
                        job.Output.WriteError($"{ex.StackTrace}\n");
                        return 1;
                    }
            }
        }
    }
}