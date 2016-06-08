using System.Data.SqlTypes;
using Microsoft.Build.Tasks;

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
        private const string ConfigFileName = "testconfig.txt";
        private const string ConfigFilePattern = "testconfig*.txt";
        //Generated for viewing failed subtests: 
        private const string FailedTestsFile = "failed-tests.txt";
        //Generated to use for resetting acceptors for failed tests,
        //as opposed to subtests as in "failed-tests.txt";
        //paths to subtests are not allowed in the test dir list
        //passed as a parameter to the regression tool
        private const string FailedTestsToResetFile = "failed-tests-for-reset.txt";
        private const string DisplayDiffsFile = "display-diffs.bat";
        private const string DiffTool = "kdiff3";

        private static PciProcess pciProcess;

        bool reset;
        bool cooperative; // for testing cooperative multitasking.

        //set according to the name of the parent directory for "testconfig.txt":
        string testFilePath;
        string execsToRun;
        static string testRoot; // the Tst directory

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '/' || arg[0] == '-')
                {
                    string option = (arg.Substring(1).ToLowerInvariant());
                    if (option == "reset")
                    {
                        reset = true;
                    }
                    else if (option == "cooperative")
                    {
                        cooperative = true;
                    }
                    else if(option.StartsWith("run"))
                    {
                        execsToRun = arg;
                    }
                    else
                    {
                        Console.WriteLine("### Unrecognized option: " + arg);
                        return false;
                    }
                }
                else if (testFilePath == null)
                {
                    testFilePath = arg;
                }
                else
                {
                    Console.WriteLine("### Too many arguments");
                    return false;
                }
            }
            return true;
        }

       static void PrintUsage()
        {
            Console.WriteLine("USAGE: RunPTool.exe  [options] [file with test dirs]");
            Console.WriteLine("Options:");
            Console.WriteLine("    /reset - remove old acceptor file, useful for generating new test baselines");
            Console.WriteLine("    /cooperative - enable testing of cooperative multitasking");
            Console.WriteLine("    /runPc - do the compile step only");
            Console.WriteLine("    /runPrt - run the compiled state machine using PrtTester");
            Console.WriteLine("    /runZing - run zinger on the compiled output");
            Console.WriteLine("    /runAll (default)");
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }
            p.Run();
        }

        void Run()
        { 
            try
            {
                // this should be the script directory.
                var tstDir = new DirectoryInfo(Environment.CurrentDirectory);
                if (!File.Exists(Path.Combine(tstDir.FullName, "testP.bat")))
                {
                    // Hmmm, we might be debugging the app, so lets see if we can find the tstDir.
                    Uri uri = new Uri(tstDir.FullName); //  D:\git\P\Bld\Drops\Debug\x64\Binaries
                    if (uri.Segments.Length > 5 && 
                        string.Compare(uri.Segments[uri.Segments.Length-1], "Binaries", StringComparison.OrdinalIgnoreCase) == 0 &&
                        string.Compare(uri.Segments[uri.Segments.Length - 5], "Bld/", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        Uri resolved = new Uri(uri, @"..\..\..\..\Tst");
                        tstDir = new DirectoryInfo(resolved.LocalPath);
                    }
                }

                testRoot = tstDir.FullName;


                if (execsToRun == null)
                {
                    execsToRun = "/runAll";
                }

                //tstDir is where testP.bat is located
                List<DirectoryInfo> activeDirs;
                if (testFilePath == null)
                {
                    Console.WriteLine("Warning: no test directories file provided; running all tests under {0}", tstDir.FullName);
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
                        Console.WriteLine("Failed to run tests: no tests in the test directories file");
                        Environment.ExitCode = FailCode;
                        return;
                    }
                    //Type-check list of test dirs:
                    //Check that test dirs do not contain paths to Pc/Zing/Prt:
                    bool result = CheckTopPaths(activeDirs);
                    if (!result)
                    {
                        return;
                    }
                    //Check other rules recursively:
                    result = CheckTestDirs(activeDirs);                
                    if (!result)
                    {
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
                StreamWriter failedTestsToResetWriter = null;
                StreamWriter tempWriter = null;
                StreamReader tempReader = null;
                StreamWriter displayDiffsWriter = null;
                //If reset = false, replace old "failed-tests.txt" and "display-diffs.bat" with newly created files:
                if (!reset)
                {
                    if (File.Exists(Path.Combine(Environment.CurrentDirectory, FailedTestsFile)))
                    {
                        File.Delete(Path.Combine(Environment.CurrentDirectory, FailedTestsFile));
                    }
                    if (File.Exists(Path.Combine(Environment.CurrentDirectory, FailedTestsToResetFile)))
                    {
                        File.Delete(Path.Combine(Environment.CurrentDirectory, FailedTestsToResetFile));
                    }
                    if (File.Exists(Path.Combine(Environment.CurrentDirectory, "tempReset.txt")))
                    {
                        File.Delete(Path.Combine(Environment.CurrentDirectory, "tempReset.txt"));
                    }
                    if (File.Exists(Path.Combine(Environment.CurrentDirectory, DisplayDiffsFile)))
                    {
                        File.Delete(Path.Combine(Environment.CurrentDirectory, DisplayDiffsFile));
                    }
                    
                    if (!OpenSummaryStreamWriter(FailedTestsFile, out failedTestsWriter))
                    {
                        throw new Exception("Cannot open failed-tests.txt for writing");
                    }
                    if (!OpenSummaryStreamWriter(FailedTestsToResetFile, out failedTestsToResetWriter))
                    {
                        throw new Exception("Cannot open failed-tests-to-reset.txt for writing");
                    }
                    if (!OpenSummaryStreamWriter("tempReset.txt", out tempWriter))
                    {
                        throw new Exception("Cannot open tempReset.txt for writing");
                    }
                    if (!OpenSummaryStreamWriter(DisplayDiffsFile, out displayDiffsWriter))
                    {
                        throw new Exception("Cannot open display-diffs.bat for writing");
                    }

                }

                string executingProcessDirectoryName = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                string pciFilePath = Path.Combine(executingProcessDirectoryName, "Pci.exe");
                if (!File.Exists(pciFilePath))
                {
                    Console.WriteLine("Cannot find pci.exe");
                    return;
                }
                pciProcess = new PciProcess(pciFilePath);

                Test(activeDirs, reset, cooperative, execsToRun, ref testCount, ref failCount, failedTestsWriter, tempWriter, displayDiffsWriter);
                
                pciProcess.Shutdown();

                Console.WriteLine();
                Console.WriteLine("Total tests: {0}, Passed tests: {1}, Failed tests: {2}", testCount, testCount - failCount, failCount);

                if (failCount > 0)
                {
                    if (!CloseSummaryStreamWriter("tempReset.txt", tempWriter))
                    {
                        throw new Exception("Cannot close tempReset.txt");
                    }
                    //open the reader (from the same file):
                    if (!OpenSummaryStreamReader("tempReset.txt", out tempReader))
                    {
                        throw new Exception("Cannot open tempReset.txt for reading");
                    }
                    RemoveDupTests(failedTestsToResetWriter, tempReader);

                    if (!CloseSummaryStreamWriter(FailedTestsFile, failedTestsWriter))
                    {
                        throw new Exception("Cannot close failed-tests.txt");
                    }
                    if (!CloseSummaryStreamWriter(FailedTestsToResetFile, failedTestsToResetWriter))
                    {
                        throw new Exception("Cannot close failed-tests-for-reset.txt");
                    }
                    if (!CloseSummaryStreamReader("tempReset.txt", tempReader))
                    {
                        throw new Exception("Cannot close tempReset.txt");
                    }
                    File.Delete(Path.Combine(Environment.CurrentDirectory, "tempReset.txt"));
                    if (!CloseSummaryStreamWriter(DisplayDiffsFile, displayDiffsWriter))
                    {
                        throw new Exception("Cannot close display-diffs.bat");
                    }

                    Environment.ExitCode = FailCode;
                    Console.WriteLine("List of all failed subtests: failed-tests.txt");
                    Console.WriteLine("List of all failed tests (to use for reset): failed-tests-for-reset.txt");
                    Console.WriteLine("To run kdiff3 on outputs for all failed tests: run display-diffs.bat");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to run tests - {0}", e.Message);
                Environment.ExitCode = FailCode;
            }
        }
        //Check paths in the list that they do not contain paths to Pc/Zing/Prt:
        private static bool CheckTopPaths(List<DirectoryInfo> diArray)
        {
            bool result = true;
            foreach (DirectoryInfo di in diArray)
            {
                if (!di.Exists)
                {
                    Console.WriteLine("Directory {0} does not exist", di.FullName);
                    Console.WriteLine("");
                    Environment.ExitCode = FailCode;
                    result = false;
                }

                if ((di.Name == "Pc") || (di.Name == "Zing") || (di.Name == "Prt"))
                {
                    Console.WriteLine("Test directory list cannot contain path to Pc, Zing or Prt dir:");
                    Console.WriteLine("{0}", di.FullName);
                    Console.WriteLine("Replace with path to the parent dir");
                    Console.WriteLine("");
                    result = false;
                }
            }
            return result;
        }
        
        //Type-check list of test dirs:
        //TODO There's no error reported if there exists a test dir which doesn't contain
        //any of the subdirs Pc/Zing/Prt - the test would just be skipped
        private static bool CheckTestDirs(List<DirectoryInfo> diArray)
        {
            //TODO Add exception handling?
            try
            {
                foreach (DirectoryInfo di in diArray)
                {
                    //check that if di.Name is Pc/Zing/Prt, testconfig.txt is present in it:
                    if ((di.Name == "Pc") || (di.Name == "Zing") || (di.Name == "Prt"))
                    {
                        if (!File.Exists(Path.Combine(di.FullName, ConfigFileName)))
                        {
                            Console.WriteLine("Config file testconfig.txt should exist under Pc, Zing or Prt dir:");
                            Console.WriteLine("{0}", di.FullName);
                            return false;
                        }
                    }

                    foreach (var fi in di.EnumerateFiles(ConfigFilePattern))
                    {
                        //assuming that leaf directory Pc, Prt or Zing is reached:
                        //fail if wrong name for config. file:
                        if (fi.Name != "testconfig.txt")
                        {
                            Console.WriteLine("Incorrect configuration file name: {0} in folder: ", fi.Name);
                            Console.WriteLine("{0}", di.FullName);
                            return false;
                        }
                        else
                        {
                            //check that for each "testconfig.txt" file, parent dir is called Pc, Zing or Prt:
                            if ((di.Name != "Pc") && (di.Name != "Zing") && (di.Name != "Prt"))
                            {
                                Console.WriteLine("Incorrect location of config file under:");
                                Console.WriteLine("{0}", di.FullName);
                                Console.WriteLine("Config files should only be located in Pc, Zing or Prt dirs");
                                return false;
                            }
                            //If current dir is not Pc (hence, Prt or Zing), check that Pc dir exists at the parent dir level:
                            if (di.Name != "Pc")
                            {
                                if ((di.Parent != null) && !Directory.Exists(Path.Combine(di.Parent.FullName, "Pc")))
                                {
                                    Console.Write("For test dir \n{0}\nno Pc subdir exists", di.Parent.FullName);
                                    return false;
                                }

                            }
                            //Check that that there's no subdirs under Pc/Zing/Prt:
                            foreach (var sdi in di.EnumerateDirectories())
                            {
                                Console.Write("No subdirs are allowed under Pc/Zing/Prt dirs \n{0}", sdi.FullName);
                                return false;
                            }
                        }
                    }
                    foreach (var dp in di.EnumerateDirectories())
                    {
                        List<DirectoryInfo> dpArray = new List<DirectoryInfo>();
                        dpArray.Add(dp);
                        var result = CheckTestDirs(dpArray);
                        if (!result)
                        {
                            return false;
                        }
                    }
                    //return true;
                }
                return true;
            }
            catch (Exception e)
            {
                {
                    Console.WriteLine("Error in CheckTestDirs - {0}", e.Message);
                    //Environment.ExitCode = FailCode;
                    return false;
                }
            }
        }
        //If reset = true, failedDirsWriter and displayDiffsWriter are "null"
        private static void Test(List<DirectoryInfo> diArray, bool reset,  bool cooperative, string execsToRun, ref int testCount, ref int failCount,
            StreamWriter failedTestsWriter, StreamWriter tempWriter, StreamWriter displayDiffsWriter)
        {
            try
            {
                string zingFilePath = Path.GetFullPath(Path.Combine(testRoot, @"..\Bld\Drops\Release\x86\Binaries\zinger.exe"));

                if (!File.Exists(zingFilePath))
                {
                    zingFilePath = Path.GetFullPath(Path.Combine(testRoot, @"..\Bld\Drops\Debug\x86\Binaries\zinger.exe"));
                }

                if (!File.Exists(zingFilePath))
                {
                    Console.WriteLine("ERROR in Test: zinger.exe not find in {0}", zingFilePath);
                    Console.WriteLine(@"Please run ~\Bld\build.bat");
                    return;
                }

                foreach (DirectoryInfo di in diArray)
                {
                    //enumerating files in the top dir only

                    foreach (var fi in di.EnumerateFiles(ConfigFileName))
                    {
                        //if di.Name is Pc or Prt or Zing, leaf directory is reached;
                        //(we are assuming that test directories cannot have these names)
                        //Note:these directory names must be exactly Pc or Prt or Zing (they are case-sensitive)

                        if ((di.Name == "Pc") ||
                            (di.Name == "Zing" && (execsToRun == "/runZing" || execsToRun == "/runAll")) ||
                            (di.Name == "Prt" && (execsToRun == "/runPrt" || execsToRun == "/runAll")))
                        {
                            ++testCount;
                            var checker = new Checker(di.FullName, testRoot, reset, cooperative, di.Name, execsToRun, zingFilePath, pciProcess);
                            if ((di.Parent != null) && !checker.Check(fi.Name))
                            {
                                ++failCount;
                                //add directory of the failed (sub)test to "failed_tests.txt": 
                                failedTestsWriter.WriteLine("{0}", di.FullName);
                                //add directory of the failed test to "tempFailed.txt": 
                                //Console.WriteLine("+++++Writing to tempFailed: {0}", di.Parent.FullName);
                                tempWriter.WriteLine("{0}", di.Parent.FullName);
                                //add diffing command to "display_diff.bat":
                                displayDiffsWriter.WriteLine("{0} {1}\\acc_0.txt {1}\\check-output.log", DiffTool,
                                    di.FullName);
                            }
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
                        Test(dpArray, reset, cooperative, execsToRun, ref testCount, ref failCount, failedTestsWriter, tempWriter,
                            displayDiffsWriter);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "ERROR in Test: {0}",
                    e.Message);
            }
        }
        //copy unique paths from src file into dest file
        //to keep it simple, this code only removes consecutive duplicate lines,
        //which should be good enough in most cases; the only dups that would be 
        //left are from duplicate tests in the original list of test dirs
        private static void RemoveDupTests(StreamWriter destWr, StreamReader srcRd)
        {
            //Console.WriteLine("+++++RemoveDupTests: start");
            try
            {
                string currentLine = srcRd.ReadLine();
                HashSet<string> previousLines = new HashSet<string>();

                //while ((currentLine = srcRd.ReadLine()) != null)
                //Debug:
                //if (currentLine == null)
                //{
                    //Console.WriteLine("+++++RemoveDupTests: currentLine is null at the beginning");
                //}
                while (currentLine != null)
                {

                    // Add returns true if it was actually added,
                    // false if it was already there
                    var res = previousLines.Add(currentLine);
                    //Console.WriteLine("+++++RemoveDupTests: res is {0}", res);
                    if (res)
                    {
                        //Console.WriteLine("+++++RemoveDupTests: writing {0} into failed-for-reset.txt", currentLine);
                        destWr.WriteLine(currentLine);
                    }
                    currentLine = srcRd.ReadLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "ERROR in creating failed-tests-for-reset.txt: {0}",
                    e.Message);
            }
        }
        private static bool OpenSummaryStreamWriter(string fileName, out StreamWriter wr)
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
        private static bool OpenSummaryStreamReader(string fileName, out StreamReader rd)
        {
            rd = null;
            try
            {
                rd = new StreamReader(Path.Combine(Environment.CurrentDirectory, fileName));
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
        private static bool CloseSummaryStreamWriter(string fileName, StreamWriter wr)
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
        private static bool CloseSummaryStreamReader(string fileName, StreamReader rd)
        {
            try
            {
                rd.Close();
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
                Uri combined = new Uri(new Uri(tstDir.FullName + "\\"), fileName);
                using (var sr = new StreamReader(combined.LocalPath))
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
