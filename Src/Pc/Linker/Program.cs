using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Pc
{
    internal class Linker
    {
        static int Main(string[] args)
        {
            bool result = false;
            CommandLineOptions options;
            if (CommandLineOptions.ParseLinkString(args, out options))
            {
                if (options.compilerService)
                {
                    // use separate process that contains pre-compiled P compiler.
                    CompilerServiceClient svc = new CompilerServiceClient();
                    if (string.IsNullOrEmpty(options.outputDir))
                    {
                        options.outputDir = Directory.GetCurrentDirectory();
                    }
                    result = svc.Link(options, Console.Out);
                }
                else
                {
                    Compiler compiler = new Compiler(true);
                    result = compiler.Link(new StandardOutput(), options);
                }
            }
            else
            {
                Console.WriteLine("USAGE: Plink.exe file.4ml [file2.4ml ...] [options]");
                Console.WriteLine("Takes the *.4ml output from pc.exe and generates the combined linker.c linker.h output from them");
                Console.WriteLine("/outputDir:path  -- where to write the linker.c and linker.h files");
                Console.WriteLine("/shared          -- use the compiler service");
                Console.WriteLine("/parallel        -- run multiple tests in parallel for quicker overall test run times");
                result = false;
            }
            return result ? 0 : -1;
        }
    }
}
