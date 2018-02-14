using System;

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
            #if NET461
            if (options.compilerService)
            {
                return new CompilerServiceClient();
            }
            return new LegacyCompiler(options.shortFileNames);
            #else
            Console.WriteLine("Legacy backend unsupported in .NET Core builds.");
            return null;
            #endif
        }

        public static int Main(string[] args)
        {
            if (CommandLineOptions.ParseArguments(args, out CommandLineOptions options))
            {
                ICompiler compiler = GetCompiler(options);
                if (compiler == null)
                {
                    return -1;
                }
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
