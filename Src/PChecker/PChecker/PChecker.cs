using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace Plang.PChecker
{
    /// <summary>
    /// This class is a wrapper over the Coyote Tester to provide simple interface for analyzing P programs
    /// </summary>
    class PChecker
    {
        
        static int Main(string[] args)
        {
           
            switch (CommandLineOptions.ParseArguments(args, out PCheckerJobConfiguration job))
            {
                case CommandLineParseResult.Failure:
                case CommandLineParseResult.HelpRequested:
                    CommandLineOptions.PrintUsage();
                    return 1;
                case CommandLineParseResult.Success:
                    try
                    {
                        return 0;
                    }
                    catch (Exception e)
                    {

                        return 1;
                    }
            }
            return 0;
        }
    }
}
