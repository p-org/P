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
            bool sharedCompiler;
            CommandLineOptions options;
            if (!CommandLineOptions.ParseCompileString(args, out sharedCompiler, out options))
            {
                goto error;
            }
            string inputFileName = options.inputFileName;
            if (inputFileName != null && inputFileName.Length > 2 && inputFileName.EndsWith(".p"))
            {
                bool result = false;
                if (sharedCompiler)
                {
                    // use separate process that contains pre-compiled P compiler.
                    CompilerServiceClient svc = new CompilerServiceClient();
                    // compiler service requires full path.
                    options.inputFileName = Path.GetFullPath(inputFileName);
                    if (string.IsNullOrEmpty(options.outputDir))
                    {
                        options.outputDir = Directory.GetCurrentDirectory();
                    }
                    result = svc.Compile(options, Console.Out);
                }
                else
                {
                    var compiler = new Compiler(options.shortFileNames);
                    result = compiler.Compile(new StandardOutput(), options);
                }
                if (!result)
                {
                    return -1;
                }
                return 0;
            }
            else
            {
                Console.WriteLine("Illegal input file name");
            }
        error:
            {
                Console.WriteLine("USAGE: Pc.exe file.p [options]");
                Console.WriteLine("/outputDir:path");
                Console.WriteLine("/outputFileName:name");
                Console.WriteLine("/test");
                Console.WriteLine("/liveness[:mace]");
                Console.WriteLine("/shortFileNames");
                Console.WriteLine("/printTypeInference");
                Console.WriteLine("/dumpFormulaModel");
                Console.WriteLine("/profile");
                Console.WriteLine("/generate:[C,Zing]");
                Console.WriteLine("/shared");
                return 0;
            }
        }
    }
}