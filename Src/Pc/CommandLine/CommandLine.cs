namespace Microsoft.Pc
{
    public static class CommandLine
    {
        public static int Main(string[] args)
        {
            if (!CommandLineOptions.ParseArguments(args, out var options))
            {
                CommandLineOptions.PrintUsage();
                return -1;
            }

            ICompiler compiler = new Compiler();
            var output = new DefaultCompilerOutput(options.OutputDirectory);
            bool result = compiler.Compile(output, options);
            return result ? 0 : -1;
        }
    }
}
