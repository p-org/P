namespace Microsoft.Pc
{
    public class CommandLine
    {
        private static int Main(string[] args)
        {
            if (CommandLineOptions.ParseArguments(args, out CommandLineOptions options))
            {
                ICompiler compiler = options.compilerService
                    ? (ICompiler) new CompilerServiceClient()
                    : new Compiler(options.shortFileNames);
                bool result = options.isLinkerPhase
                    ? compiler.Link(new StandardOutput(), options)
                    : compiler.Compile(new StandardOutput(), options);
                return result ? 0 : -1;
            }

            CommandLineOptions.PrintUsage();
            return -1;
        }
    }
}