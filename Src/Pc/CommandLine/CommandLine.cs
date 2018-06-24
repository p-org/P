namespace Microsoft.Pc
{
    public static class CommandLine
    {
        public static int Main(string[] args)
        {
            if (!CommandLineOptions.ParseArguments(args, out CommandLineOptions options))
            {
                CommandLineOptions.PrintUsage();
                return -1;
            }

            ICompiler compiler = new AntlrCompiler();
            var output = new StandardOutput();
            bool result = options.isLinkerPhase
                ? compiler.Link(output, options)
                : compiler.Compile(output, options);
            return result ? 0 : -1;
        }
    }
}
