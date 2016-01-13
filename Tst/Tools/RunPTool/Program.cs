using System.Data.SqlTypes;

namespace RunPTool
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CheckP;
    using System.Diagnostics;

    class Program
    {
        private const int FailCode = 1;
        private const string TestFilePattern = "testconfig.txt";
        private const string FailedTestsFile = "failed-tests.txt";
        private const string DisplayDiffsFile = "display-diffs.bat";
        private const string DiffTool = "kdiff3";

        private static PciProcess pciProcess;

        static void Main(string[] args)
        {
            try
            {
                bool reset = false;
                //set according to the name of the parent directory for "testconfig.txt":
                string testFilePath = null;
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    if (arg == "/reset")
                    {
                        reset = true;
                    }
                    else if (testFilePath == null && !arg.StartsWith("/"))
                    {
                        testFilePath = arg;
                    }
                    else
                    {
                        Console.WriteLine("USAGE: RunPTool.exe [file with test dirs] [/reset] [/pciPath:<path to Pci.exe>]");
                        return;
                    }
                }

                //tstDir is where testP.bat is located
                var tstDir = new DirectoryInfo(Environment.CurrentDirectory);
                List<DirectoryInfo> activeDirs;
                if (testFilePath == null)
                {
                    Console.WriteLine("Warning: no test directories file provided; running all tests under \\Tst");
                    activeDirs = new List<DirectoryInfo>();
                    activeDirs.Add(tstDir);
                }
                else
                {
                    activeDirs = ExtractActiveDirsFromFile(testFilePath, tstDir);
                    if (activeDirs == null)
                    {
                        Console.WriteLine("Failed to run tests: directory name(s) in the test directories file are in a wrong format");
                        Environment.ExitCode = FailCode;
                        return;
                    }
                    if (activeDirs.Count == 0)
                    {
                        Console.WriteLine("Failed to run tests: test directories file is blank");
                        Environment.ExitCode = FailCode;
                        return;
                    }
                }
                
                foreach (DirectoryInfo di in activeDirs)
                {
                    if (!di.Exists)
                    {
                        Console.WriteLine("Failed to run tests: directory {0} does not exist", di.FullName);
                        Environment.ExitCode = FailCode;
                        return;
                    }
                }

                Console.WriteLine("Running tests");
                int testCount = 0, failCount = 0;
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

                string executingProcessDirectoryName = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                string pciFilePath = Path.Combine(executingProcessDirectoryName, "..\\..\\..\\..\\..\\..\\Bld\\Drops\\Plang_Debug_x86\\Compiler\\Pci.exe");
                pciProcess = new PciProcess(pciFilePath);
                Test(activeDirs, reset, ref testCount, ref failCount, failedTestsWriter, displayDiffsWriter);
                pciProcess.Shutdown();

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
        private static void Test(List<DirectoryInfo> diArray, bool reset,  ref int testCount, ref int failCount,
            StreamWriter failedTestsWriter, StreamWriter displayDiffsWriter)
        {
            bool isSetExePc = false;
            bool isSetExeZing = false;
            bool isSetExePrt = false;
            string executingProcessDirectoryName =
                    Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string zingFilePath = Path.Combine(executingProcessDirectoryName,
                    "..\\..\\..\\..\\..\\..\\Bld\\Drops\\Plang_Debug_x86\\Compiler\\zinger.exe");
            foreach (DirectoryInfo di in diArray)
            {
                //enumerating files in the top dir only
                foreach (var fi in di.EnumerateFiles(TestFilePattern))
                {
                    ++testCount;

                    //if di.Name is Pc or Prt or Zing, leaf directory is reached;
                    //in that case, set isSetExePc, isSetExeZing, isSetExePrt:
                    //(we are assuming that test directories cannot have these names)
                    //Note:these directory names must be exactly Pc or Prt or Zing (they are case-sensitive)
                    if (di.Name == "Pc")   { isSetExePc = true; }
                    else if (di.Name == "Zing") { isSetExeZing = true; }
                    else if (di.Name == "Prt") {isSetExePrt = true; }
                    else
                    {
                        throw new Exception(string.Format("Incorrect directory name {0}; the only allowed names are Pc, Prt or Zing", di.Name));
                    }

                    var checker = new Checker(di.FullName, reset, isSetExePc, isSetExeZing, isSetExePrt, zingFilePath, pciProcess);
                    if (!checker.Check(fi.Name))
                    {
                        ++failCount;
                        //add directory of the failing test to "failed_tests.txt": 
                        failedTestsWriter.WriteLine("{0}", di.FullName);
                        //add diffing command to "display_diff.bat":
                        displayDiffsWriter.WriteLine("{0} {1}\\acc_0.txt {1}\\check-output.log", DiffTool, di.FullName);
                    }
                }

                //Since order of directory processing is significant (Pc should be processed before
                //Zing and Prt), order enumerated directories alphabetically:
                var dirs = (from dir in di.EnumerateDirectories()
                            orderby dir.FullName ascending
                            select dir);

                foreach (var dp in dirs)
                {
                    List<DirectoryInfo> dpArray = new List<DirectoryInfo>();
                    dpArray.Add(dp);
                    Test(dpArray, reset, ref testCount, ref failCount, failedTestsWriter, displayDiffsWriter);
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
        private static List<DirectoryInfo> ExtractActiveDirsFromFile(string fileName, DirectoryInfo tstDir)
        {
            List<DirectoryInfo> result = new List<DirectoryInfo>();
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

                        result.Add(new DirectoryInfo(Path.Combine(tstDir.FullName, dir)));
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
