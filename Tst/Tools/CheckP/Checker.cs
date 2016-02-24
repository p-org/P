using System.Threading;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace CheckP
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class PciProcess
    {
        Process pciProcess;
        AutoResetEvent evt;
        public string outputString;
        public string errorString;
        public bool pciInitialized = false;
        public bool loadSucceeded = true;

        public PciProcess(string pciPath)
        {
            try
            {
                outputString = "";
                errorString = "";
                evt = new AutoResetEvent(false);
                pciProcess = new Process();
                pciProcess.StartInfo = new ProcessStartInfo(pciPath, "/shortFileNames /server /doNotErase")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                pciProcess.ErrorDataReceived += pciProcess_ErrorDataReceived;
                pciProcess.OutputDataReceived += pciProcess_OutputDataReceived;
                pciProcess.Start();
                pciProcess.BeginErrorReadLine();
                pciProcess.BeginOutputReadLine();
                evt.WaitOne();
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                throw new Exception(string.Format("Unable to start the Pci process {0}: {1}", pciProcess.StartInfo.FileName, e.Message));
            }
        }

        private void pciProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string data = "" + e.Data;
            if (!pciInitialized && data == "Pci: initialization succeeded")
            {
                pciInitialized = true;
                evt.Set();
            }
            else if (data == "Pci: command done")
            {
                evt.Set();
            }
            else if (data.StartsWith("Pci: load failed"))
            {
                loadSucceeded = false;
                evt.Set();
            }
            else
            {
                outputString += string.Format("OUT: {0}\r\n", data);
            }
        }

        private void pciProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == "Pci: command done")
                evt.Set();
            else
                errorString += string.Format("ERROR: {0}\r\n", e.Data);
        }

        public void Run(string command, IEnumerable<string> args)
        {
            string str = command;
            foreach (string arg in args)
            {
                str += " ";
                str += arg;
            }
            evt.Reset();
            pciProcess.StandardInput.WriteLine(str);
            evt.WaitOne();
        }

        public void Reset()
        {
            loadSucceeded = true;
            outputString = "";
            errorString = "";
        }
        public void Shutdown()
        {
            evt.Dispose();
            pciProcess.StandardInput.WriteLine("exit");
        }
    }

    public class Checker
    {
        private const int BufferSize = 1024;

        private const string IncludePcOption = "inc";
        private const string IncludeZingerOption = "inc";
        private const string IncludePrtOption = "inc";
        private const string DescrOption = "dsc";
        private const string ArgsPcOption = "arg";
        private const string ArgsZingerOption = "arg";
        private const string ArgsPrtOption = "arg";
        private const string DelOption = "del";

        private const string TmpStreamFile = "check-tmp.txt";
        private const string AccFiles = "acc*.txt";
        private const string AccPrefix = "acc";
        private const string AccExt = ".txt";
        private const string LogFile = "check-output.log";
        private const string RuntimeFile1 = "program.c";
        private const string RuntimeFile2 = "program.h";
        private const string RuntimeFile3 = "stubs.c";
        private const string buildLogFileName = "testerBuildLogFile.txt";

        private static readonly string[] AllOptions = new string[]
        {
            IncludePcOption,
            IncludeZingerOption,
            IncludePrtOption,
            DescrOption,
            ArgsPcOption,
            ArgsZingerOption,
            ArgsPrtOption,
            DelOption
        };

        private string activeDirectory;
        private bool reset = false;
        //Pc, Prt or Zing:
        private string parentDir = null;
		private string execsToRun = null;
        private PciProcess pciProcess;
        private string zingFilePath;
        private string testRoot;

        public string Description
        {
            get;
            private set;
        }

        public Checker(string activeDirectory, string testRoot, bool reset, string parentDir, string execsToRun, string zingFilePath, PciProcess pciProcess)
        {
            this.activeDirectory = activeDirectory;
            this.reset = reset;
            this.parentDir = parentDir;
			this.execsToRun = execsToRun;
            this.pciProcess = pciProcess;
            this.zingFilePath = zingFilePath;
            this.testRoot = testRoot;
        }

        public static void PrintUsage()
        {
            Console.WriteLine(
                "USAGE: CheckP [-{0}: args] [-{1}: files] [-{2}: files]  [-{3}: descriptors]",
                ArgsPcOption,
                IncludePcOption,     
                DelOption,
                DescrOption  
            );

            Console.WriteLine();
            Console.WriteLine("-{0}\tA list of arguments", ArgsPcOption);
            Console.WriteLine("-{0}\tA list of files that should be included as output", IncludePcOption);
            Console.WriteLine("-{0}\tA list of files that should be deleted before running", DelOption);
            Console.WriteLine("-{0}\tDescriptions of this test", DescrOption);
        }

        void SplitPcArgs(IEnumerable<object> pcArgs, out List<string> loadArgs, out List<string> compileArgs, out List<string> testArgs)
        { 
            loadArgs = new List<string>();
            compileArgs = new List<string>();
            testArgs = new List<string>();
            foreach (string pcArg in pcArgs)
            {
                if (pcArg.EndsWith(".p"))
                {
                    loadArgs.Add(Path.GetFullPath(Path.Combine(activeDirectory, pcArg)));
                }
                else if (pcArg == "/dumpFormulaModel" || pcArg == "/printTypeInference")
                { 
                    loadArgs.Add(pcArg); 
                }
                else if (pcArg.StartsWith("/outputDir"))
                {
                    var splitArgs = pcArg.Split(':');
                    var fullPcArg = splitArgs[0] + ":" + Path.GetFullPath(Path.Combine(activeDirectory, splitArgs[1]));
                    loadArgs.Add(fullPcArg);
                    compileArgs.Add(fullPcArg);
                    testArgs.Add(fullPcArg);
                }
                else if (pcArg == "/liveness")
                {
                    testArgs.Add(pcArg);
                }
                else if (pcArg == "/shortFileNames" || pcArg == "/doNotErase")
                {
                    // ignore
                }
                else
                {
                    throw new Exception("Unknown argument to pc encountered");
                }
            }
        }

        public bool Check(string testfile)
        {
            var opts = new Options();
            if (!opts.LoadMore(activeDirectory, testfile))
            {
                return false;
            }

            return Check(opts);
        }

        private bool Check(Options opts)
        {
            bool result = true;
            bool isAdd = this.reset;

            bool isDel;
            Tuple<OptValueKind, object>[] delFiles;
            result = ValidateOption(opts, DelOption, true, 1, int.MaxValue, out isDel, out delFiles) && result;

            bool isDescr;
            Tuple<OptValueKind, object>[] descrs;
            result = ValidateOption(opts, DescrOption, true, 1, int.MaxValue, out isDescr, out descrs) && result;

            string[] unknownOpts;
            if (opts.TryGetOptionsBesides(AllOptions, out unknownOpts))
            {
                foreach (var uo in unknownOpts)
                {
                    Console.WriteLine("ERROR: -{0} is not a legal option", uo);
                }

                result = false;
            }

            if (!result)
            {
                Console.WriteLine();
                PrintUsage();
                return false;
            }

            if (isDescr)
            {
                var fullDescr = "";
                foreach (var v in descrs)
                {
                    fullDescr += v.Item2.ToString() + " ";
                }

                Description = fullDescr;
                Console.WriteLine("*********** Checking {0}***********", fullDescr);
            }

            if (isDel)
            {
                foreach (var df in delFiles)
                {
                    try
                    {
                        var dfi = new FileInfo(Path.Combine(activeDirectory, df.Item2.ToString()));
                        if (dfi.Exists)
                        {
                            Console.WriteLine("DEL: Deleted file {0}", dfi.FullName);
                            dfi.Attributes = FileAttributes.Normal;
                            dfi.Delete();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(
                            "Error deleting file {0} - {1}",
                            df.Item2,
                            e.Message);
                    }
                }
            }

            StreamWriter tmpWriter;
            if (!OpenTmpStream(out tmpWriter))
            {
                return false;
            }

            //debudding only?
            Console.WriteLine("Running test under {0} ...", activeDirectory);

            //If isAdd is true, remove old acceptor file
            //Note: this will break the logic of multiple acceptors;
            //if in the future multiple acceptors are needed, re-implement this feature, for example:
            //in adition to the "add" option, add option "reset" for CheckP;
            //testP.bat will also have two alternative options: "reset" and "add";
            //only delete acceptors for "reset" option, but not for "add" option

            const string acceptorFilePattern = "acc_0.txt";
            DirectoryInfo di = new DirectoryInfo(activeDirectory);
            if (isAdd)
            {
                foreach (var acci in di.EnumerateFiles(acceptorFilePattern))
                {
                    File.Delete(Path.Combine(activeDirectory, acci.FullName));
                }
            }

            //activeDirectory is "...\Prt", but runtime files are under
            //"..\\." (test directory)
            var temp = new DirectoryInfo(activeDirectory);
            string parentFolder = temp.Parent.FullName;
            var workDirectory = String.Concat(parentFolder, "\\");
            workDirectory = String.Concat(workDirectory, ".");

            bool isInclPc;
            Tuple<OptValueKind, object>[] includesPc;
            bool isArgsPc;
            Tuple<OptValueKind, object>[] pcArgs;
            bool isInclZinger;
            Tuple<OptValueKind, object>[] includesZinger;
            bool isArgsZinger;
            Tuple<OptValueKind, object>[] zingerArgs;
            bool isInclPrt;
            Tuple<OptValueKind, object>[] includesPrt;
            bool isArgsPrt;
            Tuple<OptValueKind, object>[] prtArgs;
            try
            {
                //Run the component of the P tool chain specified by the "activeDirectory":
                if (parentDir == "Pc")
                {
                    result = ValidateOption(opts, IncludePcOption, true, 1, int.MaxValue, out isInclPc, out includesPc) &&
                            result;
                    result = ValidateOption(opts, ArgsPcOption, true, 1, int.MaxValue, out isArgsPc, out pcArgs) && result;
                    tmpWriter.WriteLine("=================================");
                    tmpWriter.WriteLine("         Console output          ");
                    tmpWriter.WriteLine("=================================");
                    List<string> loadArgs, compileArgs, testArgs;
                    SplitPcArgs(pcArgs.Select(x => x.Item2), out loadArgs, out compileArgs, out testArgs);
                    pciProcess.Reset();
                    pciProcess.Run("load", loadArgs);
                    if (pciProcess.loadSucceeded)
                    {
                        pciProcess.Run("compile", compileArgs);
                        pciProcess.Run("test", testArgs);
                    }
                    tmpWriter.Write(pciProcess.outputString);
                    tmpWriter.Write(pciProcess.errorString);
                    if (pciProcess.loadSucceeded)
                    {
                        tmpWriter.WriteLine("EXIT: 0");
                    }
                    else
                    {
                        tmpWriter.WriteLine("EXIT: -1");
                    }
                }
                else if (parentDir == "Zing")
                {
                    result = ValidateOption(opts, IncludeZingerOption, true, 1, int.MaxValue, out isInclZinger, out includesZinger) && 
                             result;
                    result = ValidateOption(opts, ArgsZingerOption, true, 1, int.MaxValue, out isArgsZinger, out zingerArgs) &&
                             result;
                    //TODO: since Zinger returns "true" when *.dll file is missing, catch this case by explicitly 
                    //checking if files specified as  zinger arguments are present (unless Zinger is fixed
                    //and returns "false" for such errors).
                    //The error message should give a tip: "Make sure pc.exe was called and run successfully"
                    // zingerResult will be false only if zinger command line call didn't work;
                    // otherwise, it will be "true", even if Zinger's exit value is non-zero
                    // TODO: catch Zinger's exit code 7 (wrong parameters) and report it to cmd window
                    bool zingerResult = Run(tmpWriter, zingFilePath, zingerArgs);

                    //debug:
                    //Console.WriteLine("Zinger returned: {0}", zingerResult);

                    if (!zingerResult)
                    {
                        result = false;
                    }
                    else if (isInclZinger && !AppendIncludes(tmpWriter, includesZinger))
                    {
                        result = false;
                    }
                }
                else if (parentDir == "Prt")
                {
                    result =
                        ValidateOption(opts, IncludePrtOption, true, 1, int.MaxValue, out isInclPrt, out includesPrt) &&
                        result;
                    result = ValidateOption(opts, ArgsPrtOption, true, 1, int.MaxValue, out isArgsPrt, out prtArgs) &&
                             result;
                    //Compute "TesterDirectory" (Tst\PrtTester):
                    //path to ...PrtTester\Debug\x86\tester.exe (since that is the configuration that RunBuildTester builds).
                    string testerExeDir = Path.Combine(this.testRoot, "PrtTester\\Debug\\x86");
                    var testerExePath = Path.Combine(testerExeDir, "tester.exe");
                    var testerDirectory = Path.Combine(this.testRoot, "PrtTester");

                    //Remove previous runtime files from Tst\PrtTester:
                    File.Delete(Path.Combine(testerDirectory, RuntimeFile1));
                    File.Delete(Path.Combine(testerDirectory, RuntimeFile2));
                    File.Delete(Path.Combine(testerDirectory, RuntimeFile3));

                    //Copy current runtime files generated by Pc.exe to Tst\PrtTester:
                    File.Copy(
                        Path.Combine(workDirectory, RuntimeFile1),
                        Path.Combine(testerDirectory, RuntimeFile1)
                        );
                    File.Copy(
                        Path.Combine(workDirectory, RuntimeFile2),
                        Path.Combine(testerDirectory, RuntimeFile2)
                        );
                    File.Copy(
                        Path.Combine(workDirectory, RuntimeFile3),
                        Path.Combine(testerDirectory, RuntimeFile3)
                        );

                    //Build tester.exe for the updated runtime files.
                    var prtTesterProj = Path.Combine(this.testRoot, @"PrtTester\Tester.vcxproj");

                    //1. Define msbuildPath for msbuild.exe:
                    var msbuildPath = FindTool("MSBuild.exe");
                    if (msbuildPath == null)
                    {
                        Console.WriteLine("Error: msbuild.exe is not in your PATH.");
                        return false;
                    }

                    //2. Build Tester: "msbuildDir  .\PrtTester\Tester.vcxproj /p:Configuration=Debug /verbosity:quiet /nologo"
                    //Check that Tester.vcxproj exists under PrtTester:
                    if (!File.Exists(prtTesterProj))
                    {
                        Console.WriteLine("Error: Tester.vcxproj is not found under PrtTester\\");
                        return false;
                    }
                    //Checking that program.c, program.h and stubs.c have been copied into testerDirectory:
                    if (!File.Exists(Path.Combine(testerDirectory, "program.c")) ||
                        !File.Exists(Path.Combine(testerDirectory, "program.h")) ||
                        !File.Exists(Path.Combine(testerDirectory, "stubs.c")))
                    {
                        Console.WriteLine("Error: runtime file(s) are not found under PrtTester\\");
                        return false;
                    }

                    //Cleaning tester.exe:
                    bool buildRes = RunBuildTester(msbuildPath, prtTesterProj, true);
                    if (!buildRes)
                    {
                        Console.WriteLine("Error cleaning Tester project");
                        return false;
                    }
                    //Building tester.exe:
                    buildRes = RunBuildTester(msbuildPath, prtTesterProj, false);
                    if (!buildRes)
                    {
                        Console.WriteLine("Error building Tester project");
                        return false;
                    }

                    //Run tester.exe:
                    bool prtResult = Run(tmpWriter, testerExePath, prtArgs);
                    if (!prtResult)
                    {
                        result = false;
                    }
                    else if (isInclPrt && !AppendIncludes(tmpWriter, includesPrt))
                    {
                        result = false;
                    }
                }
                else
                {
                    Console.WriteLine("Unreachable: parentDir {0} has an invalid value:", parentDir);
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR running P tool: {0}", e.Message);
                return false;
            }

            if (!CloseTmpStream(tmpWriter))
            {
                result = false;
            }

            if (result && !CompareAcceptors(activeDirectory, isAdd))
            {
                File.Delete(Path.Combine(activeDirectory, LogFile));
                File.Copy(
                    Path.Combine(activeDirectory, TmpStreamFile),
                    Path.Combine(activeDirectory, LogFile));
                Console.WriteLine("LOGGED: Saved bad output to {0}",
                    Path.Combine(activeDirectory, LogFile));

                result = false;
            }

            if (!DeleteTmpFile())
            {
                result = false;
            }

            if (result)
            {
                Console.WriteLine("SUCCESS: Output matched");
            }

            return result;
        }

        private static string FindTool(string name)
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            string[] dirs = path.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string dir in dirs)
            {
                string toolPath = Path.Combine(dir, name);
                if (File.Exists(toolPath))
                {
                    return toolPath;
                }
            }
            return null;
        }

        private static bool ValidateOption(
            Options opts,
            string opt,
            bool isOptional,
            int nArgsMin,
            int nArgsMax,
            out bool isSet,
            out Tuple<OptValueKind, object>[] values)
        {
            isSet = opts.TryGetOption(opt, out values);
            if (!isSet && !isOptional)
            {
                Console.WriteLine("ERROR: -{0} option not provided", opt);
                return false;
            }
            else if (isSet && (values.Length < nArgsMin || values.Length > nArgsMax))
            {
                Console.WriteLine("ERROR: -{0} option has wrong number of arguments", opt);
                return false;
            }

            return true;
        }

        private bool OpenTmpStream(out StreamWriter wr)
        {
            wr = null;
            try
            {
                wr = new StreamWriter(Path.Combine(activeDirectory, TmpStreamFile));
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "ERROR: Could not open temporary file {0} - {1}",
                    TmpStreamFile,
                    e.Message);
                return false;
            }

            return true;
        }

        private static bool CloseTmpStream(StreamWriter wr)
        {
            try
            {
                wr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "ERROR: Could not close temporary file {0} - {1}",
                    TmpStreamFile,
                    e.Message);
                return false;
            }

            return true;
        }

        private bool DeleteTmpFile()
        {
            try
            {
                var fi = new FileInfo(Path.Combine(activeDirectory, TmpStreamFile));
                if (fi.Exists)
                {
                    fi.Delete();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "ERROR: Could not delete temporary file {0} - {1}",
                    TmpStreamFile,
                    e.Message);
                return false;
            }

            return true;
        }

        private bool AppendIncludes(StreamWriter outStream,
                                           Tuple<OptValueKind, object>[] includes)
        {
            foreach (var inc in includes)
            {
                outStream.WriteLine();
                outStream.WriteLine("=================================");
                outStream.WriteLine("{0}", inc.Item2.ToString());
                outStream.WriteLine("=================================");

                try
                {
                    using (var sr = new StreamReader(Path.Combine(activeDirectory, inc.Item2.ToString())))
                    {
                        while (!sr.EndOfStream)
                        {
                            outStream.WriteLine(sr.ReadLine());
                        }
                    }
                }
                catch (Exception e)
                {
                    //special case: testconfig.txt for Zing is intended for "failed" result, but zinger passes, hence, no .trace file generated -
                    //hence, nothing to append to the acceptor.
                    //This code relies on the trace file having an extension "trace".
                    if (e.Message.StartsWith("Could not find file") && (inc.Item2.ToString().EndsWith("trace")))
                    {
                        //Console.WriteLine("Zinger passes, no trace generated");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Could not include {0} - {1}", inc.Item2.ToString(), e.Message);
                        return false;
                    }
                    
                }
            }

            return true;
        }

        private bool CompareAcceptors(string accDir, bool add)
        {
            var tmpFile = Path.Combine(activeDirectory, TmpStreamFile);
            try
            {
                var di = new DirectoryInfo(Path.Combine(activeDirectory, accDir));
                if (!di.Exists)
                {
                    Console.WriteLine("ERROR: Acceptor directory {0} does not exist", accDir);
                    return false;
                }

                var hashSet = new HashSet<string>();
                foreach (var fi in di.EnumerateFiles(AccFiles))
                {
                    hashSet.Add(fi.Name);
                    if (!IsDifferent(fi.FullName, tmpFile))
                    {
                        return true;
                    }
                }

                if (add)
                {
                    var nextId = 0;
                    string name = "";
                    while (hashSet.Contains(name = string.Format("{0}_{1}{2}", AccPrefix, nextId, AccExt)))
                    {
                        ++nextId;
                    }

                    File.Copy(
                        Path.Combine(activeDirectory, TmpStreamFile),
                        Path.Combine(Path.Combine(activeDirectory, accDir), name));
                    return true;
                }
                else
                {
                    Console.WriteLine("ERROR: Output is not accepted");
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Could not compare acceptors - {0}", e.Message);
                return false;
            }
        }

        private bool Run(
            StreamWriter outStream,
            string exe,
            Tuple<OptValueKind, object>[] values)
        {
            var args = "";
            if (values != null)
            {
                foreach (var v in values)
                {
                    args += v.Item2.ToString() + " ";
                }
            }

            outStream.WriteLine("=================================");
            outStream.WriteLine("         Console output          ");
            outStream.WriteLine("=================================");

            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = activeDirectory;

                Uri combined = new Uri(new Uri(activeDirectory), exe);
                psi.FileName = combined.LocalPath;
                psi.Arguments = args.Trim();
                psi.CreateNoWindow = true;

                string outString = "";
                string errorString = "";
                var process = new Process();
                process.StartInfo = psi;
                process.OutputDataReceived += (s, e) => OutputReceived(ref outString, s, e);
                process.ErrorDataReceived += (s, e) => ErrorReceived(ref errorString, s, e);
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                outStream.Write(outString);
                outStream.Write(errorString);
                outStream.WriteLine("EXIT: {0}", process.ExitCode);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Failed to run command: {0}", e.Message);
                return false;
            }

            return true;
        }


        private bool RunBuildTester(string buildExe, string projectFile, bool clean)
        {
            try
            {
                var msbuildArgs = "";
                msbuildArgs += projectFile;
                msbuildArgs += clean ? @" /t:Clean " : @" /t:Build ";
                msbuildArgs += @"/p:Configuration=Debug ";
                msbuildArgs += @"/p:Platform=x86 ";
                msbuildArgs += @"/verbosity:quiet ";
                msbuildArgs += @"/nologo";
                //Console.WriteLine("msbuildArgs: {0}", msbuildArgs);
                ProcessStartInfo startInfo = new ProcessStartInfo(buildExe);
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = msbuildArgs;
                startInfo.UseShellExecute = false;
                var buildProcess = new Process();
                buildProcess.StartInfo = startInfo;
                buildProcess.Start();
                buildProcess.WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Build/Clean of Tester failed: {0}", e.Message);
                return false;
            }
            return true;
        }

        private static void OutputReceived(
            ref string outString,
            object sender,
            DataReceivedEventArgs e)
        {
            outString += string.Format("OUT: {0}\r\n", e.Data);
        }

        private static void ErrorReceived(
            ref string errorString,
            object sender,
            DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                errorString += string.Format("ERROR: {0}\r\n", e.Data);
            }
        }

        private static bool IsDifferent(string file1, string file2)
        {
            try
            {
                using (var sr1 = new StreamReader(file1))
                {
                    using (var sr2 = new StreamReader(file2))
                    {
                        while (true)
                        {
                            string line1 = sr1.ReadLine();
                            string line2 = sr2.ReadLine();
                            if (line1 != line2)
                            {
                                return true;
                            }

                            if (line1 == null && line2 == null)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            catch
            {
                return true;
            }
        }
    }
}
