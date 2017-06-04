using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Pc
{
    public class CommandLine
    {
        private static int Main(string[] args)
        {
            CommandLineOptions options = new CommandLineOptions();
            if (!options.ParseArguments(args))
            {
                goto error;
            }
            bool result;
            if (options.compilerService)
            {
                // use separate process that contains pre-compiled P compiler.
                CompilerServiceClient svc = new CompilerServiceClient();
                if (!options.isLinkerPhase)
                {
                    result = svc.Compile(options, Console.Out);
                }
                else
                {
                    result = svc.Link(options, Console.Out);
                }
                
            }
            else
            {
                var compiler = new Compiler(options.shortFileNames);
                if (!options.isLinkerPhase)
                {
                    result = compiler.Compile(new StandardOutput(), options);
                }
                else
                {
                    result = compiler.Link(new StandardOutput(), options);
                }
                
            }
            if (!result)
            {
                return -1;
            }
            return 0;

            error:
            {
                Console.WriteLine(" ------------ Compiler Phase ------------");
                Console.WriteLine("USAGE: Pc.exe file1.p [file2.p ...] [/t:tfile.4ml] [/r:rfile.4ml ...] [options]");
                Console.WriteLine("Compiles *.p programs and produces .4ml summary file which can then be passed to PLink.exe");
                Console.WriteLine("/t:file.4ml             -- name of summary file produced for this compilation unit; if not supplied then file1.4ml");
                Console.WriteLine("/r:file.4ml             -- refer to another summary file");
                Console.WriteLine("/outputDir:path         -- where to write the generated files");
                Console.WriteLine("/shared                 -- use the compiler service");
                Console.WriteLine("/profile                -- print detailed timing information");
                Console.WriteLine("/generate:[C,C#,Zing]");
                Console.WriteLine("    C   : generate C without model functions");
                Console.WriteLine("    C#  : generate C# with model functions");
                Console.WriteLine("    Zing: generate Zing with model functions");
                Console.WriteLine("/liveness[:sampling]    -- controls compilation for Zinger");
                Console.WriteLine("/shortFileNames         -- print only file names in error messages");
                Console.WriteLine("/dumpFormulaModel       -- write the entire formula model to a file named 'output.4ml'");
                Console.WriteLine(" ------------ Linker Phase ------------");
                Console.WriteLine("USAGE: Pc.exe [linkfile.p] /link /r:file1.4ml [/r:file2.4ml ...] [options]");
                Console.WriteLine("Links *.4ml summary files against an optional linkfile.p and generates linker.{h,c,dll}");
                Console.WriteLine("/outputDir:path  -- where to write the generated files");
                Console.WriteLine("/shared          -- use the compiler service");
                Console.WriteLine("/profile         -- print detailed timing information");
                Console.WriteLine("Profiling can also be enabled by setting the environment variable PCOMPILE_PROFILE=1");
                return -1;
            }
        }
    }
}