namespace RunPTool
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class Program
    {
        private const int FailCode = 1;
        private const string TestFilePattern = "testconfig*.txt";

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 2)
                {
                    Console.WriteLine("USAGE: RunPTool.exe [root dir] [reset]");
                }
                DirectoryInfo di = args.Length == 0
                    ? new DirectoryInfo(Environment.CurrentDirectory)
                    : new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, args[0]));

                if (!di.Exists)
                {
                    Console.WriteLine("Failed to run tests; directory {0} does not exist", di.FullName);
                    Environment.ExitCode = FailCode;
                    return;
                }

                Console.WriteLine("Running tests under {0}...", di.FullName);
                int testCount = 0, failCount = 0;
                bool reset = args.Length == 2
                    ? (args[1] == "reset") ? true : false
                    : false;
                Test(di, reset, ref testCount, ref failCount);

                Console.WriteLine();
                Console.WriteLine("Total tests: {0}, Passed tests: {1}. Failed tests: {2}", testCount, testCount - failCount, failCount);
                if (failCount > 0)
                {
                    Environment.ExitCode = FailCode;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to run tests - {0}", e.Message);
                Environment.ExitCode = FailCode;
            }
        }

        private static void Test(DirectoryInfo di, bool reset, ref int testCount, ref int failCount)
        {
            //TODO: try{} at the top level
            //enumerating files in the top dor only
            foreach (var fi in di.EnumerateFiles(TestFilePattern))
            {
                ++testCount;
                var checker = new CheckP.Checker(di.FullName, reset);
                if (!checker.Check(fi.Name))
                {
                    ++failCount;
                }
            }

            foreach (var dp in di.EnumerateDirectories())
            {
                Test(dp, reset, ref testCount, ref failCount);
            }
        }
    }
    
}
