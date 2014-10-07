namespace CheckP
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Checker
    {
        private const int BufferSize = 1024;

        private const string RunPcOption = "runPc";
        private const string RunZingerOption = "runZing";
        private const string RunPrtOption = "runPrt";
        private const string AcceptOption = "acc";
        private const string IncludePcOption = "incPc";
        private const string IncludeZingerOption = "incZing";
        private const string IncludePrtOption = "incPrt";
        private const string AddOption = "add";
        private const string IgnorePromptOption = "igp";
        private const string DescrOption = "dsc";
        private const string ArgsPcOption = "argPc";
        private const string ArgsZingerOption = "argZing";
        private const string ArgsPrtOption = "argPrt";
        private const string TestOption = "cfg";
        private const string DelOption = "del";

        private const string TmpStreamFile = "check-tmp.txt";
        private const string AccFiles = "acc*.txt";
        private const string AccPrefix = "acc";
        private const string AccExt = ".txt";
        private const string LogFile = "check-output.log";

        private static readonly string[] AllOptions = new string[]
        {
            RunPcOption,
            RunZingerOption,
            RunPrtOption,
            AcceptOption,
            IncludePcOption,
            IncludeZingerOption,
            IncludePrtOption,
            AddOption,
            IgnorePromptOption,
            DescrOption,
            ArgsPcOption,
            ArgsZingerOption,
            ArgsPrtOption,
            TestOption,
            DelOption
        };

        private string activeDirectory;

        public string Description
        {
            get;
            private set;
        }

        public Checker(string activeDirectory)
        {
            this.activeDirectory = activeDirectory;
        }

        public static void PrintUsage()
        {
            Console.WriteLine(
                "USAGE: Check -{0}: pc.exe -{1}: zinger.exe -{2}: prtMain.exe -{3}: dir [-{10}: args] [-{11}: args] [-{12}: args] [-{4}: files] [-{5}: files] [-{6}: files] [-{14}: files] [-{13}: file] [-{7}] [-{8}] [-{9}: descriptors]",
                RunPcOption,
                RunZingerOption,
                RunPrtOption,
                AcceptOption,
                IncludePcOption,
                IncludeZingerOption,
                IncludePrtOption,
                AddOption,
                IgnorePromptOption,
                DescrOption,
                ArgsPcOption,
                ArgsZingerOption,
                ArgsPrtOption,
                TestOption,
                DelOption
            );

            Console.WriteLine();
            Console.WriteLine("-{0}\tpc.exe to run", RunPcOption);
            Console.WriteLine("-{0}\tzinger.exe to run", RunZingerOption);
            Console.WriteLine("-{0}\tprtMain.exe to run", RunPrtOption);
            Console.WriteLine("-{0}\tA directory containing acceptable outputs", AcceptOption);
            Console.WriteLine("-{0}\tA list of arguments to pc.exe", ArgsPcOption);
            Console.WriteLine("-{0}\tA list of arguments to zinger.exe", ArgsZingerOption);
            //TODO: prtMain is a temp name for runtime exe file
            Console.WriteLine("-{0}\tA list of arguments to prtMain.exe", ArgsPrtOption);
            Console.WriteLine("-{0}\tA list of files that should be included as output for pc.exe", IncludePcOption);
            Console.WriteLine("-{0}\tA list of files that should be included as output for zinger.exe", IncludeZingerOption);
            Console.WriteLine("-{0}\tA list of files that should be included as output for prt.exe", IncludePrtOption);
            Console.WriteLine("-{0}\tA list of files that should be deleted before running", DelOption);
            Console.WriteLine("-{0}\tA test file with additional configuration", TestOption);
            Console.WriteLine("-{0}\tAdds the output of this run to set of acceptable outputs", AddOption);
            Console.WriteLine("-{0}\tIgnore output sent to the prompt by commands", IgnorePromptOption);
            Console.WriteLine("-{0}\tDescriptions of this test", DescrOption);
        }

        /// <summary>
        /// Runs check using command line arguments
        /// </summary>
        /// <returns></returns>
        public bool Check()
        {
            Options opts;
            int errPos;
            string cmdStr;
            if (!OptionParser.Parse(out opts, out errPos, out cmdStr))
            {
                Console.WriteLine("ERROR: Could not parse command line");
                Console.WriteLine("INPUT: {0}", cmdStr);
                Console.WriteLine("POS  : {0}^", errPos == 0 ? string.Empty : new string(' ', errPos));
                Console.WriteLine();
                PrintUsage();
                return false;
            }

            bool isTestSet, result = true;
            Tuple<OptValueKind, object>[] testFile;
            result = ValidateOption(opts, TestOption, true, 1, 1, out isTestSet, out testFile) && result;
            if (isTestSet)
            {
                result = opts.LoadMore(activeDirectory, (string)testFile[0].Item2) && result;
                if (result)
                {
                    activeDirectory = new FileInfo(Path.Combine(activeDirectory, (string)testFile[0].Item2)).DirectoryName;
                }
            }

            return result && Check(opts);
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
            bool isSet, result = true;
            Tuple<OptValueKind, object>[] exePc;
            result = ValidateOption(opts, RunPcOption, false, 1, 1, out isSet, out exePc) && result;

            //TODO: Do I need a separate isSetXX for each of pc, zinger, prt? 
            //Why was the same isSet used for for "exe" and "acc" options?
            bool isSetExeZinger;
            Tuple<OptValueKind, object>[] exeZinger;
            result = ValidateOption(opts, RunZingerOption, true, 1, 1, out isSetExeZinger, out exeZinger) && result;

            bool isSetExePrt;
            Tuple<OptValueKind, object>[] exePrt;
            result = ValidateOption(opts, RunPrtOption, true, 1, 1, out isSetExePrt, out exePrt) && result;

            Tuple<OptValueKind, object>[] accDir;
            result = ValidateOption(opts, AcceptOption, false, 1, 1, out isSet, out accDir) && result;

            bool isInclPc;
            Tuple<OptValueKind, object>[] includesPc;
            result = ValidateOption(opts, IncludePcOption, true, 1, int.MaxValue, out isInclPc, out includesPc) && result;

            bool isInclZinger;
            Tuple<OptValueKind, object>[] includesZinger;
            result = ValidateOption(opts, IncludeZingerOption, true, 1, int.MaxValue, out isInclZinger, out includesZinger) && result;

            bool isInclPrt;
            Tuple<OptValueKind, object>[] includesPrt;
            result = ValidateOption(opts, IncludePrtOption, true, 1, int.MaxValue, out isInclPrt, out includesPrt) && result;

            bool isArgsPc;
            Tuple<OptValueKind, object>[] pcArgs;
            result = ValidateOption(opts, ArgsPcOption, true, 1, int.MaxValue, out isArgsPc, out pcArgs) && result;

            bool isArgsZinger;
            Tuple<OptValueKind, object>[] zingerArgs;
            result = ValidateOption(opts, ArgsZingerOption, true, 1, int.MaxValue, out isArgsZinger, out zingerArgs) && result;

            bool isArgsPrt;
            Tuple<OptValueKind, object>[] prtArgs;
            result = ValidateOption(opts, ArgsPrtOption, true, 1, int.MaxValue, out isArgsPrt, out prtArgs) && result;

            bool isAdd;
            Tuple<OptValueKind, object>[] values;
            result = ValidateOption(opts, AddOption, true, 0, 0, out isAdd, out values) && result;

            bool isIgnPrmpt;
            result = ValidateOption(opts, IgnorePromptOption, true, 0, 0, out isIgnPrmpt, out values) && result;

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

            //Trying to run P tool chain until the next tool returns an error:
            bool pcResult = Run(tmpWriter, isIgnPrmpt, exePc[0].Item2.ToString(), pcArgs);
            //if (pcResult)
            //{
                
            //}
            if (!pcResult)
            {
                result = false;
            }
            else if (isInclPc && !AppendIncludes(tmpWriter, includesPc))
            {
                result = false;
            }
            else if (isSetExeZinger)
            {
                bool zingerResult = Run(tmpWriter, isIgnPrmpt, exeZinger[0].Item2.ToString(), zingerArgs);
                if (!zingerResult) { result = false; }
                else if (isInclZinger && !AppendIncludes(tmpWriter, includesZinger))
                {
                    result = false; 
                }
                else if (isSetExePrt)
                {
                    bool prtResult = Run(tmpWriter, isIgnPrmpt, exePrt[0].Item2.ToString(), prtArgs);
                    if (!prtResult) { result = false; }
                    else if (isInclPrt && !AppendIncludes(tmpWriter, includesPrt))
                    {
                        result = false; 
                    }
                }
            }

            if (!CloseTmpStream(tmpWriter))
            {
                result = false;
            }

            if (result && !CompareAcceptors(accDir[0].Item2.ToString(), isAdd))
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
                    Console.WriteLine("ERROR: Could not include {0} - {1}", inc.Item2.ToString(), e.Message);
                    return false;
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
            bool ignorePrompt,
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

            if (!ignorePrompt)
            {
                outStream.WriteLine("=================================");
                outStream.WriteLine("         Console output          ");
                outStream.WriteLine("=================================");
            }

            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = activeDirectory;
                psi.FileName = Path.Combine(activeDirectory, exe);
                psi.Arguments = args.Trim();
                psi.CreateNoWindow = true;

                var process = new Process();
                process.StartInfo = psi;
                process.OutputDataReceived += (s, e) => OutputReceived(outStream, ignorePrompt, s, e);
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                Console.WriteLine("EXIT: {0}", process.ExitCode);
                outStream.WriteLine("EXIT: {0}", process.ExitCode);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Failed to run command: {0}", e.Message);
                return false;
            }

            return true;
        }

        private static void OutputReceived(
            StreamWriter outStream,
            bool ignorePrompt,
            object sender,
            DataReceivedEventArgs e)
        {
            if (ignorePrompt)
            {
                Console.WriteLine("OUT: {0}", e.Data);
            }
            else
            {
                outStream.WriteLine("OUT: {0}", e.Data);
            }
        }

        private static bool IsDifferent(string file1, string file2)
        {
            int read1, read2;
            var buf1 = new char[BufferSize];
            var buf2 = new char[BufferSize];
            try
            {
                using (var sr1 = new StreamReader(file1))
                {
                    using (var sr2 = new StreamReader(file2))
                    {
                        while (true)
                        {
                            read1 = sr1.ReadBlock(buf1, 0, BufferSize);
                            read2 = sr2.ReadBlock(buf2, 0, BufferSize);
                            if (read1 != read2)
                            {
                                return true;
                            }

                            for (int i = 0; i < read1; ++i)
                            {
                                if (buf1[i] != buf2[i])
                                {
                                    return true;
                                }
                            }

                            if (read1 == 0)
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
