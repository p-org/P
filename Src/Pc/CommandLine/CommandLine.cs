namespace Microsoft.Pc
{
    public class CommandLine
    {
        private static int Main(string[] args)
        {
            CommandLineOptions options;
            if (!CommandLineOptions.ParseArguments(args, out options))
            {
                CommandLineOptions.PrintUsage();
                return -1;
            }

            ICompiler compiler = options.compilerService ? (ICompiler) new CompilerServiceClient() : new Compiler(options.shortFileNames);
            bool result = options.isLinkerPhase
                ? compiler.Link(new StandardOutput(), options)
                : compiler.Compile(new StandardOutput(), options);
            return result ? 0 : -1;
        }
    }
}