namespace Microsoft.Pc
{
    public class CommandLine
    {
        private static ICompiler GetCompiler(CommandLineOptions options)
        {
            if (options.compilerOutput == CompilerOutput.PSharp)
            {
                return new AntlrCompiler();
            }
            if (options.compilerService)
            {
                return new CompilerServiceClient();
            }
            return new Compiler(options.shortFileNames);
        }

        public static int Main(string[] args)
        {
            if (CommandLineOptions.ParseArguments(args, out CommandLineOptions options))
            {
                ICompiler compiler = GetCompiler(options);
                var output = new StandardOutput();
                bool result = options.isLinkerPhase
                                  ? compiler.Link(output, options)
                                  : compiler.Compile(output, options);
                return result ? 0 : -1;
            }

            CommandLineOptions.PrintUsage();
            return -1;
        }
    }
}
