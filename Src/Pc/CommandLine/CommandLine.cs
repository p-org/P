using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc
{
    public static class CommandLine
    {
        public static int Main(string[] args)
        {
            ICompiler compiler = new Compiler();

            if (!CommandLineOptions.ParseArguments(args, out CompilationJob job))
            {
                CommandLineOptions.PrintUsage();
                return -1;
            }
            
            try
            {
                compiler.Compile(job);
                return 0;
            }
            catch (TranslationException e)
            {
                job.Output.WriteMessage(e.Message, SeverityKind.Error);
                return 1;
            }
        }
    }
}
