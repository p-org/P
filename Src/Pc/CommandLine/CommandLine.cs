namespace Microsoft.Pc
{
    public class CommandLine
    {
        private static bool GetCompiler(CommandLineOptions options, out ICompiler compiler)
        {
            compiler = null;
            if (options.compilerOutput == CompilerOutput.PSharp)
            {
                compiler = new AntlrCompiler();
                return true;
            }

            if (options.compilerService)
            {
                compiler = new CompilerServiceClient();
                return true;
            }

            compiler = new LegacyCompiler(options.shortFileNames);
            return true;
        }

        public static int Main(string[] args)
        {
            if (!CommandLineOptions.ParseArguments(args, out CommandLineOptions options))
            {
                CommandLineOptions.PrintUsage();
                return -1;
            }

            if (!GetCompiler(options, out ICompiler compiler))
            {
                return -1;
            }

            var output = new StandardOutput();
            bool result = options.isLinkerPhase
                ? compiler.Link(output, options)
                : compiler.Compile(output, options);
            return result ? 0 : -1;
        }
    }
}
