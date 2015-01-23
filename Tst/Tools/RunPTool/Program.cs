using System.Data.SqlTypes;

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
        private const string FailedTestsFile = "failed-tests.txt";
        private const string DisplayDiffsFile = "display-diffs.bat";
        private const string DiffTool = "kdiff3";

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 2)
                {
                    Console.WriteLine("USAGE: RunPTool.exe [root dir] [reset]");
                }
                //tstDir is where testP.bat is located
                //Case 1 (debugging only): Running from VS Debugger:
                //Environment.CurrentDirectory is Tst\Tools\RunPTool\bin\Debug
                //var tstDir = new DirectoryInfo("..\\..\\..\\..\\..\\Tst");
                //Case 2: Running from testP.bat:
                //Environment.CurrentDirectory is Tst
                var tstDir = new DirectoryInfo(Environment.CurrentDirectory);
                //Console.WriteLine("tstDir is {0}", tstDir.FullName);
                int activeDirsCount = 0;
                DirectoryInfo[] activeDirs = new DirectoryInfo[200];
                if (args.Length == 0)
                {
                    Console.WriteLine("Warning: no test directories file provided; running all tests under \\Tst");
                    activeDirsCount = 1;
                    activeDirs[0] = tstDir;

                }
                else activeDirs = ExtractActiveDirsFromFile(args[0], tstDir, out activeDirsCount);
               
                if (activeDirs == null)
                {
                    Console.WriteLine("Failed to run tests: directory name(s) in the test directories file are in a wrong format");
                    Environment.ExitCode = FailCode;
                    return;
                }
                if (activeDirsCount == 0)
                {
                    Console.WriteLine("Failed to run tests: test directories file is blank");
                    Environment.ExitCode = FailCode;
                    return;
                }
                for (int i = 0; i < activeDirsCount; ++i)
                {
                    if (!activeDirs[i].Exists)
                    {
                        Console.WriteLine("Failed to run tests: directory {0} does not exist", activeDirs[i].FullName);
                        Environment.ExitCode = FailCode;
                        return;
                    }
                }

                Console.WriteLine("Running tests");
                int testCount = 0, failCount = 0;
                bool reset = args.Length == 2
                    ? (args[1] == "reset") ? true : false
                    : false;

                StreamWriter failedTestsWriter = null;
                StreamWriter displayDiffsWriter = null;
                //If reset = false, replace old "failed-tests.txt" and "display-diffs.bat" with newly created files:
                if (!reset)
                {
                    File.Delete(Path.Combine(Environment.CurrentDirectory, FailedTestsFile));
                    if (!OpenSummaryStream(FailedTestsFile, out failedTestsWriter))
                    {
                        throw new Exception("Cannot open failed-tests.txt for writing");
                    }

                    File.Delete(Path.Combine(Environment.CurrentDirectory, DisplayDiffsFile));
                    if (!OpenSummaryStream(DisplayDiffsFile, out displayDiffsWriter))
                    {
                        throw new Exception("Cannot open display-diffs.bat for writing");
                    }

                }

                Test(activeDirs, activeDirsCount, reset, ref testCount, ref failCount, failedTestsWriter, displayDiffsWriter);

                Console.WriteLine();
                Console.WriteLine("Total tests: {0}, Passed tests: {1}. Failed tests: {2}", testCount, testCount - failCount, failCount);
               
                if (failCount > 0)
                {
                    Console.WriteLine("List of all failed tests: failed-tests.txt");
                  
                    Console.WriteLine("To run kdiff3 on outputs for all failed tests: run display-diffs.bat");
                    
                    if (!CloseSummaryStream(FailedTestsFile, failedTestsWriter))
                    {
                        throw new Exception("Cannot close failed-tests.txt");
                    }
                    if (!CloseSummaryStream(DisplayDiffsFile, displayDiffsWriter))
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

        //If reset = true, failedDirsWriter and displayDiffsWriter are "null"
        private static void Test(DirectoryInfo[] diArray, int diArrayCount, bool reset, ref int testCount, ref int failCount,
            StreamWriter failedTestsWriter, StreamWriter displayDiffsWriter)
        {
            //TODO: try{} at the top level
            for (int i = 0; i < diArrayCount; ++i)
            {
                //enumerating files in the top dir only
                foreach (var fi in diArray[i].EnumerateFiles(TestFilePattern))
                {
                    ++testCount;
                    //Console.WriteLine("Running CheckP under dir {0}", diArray[i].FullName);
                    var checker = new CheckP.Checker(diArray[i].FullName, reset);
                    if (!checker.Check(fi.Name))
                    {
                        ++failCount;
                        //add directory of the failing test to "failed_tests.txt": 
                        failedTestsWriter.WriteLine("{0}", diArray[i].FullName);
                        //add diffing command to "display_diff.bat":
                        displayDiffsWriter.WriteLine("{0} {1}\\acc_0.txt {1}\\check-output.log", DiffTool, diArray[i].FullName);
                    }
                }

                //Since order of directory processing is significant (Pc should be processed before
                //Zing and Prt), order enumerated directories alphabetically:
                var dirs = (from dir in diArray[i].EnumerateDirectories()
                    orderby dir.FullName ascending
                    select dir);
 
                foreach (var dp in dirs)
                {
                    DirectoryInfo[] dpArray = new DirectoryInfo[] {dp};
                    Test(dpArray, 1, reset, ref testCount, ref failCount, failedTestsWriter, displayDiffsWriter);
                }
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

        //generate list of directories for running regression from the input file (1st argument of testP.bat)
        private static DirectoryInfo[] ExtractActiveDirsFromFile(string fileName, DirectoryInfo tstDir, out int count)
        {
            DirectoryInfo[] result = new DirectoryInfo[200];
            count = 0;
            //var fileExists = File.Exists(Path.Combine(tstDir.FullName, fileName));
  
            try
            {
                using (var sr = new StreamReader(Path.Combine(tstDir.FullName, fileName)))
                {
                    while (!sr.EndOfStream)
                    {
                        var dir = sr.ReadLine();
                        //Skip the line if it is blank:
                        if ((dir.Trim() == "")) break;

                        if (dir.StartsWith("\\") || dir.StartsWith("/") || dir.StartsWith("\\\\"))
                        {
                            Console.WriteLine("Failed to run tests: directory name in the test directory file cannot start with \"\\\" or \"/\" or \"\\\\\"");
                            return null;
                        }
                      
                        result[count] = new DirectoryInfo(Path.Combine(tstDir.FullName, dir));
                        count = count + 1;
                    }
                }

            }
            catch (Exception e)
            {
                {
                    Console.WriteLine("Failed to read regression dirs from input file - {0}", e.Message);
                    Environment.ExitCode = FailCode;                   
                }
            }
            return result;
        }
    }
    
}
