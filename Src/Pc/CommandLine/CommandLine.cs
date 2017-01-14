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
                if (string.IsNullOrEmpty(options.outputDir))
                {
                    options.outputDir = Directory.GetCurrentDirectory();
                }
                if(!options.isLinkerPhase)
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
                if(!options.isLinkerPhase)
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
                Console.WriteLine("USAGE: Pc.exe file.p [options]");
                Console.WriteLine("Compiles *.p programs and produces *.4ml intermediate output which can then be passed to PLink.exe");
                Console.WriteLine("/outputDir:path         -- where to write the generated *.c, *.h and *.4ml files");
                Console.WriteLine("/liveness[:sampling]    -- these control what the Zing program is looking for");
                Console.WriteLine("/shortFileNames         -- print only file names in error messages");
                Console.WriteLine("/printTypeInference     -- dumps compiler type inference information (in formula)");
                Console.WriteLine("/dumpFormulaModel       -- write the entire formula model to a file named 'output.4ml'");
                Console.WriteLine("/profile                -- print detailed timing information");
                Console.WriteLine("/rebuild                -- rebuild all the P files");
                Console.WriteLine("/generate:[C0,C,Zing,C#]");
                Console.WriteLine("    C0  : generate C without model functions");
                Console.WriteLine("    C   : generate C with model functions");
                Console.WriteLine("    Zing: generate Zing");
                Console.WriteLine("    C#  : generate C# code");
                Console.WriteLine("/shared                 -- use the compiler service)"   );
                Console.WriteLine(" ------------ Linker Phase ------------");
                Console.WriteLine("USAGE: Pc.exe /link file1.4ml [file2.4ml ...] linkfile.p [options]");
                Console.WriteLine("Takes the *.4ml output from pc.exe and generates the combined linker.c linker.h output from them");
                Console.WriteLine("/outputDir:path  -- where to write the generated linker.c and linker.h files");
                Console.WriteLine("/shared          -- use the compiler service");
                Console.WriteLine("/parallel        -- run multiple tests in parallel for quicker overall test run times");
                Console.WriteLine("/profile         -- print detailed timing information");
                Console.WriteLine("Profiling can also be enabled by setting the environment variable PCOMPILE_PROFILE=1");
                return -1;
            }
        }
    }
}