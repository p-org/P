using System.Threading;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace CheckP
{
    using Microsoft.Formula.API;
    using Microsoft.Pc;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class Checker
    {
        private const int BufferSize = 1024;

        private const string IncludePcOption = "inc";
        private const string IncludeZingerOption = "inc";
        private const string IncludePrtOption = "inc";
        private const string DescrOption = "dsc";
        private const string LinkFileOption = "link";
        private const string ArgsPcOption = "arg";
        private const string ArgsZingerOption = "arg";
        private const string ArgsPrtOption = "arg";
        private const string DelOption = "del";

        private const string TmpStreamFile = "check-tmp.txt";
        private const string AccFiles = "acc*.txt";
        private const string AccPrefix = "acc";
        private const string AccExt = ".txt";
        private const string LogFile = "check-output.log";
        private const string buildLogFileName = "testerBuildLogFile.txt";
        private static readonly HashSet<string> TestDirectoryContents =
            new HashSet<string>(new string[] { ".gitignore", "tester.sln", "tester.vcxproj", "tester.c" });
        private static readonly string[] AllOptions = new string[]
        {
            IncludePcOption,
            IncludeZingerOption,
            IncludePrtOption,
            DescrOption,
            ArgsPcOption,
            ArgsZingerOption,
            ArgsPrtOption,
            DelOption,
            LinkFileOption
        };

        private string activeDirectory;
        private bool reset;
        private bool cooperative;
        //Pc, Prt or Zing:
        private string parentDir;
        private string execsToRun;
        private Compiler compiler;
        private string zingFilePath;
        private string testerExePath;
        private string testRoot;
        private string configuration;
        private string platform;

        public string Description
        {
            get;
            private set;
        }

        public Checker(string activeDirectory, string testRoot, bool reset, bool cooperative, string configuration, string platform, string execsToRun, string zingFilePath, Compiler compiler)
        {
            this.activeDirectory = activeDirectory;
            this.reset = reset;
            this.cooperative = cooperative;
            this.parentDir = Path.GetFileName(activeDirectory);
            this.execsToRun = execsToRun;
            this.compiler = compiler;
            this.zingFilePath = zingFilePath;
            this.testRoot = testRoot;
            this.configuration = configuration;
            this.platform = platform;
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

        void ParsePcArgs(IEnumerable<object> pcArgs, out string fileName, out bool liveness)
        {
            fileName = null;
            liveness = false;
            foreach (string pcArg in pcArgs)
            {
                if (pcArg.EndsWith(".p"))
                {
                    if (fileName != null)
                    {
                        throw new Exception("multiple input file names not supported");
                    }
                    fileName = Path.GetFullPath(Path.Combine(activeDirectory, pcArg));
                }
                else if (pcArg == "/liveness")
                {
                    liveness = true;
                }
            }
            if (fileName == null)
            {
                throw new Exception("no input file");
            }
        }

        public bool Check(string testfile)
        {
            var opts = new Options();
            opts.Variables.Add("configuration", this.configuration);
            opts.Variables.Add("platform", this.platform);
            opts.Variables.Add("testroot", this.testRoot);

            var binaries = Path.Combine(this.testRoot, @"..\bld\drops\" + this.configuration + "\\" + this.platform + @"\binaries\");
            opts.Variables.Add("testbinaries", binaries);

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
                    WriteError("ERROR: -{0} is not a legal option", uo);
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
                        WriteError(
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

            Console.WriteLine("Running test under {0} ...", activeDirectory);

            const string acceptorFilePattern = "acc_0.txt";
            DirectoryInfo di = new DirectoryInfo(activeDirectory);
            if (isAdd)
            {
                foreach (var acci in di.EnumerateFiles(acceptorFilePattern))
                {
                    File.Delete(Path.Combine(activeDirectory, acci.FullName));
                }
            }

            string workDirectory = new DirectoryInfo(activeDirectory).Parent.FullName;

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
            bool isLinkOption;
            Tuple<OptValueKind, object>[] linkFile;
            try
            {
                //Run the component of the P tool chain specified by the "activeDirectory":
                if (parentDir == "Pc")
                {
                    foreach (string fileName in Directory.EnumerateFiles(workDirectory))
                    {
                        if (Path.GetExtension(fileName) == ".c" ||
                            Path.GetExtension(fileName) == ".h" ||
                            Path.GetExtension(fileName) == ".4ml" ||
                            Path.GetExtension(fileName) == ".zing" ||
                            Path.GetExtension(fileName) == ".dll")
                        {
                            File.Delete(fileName);
                        }
                    }

                    result = ValidateOption(opts, IncludePcOption, true, 1, int.MaxValue, out isInclPc, out includesPc) &&
                            result;
                    result = ValidateOption(opts, ArgsPcOption, true, 1, int.MaxValue, out isArgsPc, out pcArgs) && result;
                    result = ValidateOption(opts, LinkFileOption, true, 1, int.MaxValue, out isLinkOption, out linkFile) && result;
                    if(isLinkOption && linkFile.Count() > 1)
                    {
                            throw new Exception("multiple link files are not supported");
                    }
                    tmpWriter.WriteLine("=================================");
                    tmpWriter.WriteLine("         Console output          ");
                    tmpWriter.WriteLine("=================================");
                    string inputFileName;
                    bool liveness;
                    ParsePcArgs(pcArgs.Select(x => x.Item2), out inputFileName, out liveness);

                    var compileArgs = new CommandLineOptions();
                    compileArgs.inputFileNames = new List<string>();
                    compileArgs.inputFileNames.Add(inputFileName);
                    compileArgs.compilerOutput = CompilerOutput.C;
                    compileArgs.shortFileNames = true;
                    compileArgs.outputDir = workDirectory;
                    compileArgs.shortFileNames = true;                    
                    compileArgs.reBuild = true;
                    var compilerOutput = new CompilerTestOutputStream(tmpWriter);

                    bool compileResult = false;

                    using (compiler.Profiler.Start("compile", inputFileName))
                    {
                        compileResult = compiler.Compile(compilerOutput, compileArgs);
                    }
                    if (compileResult)
                    {
                        compileArgs.inputFileNames.Clear();
                        compileArgs.inputFileNames.Add(inputFileName);
                        compileArgs.compilerOutput = CompilerOutput.CSharp;
                        compileArgs.reBuild = true;
                        if (liveness)
                        {
                            compileArgs.liveness = LivenessOption.Standard;
                        }

                        using (compiler.Profiler.Start("compile csharp", inputFileName))
                        {
                            compileResult = compiler.Compile(compilerOutput, compileArgs);
                        }
                    }

                    if (compileResult)
                    {
                        //For C code generation we can use link file
                        // link the *.4ml
                        compileArgs.inputFileNames.Clear();
                        string linkFileName = Path.ChangeExtension(inputFileName, ".4ml");
                        compileArgs.inputFileNames.Add(linkFileName);
                        compileArgs.reBuild = true;
                        if (isLinkOption)
                        {
                            var linkPFile = Path.GetFullPath(Path.Combine(activeDirectory, (string)linkFile[0].Item2));
                            compileArgs.inputFileNames.Add(linkPFile);
                        }

                        using (compiler.Profiler.Start("link", linkFileName))
                        {
                            compileResult = compiler.Link(compilerOutput, compileArgs);
                        }
                    }

                    if (compileResult)
                    {
                        // compile *.p again, this time with Zing option.
                        compileArgs.inputFileNames.Clear();
                        compileArgs.inputFileNames.Add(inputFileName);
                        compileArgs.compilerOutput = CompilerOutput.Zing;
                        compileArgs.reBuild = true;
                        if (liveness)
                        {
                            compileArgs.liveness = LivenessOption.Standard;
                        }
                            
                        using (compiler.Profiler.Start("compile zing", inputFileName))
                        {
                            compileResult = compiler.Compile(compilerOutput, compileArgs);
                        }
                    }

                    
                    if (compileResult)
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

                    string zingDllName = null;
                    foreach (var fileName in Directory.EnumerateFiles(workDirectory))
                    {
                        if (Path.GetExtension(fileName) == ".dll" && !fileName.Contains("output"))
                        {
                            zingDllName = Path.GetFullPath(fileName);
                            break;
                        }
                    }
                    if (zingDllName == null)
                    {
                        WriteError("Zinger input not found.");
                        return false;
                    }
                    Tuple<OptValueKind, object> zingerDefaultArg =
                            new Tuple<OptValueKind, object>(OptValueKind.String, zingDllName);
                    var lst = new List<Tuple<OptValueKind, object>>();
                    if (zingerArgs != null)
                    {
                        lst = zingerArgs.ToList();
                        lst.Add(zingerDefaultArg);
                        zingerArgs = lst.ToArray();
                    }
                    else
                    {
                        zingerArgs = new Tuple<OptValueKind, object>[1];
                        zingerArgs[0] = zingerDefaultArg;
                    }

                    bool zingerResult = false;
                    using (compiler.Profiler.Start("run zing", zingDllName))
                    {
                        zingerResult = Run(tmpWriter, zingFilePath, zingerArgs);
                    }
                    //debug:

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

                    // copy Tester.vcxproj and tester.c into this test directory (so we can run multiple tests in parallel).
                    CopyFiles(Path.Combine(this.testRoot, "PrtTester"), workDirectory);
                    string exePath = configuration + Path.DirectorySeparatorChar + platform;
                    string testerExeDir = Path.Combine(workDirectory, exePath);
                    
                    this.testerExePath = Path.Combine(testerExeDir, "tester.exe");

                    //Build tester.exe for the updated runtime files.
                    var prtTesterProj = Path.Combine(workDirectory, "Tester.vcxproj");

                    using (compiler.Profiler.Start("build prttester", workDirectory))
                    {
                        //1. Define msbuildPath for msbuild.exe:
                        var msbuildPath = FindTool("MSBuild.exe");
                        if (msbuildPath == null)
                        {
                            string programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                            if (string.IsNullOrEmpty(programFiles))
                            {
                                programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
                            }
                            msbuildPath = Path.Combine(programFiles, @"MSBuild\14.0\Bin\MSBuild.exe");
                            if (!File.Exists(msbuildPath))
                            {
                                WriteError("Error: msbuild.exe is not in your PATH.");
                                return false;
                            }
                        }

                        //2. Build Tester: "msbuildDir  .\PrtTester\Tester.vcxproj /p:Configuration=Debug /verbosity:quiet /nologo"
                        //Check that Tester.vcxproj exists under PrtTester:
                        if (!File.Exists(prtTesterProj))
                        {
                            WriteError("Error: Tester.vcxproj is not found");
                            return false;
                        }
                        //Checking that linker.c and linker.h have been copied into testerDirectory:
                        if (!File.Exists(Path.Combine(workDirectory, "linker.c")) ||
                            !File.Exists(Path.Combine(workDirectory, "linker.h")))
                        {
                            WriteError("Error: linker.c and linker.h are not found, did you run pc.exe ?");
                            return false;
                        }

                        //Cleaning tester.exe:
                        bool buildRes = RunBuildTester(msbuildPath, prtTesterProj, true);
                        if (!buildRes)
                        {
                            WriteError("Error cleaning Tester project");
                            return false;
                        }
                        //Building tester.exe:
                        buildRes = RunBuildTester(msbuildPath, prtTesterProj, false);
                        if (!buildRes)
                        {
                            WriteError("Error building Tester project");
                            return false;
                        }
                    }
                    bool prtResult = false;
                    using (compiler.Profiler.Start("run prttester", workDirectory))
                    {
                        //Run tester.exe:
                        prtResult = Run(tmpWriter, testerExePath, prtArgs);
                    }
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
                    WriteError("Invalid test directory {0}, expecting 'pc','prt' or 'zing'.", parentDir);
                    return false;
                }
            }
            catch (Exception e)
            {
                WriteError("ERROR running P tool: {0}", e.Message);
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

        private static void WriteError(string format, params object[] args)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
            Console.ForegroundColor = saved;
        }

        public class CompilerTestOutputStream : ICompilerOutput
        {
            TextWriter writer;

            public CompilerTestOutputStream(TextWriter writer)
            {
                this.writer = writer;
            }

            public void WriteMessage(string msg, SeverityKind severity)
            {
                this.writer.WriteLine("OUT: " + msg);
            }
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
                WriteError("ERROR: -{0} option not provided", opt);
                return false;
            }
            else if (isSet && (values.Length < nArgsMin || values.Length > nArgsMax))
            {
                WriteError("ERROR: -{0} option has wrong number of arguments", opt);
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
                WriteError(
                    "ERROR: Could not open temporary file {0} - {1}",
                    TmpStreamFile,
                    e.Message);
                return false;
            }

            return true;
        }


        public static void CopyFiles(string src, string target)
        {
            foreach (var file in Directory.GetFiles(src))
            {
                string name = Path.GetFileName(file);
                File.Copy(file, target + Path.DirectorySeparatorChar + name, true);
            }
        }

        public static void CloneSubtree(string src, string target)
        {
            Directory.CreateDirectory(target);
            CopyFiles(src, target);
            foreach (var dir in Directory.GetDirectories(src))
            {
                string name = Path.GetFileName(dir);
                string subDir = target + Path.DirectorySeparatorChar + name;
                CloneSubtree(dir, subDir);
            }
        }

        private static bool CloseTmpStream(StreamWriter wr)
        {
            try
            {
                wr.Close();
            }
            catch (Exception e)
            {
                WriteError(
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
                WriteError(
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
                        //WriteError("Zinger passes, no trace generated");
                        return true;
                    }
                    else
                    {
                        WriteError("ERROR: Could not include {0} - {1}", inc.Item2.ToString(), e.Message);
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
                    WriteError("ERROR: Acceptor directory {0} does not exist", accDir);
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
                    WriteError("ERROR: Output is not accepted");
                    return false;
                }
            }
            catch (Exception e)
            {
                WriteError("ERROR: Could not compare acceptors - {0}", e.Message);
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
            if (cooperative && exe == this.testerExePath)
            {
                args += " /cooperative";
            }

            outStream.WriteLine("=================================");
            outStream.WriteLine("         Console output          ");
            outStream.WriteLine("=================================");

            try
            {
                var psi = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = activeDirectory
                };

                Uri combined = new Uri(new Uri(activeDirectory), exe);
                psi.FileName = combined.LocalPath;
                psi.Arguments = args.Trim();
                //Debug:
                //Console.WriteLine("exe: {0}", exe);
                //Console.WriteLine("Run arguments: {0}", psi.Arguments);

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
                WriteError("ERROR: Failed to run command: {0}", e.Message);
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
                msbuildArgs += @"/p:Configuration=" + configuration + " ";
                msbuildArgs += @"/p:Platform=" + platform + " ";
                msbuildArgs += @"/nologo";
                //Console.WriteLine("msbuildArgs: {0}", msbuildArgs);
                string outString = "";
                string errorString = "";
                ProcessStartInfo startInfo = new ProcessStartInfo(buildExe)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = activeDirectory,
                    Arguments = msbuildArgs
                };
                var buildProcess = new Process();
                buildProcess.StartInfo = startInfo;
                buildProcess.OutputDataReceived += (s, e) => OutputReceived(ref outString, s, e);
                buildProcess.ErrorDataReceived += (s, e) => ErrorReceived(ref errorString, s, e);
                buildProcess.Start();
                buildProcess.BeginErrorReadLine();
                buildProcess.BeginOutputReadLine();
                buildProcess.WaitForExit();
                if (buildProcess.ExitCode != 0)
                {
                    string msg = "MSBuild of " + projectFile + " failed:\n";
                    if (!string.IsNullOrEmpty(errorString))
                    {
                        msg += "\n" + errorString;
                    }
                    else
                    {
                        msg += "\n" + outString;
                    }
                    throw new Exception(msg);
                }
            }
            catch (Exception e)
            {
                WriteError("ERROR: Build/Clean of Tester failed: {0}", e.Message);
                return false;
            }
            return true;
        }

        private static void OutputReceived(
            ref string outString,
            object sender,
            DataReceivedEventArgs e)
        {
            string line = string.Format("OUT: {0}", e.Data);
            Debug.WriteLine(line);
            outString += line;
            outString += Environment.NewLine;
        }

        private static void ErrorReceived(
            ref string errorString,
            object sender,
            DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                string line = string.Format("ERROR: {0}", e.Data);
                Debug.WriteLine(line);
                errorString += line;
                errorString += Environment.NewLine;
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
