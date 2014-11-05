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
        private const string failedDirsFile = "failed-tests.txt";
        private const string displayDiffsFile = "display-diffs.bat";
        private const string diffTool = "kdiff3";

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

                //Replace old "failed-tests.txt" and "display-diffs.bat" with newly created files:
                File.Delete(Path.Combine(Environment.CurrentDirectory, failedDirsFile));
                StreamWriter failedDirsWriter;
                if (!OpenSummaryStream(failedDirsFile, out failedDirsWriter))
                {
                    throw new Exception("Cannot open failed-tests.txt for writing");
                }

                File.Delete(Path.Combine(Environment.CurrentDirectory, displayDiffsFile));
                StreamWriter displayDiffsWriter;
                if (!OpenSummaryStream(displayDiffsFile, out displayDiffsWriter))
                {
                    throw new Exception("Cannot open display-diffs.bat for writing");
                }

                Test(di, reset, ref testCount, ref failCount, failedDirsWriter, displayDiffsWriter);

                Console.WriteLine();
                Console.WriteLine("Total tests: {0}, Passed tests: {1}. Failed tests: {2}", testCount, testCount - failCount, failCount);
               
                if (failCount > 0)
                {
                    Console.WriteLine("Test directories with failed tests: failed-tests.txt");
                  
                    Console.WriteLine("To run kdiff3 on outputs for all failed tests: run display-diffs.bat");
                    
                    if (!CloseSummaryStream(failedDirsFile, failedDirsWriter))
                    {
                        throw new Exception("Cannot close failed-tests.txt");
                    }
                    if (!CloseSummaryStream(displayDiffsFile, displayDiffsWriter))
                    {
                        throw new Exception("Cannot close display-diffs.bat");
                    }

                    Environment.ExitCode = FailCode;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to run tests - {0}", e.Message);
                Environment.ExitCode = FailCode;
            }
        }

        private static void Test(DirectoryInfo di, bool reset, ref int testCount, ref int failCount, StreamWriter failedDirsWriter, StreamWriter displayDiffsWriter)
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
                    //add directory of the failing test to "failed_tests.txt": 
                    failedDirsWriter.WriteLine("{0}", di.FullName);
                    //add diffing command to "display_diff.bat":
                    displayDiffsWriter.WriteLine("{0} {1}\\acc_0.txt {1}\\check-output.log", diffTool, di.FullName);
                }            
            }

            foreach (var dp in di.EnumerateDirectories())
            {
                Test(dp, reset, ref testCount, ref failCount, failedDirsWriter, displayDiffsWriter);
            }
        }
        private static bool OpenSummaryStream(string fileName, out StreamWriter wr)
        {
            wr = null;
            try
            {
                wr = new StreamWriter(Path.Combine(Environment.CurrentDirectory, fileName));
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "ERROR: Could not open summary file {0} - {1}",
                    fileName,
                    e.Message);
                return false;
            }

            return true;
        }
        private static bool CloseSummaryStream(string fileName, StreamWriter wr)
        {
            try
            {
                wr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "ERROR: Could not close summary file {0} - {1}",
                    fileName,
                    e.Message);
                return false;
            }

            return true;
        }
    }
    
}
