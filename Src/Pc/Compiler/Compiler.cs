namespace Microsoft.Pc
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Formula.API;
    using Microsoft.Formula.API.ASTQueries;
    using Microsoft.Formula.API.Nodes;
    using Microsoft.Formula.Common;
    using Microsoft.Formula.Compiler;
    using Microsoft.Pc.Domains;
    using System.Diagnostics;
    using Formula.Common.Terms;
#if DEBUG_DGML
    using VisualStudio.GraphModel;
#endif
    using System.Windows.Forms;

    public enum CompilerOutput { None, C0, C, Zing, CSharp, Link };

    public enum LivenessOption { None, Standard, Mace };

    public enum TopDecl { Event, EventSet, Interface, Module, Machine, Test, TypeDef, Enum, FunProto, MachineProto };
    public class TopDeclNames
    {
        public HashSet<string> eventNames;
        public HashSet<string> eventSetNames;
        public HashSet<string> moduleNames;
        public HashSet<string> testNames;
        public HashSet<string> typeNames;
        public HashSet<string> machineNames;
        public HashSet<string> interfaceNames;
        public HashSet<string> enumNames;
        public HashSet<string> machineProto;
        public HashSet<string> funProto;
        public HashSet<string> funNames;
        public TopDeclNames()
        {
            eventNames = new HashSet<string>();
            eventSetNames = new HashSet<string>();
            interfaceNames = new HashSet<string>();
            moduleNames = new HashSet<string>();
            machineNames = new HashSet<string>();
            testNames = new HashSet<string>();
            typeNames = new HashSet<string>();
            enumNames = new HashSet<string>();
            funProto = new HashSet<string>();
            machineProto = new HashSet<string>();
            funNames = new HashSet<string>();

        }

        public void Reset()
        {
            eventNames.Clear();
            eventSetNames.Clear();
            interfaceNames.Clear();
            moduleNames.Clear();
            machineNames.Clear();
            testNames.Clear();
            typeNames.Clear();
            funProto.Clear();
            machineProto.Clear();
            funNames.Clear();
        }

    }

    public class StandardOutput : ICompilerOutput
    {
        public void WriteMessage(string msg, SeverityKind severity)
        {
            switch (severity)
            {
                case SeverityKind.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case SeverityKind.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case SeverityKind.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }

    public class CompilerOutputStream : ICompilerOutput
    {
        TextWriter writer;

        public CompilerOutputStream(TextWriter writer)
        {
            this.writer = writer;
        }

        public void WriteMessage(string msg, SeverityKind severity)
        {
            this.writer.WriteLine(msg);
        }
    }

    public class Compiler
    {
        private const string PDomain = "P";
        private const string PLinkDomain = "PLink";
        private const string PLinkTransform = "PLink2C";
        private const string CDomain = "C";
        private const string ZingDomain = "Zing";
        private const string P2InfTypesTransform = "P2PWithInferredTypes";
        private const string P2CTransform = "P2CProgram";
        private const string AliasPrefix = "p_compiler__";
        private const string MsgPrefix = "msg:";
        private const string ErrorClassName = "error";
        private const int TypeErrorCode = 1;

        private Dictionary<string, string> ReservedModuleToLocation;
        private Dictionary<string, Tuple<AST<Program>, bool>> ManifestPrograms;

        void InitManifestPrograms()
        {
            ReservedModuleToLocation = new Dictionary<string, string>();
            ReservedModuleToLocation.Add(PDomain, "P.4ml");
            ReservedModuleToLocation.Add(PLinkDomain, "PLink.4ml");
            ReservedModuleToLocation.Add(CDomain, "C.4ml");
            ReservedModuleToLocation.Add(ZingDomain, "Zing.4ml");
            ReservedModuleToLocation.Add(P2InfTypesTransform, "PWithInferredTypes.4ml");
            ReservedModuleToLocation.Add(P2CTransform, "P2CProgram.4ml");

            ManifestPrograms = new Dictionary<string, Tuple<AST<Program>, bool>>();
            ManifestPrograms["Pc.Domains.P.4ml"] = Tuple.Create<AST<Program>, bool>(ParseManifestProgram("Pc.Domains.P.4ml", "P.4ml"), false);
            ManifestPrograms["Pc.Domains.PLink.4ml"] = Tuple.Create<AST<Program>, bool>(ParseManifestProgram("Pc.Domains.PLink.4ml", "PLink.4ml"), false);
            ManifestPrograms["Pc.Domains.C.4ml"] = Tuple.Create<AST<Program>, bool>(ParseManifestProgram("Pc.Domains.C.4ml", "C.4ml"), false);
            ManifestPrograms["Pc.Domains.Zing.4ml"] = Tuple.Create<AST<Program>, bool>(ParseManifestProgram("Pc.Domains.Zing.4ml", "Zing.4ml"), false);
            ManifestPrograms["Pc.Domains.PWithInferredTypes.4ml"] = Tuple.Create<AST<Program>, bool>(ParseManifestProgram("Pc.Domains.PWithInferredTypes.4ml", "PWithInferredTypes.4ml"), false);
            ManifestPrograms["Pc.Domains.P2CProgram.4ml"] = Tuple.Create<AST<Program>, bool>(ParseManifestProgram("Pc.Domains.P2CProgram.4ml", "P2CProgram.4ml"), false);
        }

        SortedSet<Flag> errors;

        public ICompilerOutput Log { get; set; }

        public Env CompilerEnv
        {
            get;
            private set;
        }

        public CommandLineOptions Options
        {
            get;
            set;
        }

        public Compiler(bool shortFileNames)
        {
            InitManifestPrograms();
            EnvParams envParams = null;
            if (shortFileNames)
            {
                envParams = new EnvParams(new Tuple<EnvParamKind, object>(EnvParamKind.Msgs_SuppressPaths, true));
            }
            CompilerEnv = new Env(envParams);
        }

        public bool Compile(ICompilerOutput log, CommandLineOptions options)
        {
            if (options.profile)
            {
                PerfTimer.ConsoleOutput = true;
            }
            options.eraseModel = options.compilerOutput != CompilerOutput.C0;
            this.errors = new SortedSet<Flag>(default(FlagSorter));
            this.Log = log;
            this.Options = options;
            foreach (var inputFileName in options.inputFileNames)
            {
                var result = InternalCompile(inputFileName);
                foreach (var f in errors)
                {
                    PrintFlag(f);
                }
                if (!result)
                {
                    Log.WriteMessage("Compilation failed", SeverityKind.Error);
                    return false;
                }
            }
            return true;
        }

        private void AddFlag(Flag f)
        {
            if (errors.Contains(f))
            {
                return;
            }
            errors.Add(f);
        }

        private void PrintFlag(Flag f)
        {
            Log.WriteMessage(FormatError(f), f.Severity);
        }

        public string FormatError(Flag f)
        {
            string programName = "?";
            if (f.ProgramName != null)
            {
                bool shortFileNames = Options.shortFileNames;
                if (shortFileNames)
                {
                    var envParams = new EnvParams(
                        new Tuple<EnvParamKind, object>(EnvParamKind.Msgs_SuppressPaths, true));
                    programName = f.ProgramName.ToString(envParams);
                }
                else
                {
                    programName = (f.ProgramName.Uri.IsFile ? f.ProgramName.Uri.LocalPath : f.ProgramName.ToString());
                }
            }
            // for regression test compatibility reasons we do not include the error number when running regression tests.
            if (Options.eraseModel)
            {
                return
                  // this format causes VS to put the errors in the error list window.
                  string.Format("{0} ({1}, {2}): {3}",
                  programName,
                  f.Span.StartLine,
                  f.Span.StartCol,
                  f.Message);
            }
            else
            {
                string errorNumber = "PC1001"; // todo: invent meaningful error numbers to go with P documentation...
                return
                  // this format causes VS to put the errors in the error list window.
                  string.Format("{0}({1},{2},{3},{4}): error {5}: {6}",
                  programName,
                  f.Span.StartLine,
                  f.Span.StartCol,
                  f.Span.EndLine,
                  f.Span.EndCol,
                  errorNumber,
                  f.Message);
            }

        }

        public bool ParseProgram(string inputFileName, out PProgram parsedProgram, out ProgramName RootProgramName)
        {
            parsedProgram = new PProgram();
            using (new PerfTimer("Compiler parsing " + Path.GetFileName(inputFileName)))
            {
                try
                {
                    RootProgramName = new ProgramName(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, inputFileName)));
                }
                catch (Exception e)
                {
                    AddFlag(
                        new Flag(
                            SeverityKind.Error,
                            default(Span),
                            Constants.BadFile.ToString(string.Format("{0} : {1}", inputFileName, e.Message)),
                            Constants.BadFile.Code));
                    RootProgramName = null;
                    return false;
                }

                TopDeclNames topDeclNames = new TopDeclNames();
                Dictionary<string, ProgramName> SeenFileNames = new Dictionary<string, ProgramName>(StringComparer.OrdinalIgnoreCase);
                Queue<string> parserWorkQueue = new Queue<string>();
                var RootFileName = RootProgramName.ToString();
                SeenFileNames[RootFileName] = RootProgramName;
                parserWorkQueue.Enqueue(RootFileName);
                while (parserWorkQueue.Count > 0)
                {
                    List<string> includedFileNames;
                    List<Flag> parserFlags;
                    string currFileName = parserWorkQueue.Dequeue();
                    Debug.WriteLine("Loading " + currFileName);
                    var parser = new Parser.PParser();
                    var result = parser.ParseFile(SeenFileNames[currFileName], Options, topDeclNames, parsedProgram, out parserFlags, out includedFileNames);
                    foreach (Flag f in parserFlags)
                    {
                        AddFlag(f);
                    }
                    if (!result)
                    {
                        RootProgramName = null;
                        return false;
                    }

                    string currDirectoryName = Path.GetDirectoryName(Path.GetFullPath(currFileName));
                    foreach (var fileName in includedFileNames)
                    {
                        string fullFileName = Path.GetFullPath(Path.Combine(currDirectoryName, fileName));
                        ProgramName programName;
                        if (SeenFileNames.ContainsKey(fullFileName)) continue;
                        try
                        {
                            programName = new ProgramName(fullFileName);
                        }
                        catch (Exception e)
                        {
                            AddFlag(
                                new Flag(
                                    SeverityKind.Error,
                                    default(Span),
                                    Constants.BadFile.ToString(string.Format("{0} : {1}", fullFileName, e.Message)),
                                    Constants.BadFile.Code));
                            RootProgramName = null;
                            return false;
                        }
                        SeenFileNames[fullFileName] = programName;
                        parserWorkQueue.Enqueue(fullFileName);
                    }
                }
            }
            return true;
        }

        void InstallProgram(string inputFileName, PProgram parsedProgram, ProgramName RootProgramName, out AST<Model> RootModel)
        {
            using (new PerfTimer("Compiler installing " + Path.GetFileName(inputFileName)))
            {
                //// Step 0. Load P.4ml.
                LoadManifestProgram("Pc.Domains.P.4ml");

                //// Step 1. Serialize the parsed object graph into a Formula model and install it. Should not fail.
                AST<Model> rootModel = null;
                var mkModelResult = Factory.Instance.MkModel(
                    MkSafeModuleName(RootProgramName.ToString()),
                    PDomain,
                    parsedProgram.Terms,
                    out rootModel,
                    null,
                    MkReservedModuleLocation(PDomain),
                    ComposeKind.None);
                Contract.Assert(mkModelResult);
                RootModel = rootModel;

                InstallResult instResult;
                AST<Program> modelProgram = MkProgWithSettings(RootProgramName, new KeyValuePair<string, object>(Configuration.Proofs_KeepLineNumbersSetting, "TRUE"));

                // CompilerEnv only expects one call to Install at a time.
                bool progressed = CompilerEnv.Install(Factory.Instance.AddModule(modelProgram, rootModel), out instResult);
                Contract.Assert(progressed && instResult.Succeeded, GetFirstMessage(from t in instResult.Flags select t.Item2));

                if (Options.outputFormula)
                {
                    string outputDirName = Options.outputDir == null ? Environment.CurrentDirectory : Options.outputDir;
                    StreamWriter wr = new StreamWriter(File.Create(Path.Combine(outputDirName, "output.4ml")));
                    rootModel.Print(wr);
                    wr.Close();
                }
            }
        }

        void UninstallProgram(ProgramName RootProgramName)
        {
            InstallResult uninstallResult;
            var uninstallDidStart = CompilerEnv.Uninstall(new ProgramName[] { RootProgramName }, out uninstallResult);
            Contract.Assert(uninstallDidStart && uninstallResult.Succeeded);
        }

        bool InternalCompile(string inputFileName)
        {
            PProgram parsedProgram;
            ProgramName RootProgramName;
            AST<Model> RootModel;

            if (!ParseProgram(inputFileName, out parsedProgram, out RootProgramName))
            {
                return false;
            }

            InstallProgram(inputFileName, parsedProgram, RootProgramName, out RootModel);
            
            if (!Check(RootProgramName, RootModel.Node.Name))
            {
                UninstallProgram(RootProgramName);
                return false;
            }
                        
            if (Options.compilerOutput == CompilerOutput.None)
            {
                UninstallProgram(RootProgramName);
                return true;
            }

            bool rc = ((Options.compilerOutput == CompilerOutput.C0 || Options.compilerOutput == CompilerOutput.C) ? GenerateC(RootProgramName, RootModel) : true) && 
                      (Options.compilerOutput == CompilerOutput.Zing ? GenerateZing(RootProgramName, RootModel) : true) && 
                      (Options.compilerOutput == CompilerOutput.CSharp ? GenerateCSharp(RootProgramName, RootModel) : true);
            UninstallProgram(RootProgramName);
            return rc;
        }

        public static string GetFirstMessage(IEnumerable<Flag> flags)
        {
            Flag first = flags.FirstOrDefault();
            if (first != null)
            {
                return first.Message;
            }
            return "";
        }

        public bool GenerateCSharp(ProgramName RootProgramName, AST<Model> RootModel)
        {
            ProgramName RootProgramNameWithTypes;
            AST<Model> RootModelWithTypes;
            using (new PerfTimer("Compiler generating model with types " + Path.GetFileName(RootModel.Node.Name)))
            {
                if (!CreateRootModelWithTypes(RootProgramName, RootModel, out RootProgramNameWithTypes, out RootModelWithTypes))
                {
                    return false;
                }
            }

            string RootFileName = RootProgramName.ToString();
            string fileName = Path.GetFileNameWithoutExtension(RootFileName);
            string csharpFileName = fileName + ".cs";
            string dllFileName = fileName + ".dll";
            string outputDirName = Options.outputDir == null ? Environment.CurrentDirectory : Options.outputDir;

            using (new PerfTimer("Generating CSharp"))
            {
                var pToCSharp = new PToCSharp(this, RootModel, RootModelWithTypes);
                var success = pToCSharp.GenerateCSharp(csharpFileName);
                UninstallProgram(RootProgramNameWithTypes);
                return success;
            }
        }

        public bool GenerateZing(ProgramName RootProgramName, AST<Model> RootModel)
        {
            ProgramName RootProgramNameWithTypes;
            AST<Model> RootModelWithTypes;
            using (new PerfTimer("Compiler generating model with types " + Path.GetFileName(RootModel.Node.Name)))
            {
                if (!CreateRootModelWithTypes(RootProgramName, RootModel, out RootProgramNameWithTypes, out RootModelWithTypes))
                {
                    return false;
                }
            }

            string RootFileName = RootProgramName.ToString();
            AST<Model> zingModel;
            string fileName = Path.GetFileNameWithoutExtension(RootFileName);
            string zingFileName = fileName + ".zing";
            string dllFileName = fileName + ".dll";
            string outputDirName = Options.outputDir == null ? Environment.CurrentDirectory : Options.outputDir;

            using (new PerfTimer("Generating Zing"))
            {
                LoadManifestProgram("Pc.Domains.Zing.4ml");
                zingModel = MkZingOutputModel();
                var pToZing = new PToZing(this, RootModel, RootModelWithTypes);
                bool success = pToZing.GenerateZing(zingFileName, ref zingModel);
                UninstallProgram(RootProgramNameWithTypes);
                if (!success)
                {
                    return false;
                }
            }

            if (!PrintZingFile(zingModel, CompilerEnv, outputDirName))
                return false;

            System.Diagnostics.Process zcProcess = null;

            using (new PerfTimer("Compiling Zing"))
            {
                var binPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
                string zingCompiler = Path.Combine(binPath.FullName, "zc.exe");
                if (!File.Exists(zingCompiler))
                {
                    Log.WriteMessage("Cannot find Zc.exe, did you build it ?", SeverityKind.Error);
                    return false;
                }
                var zcProcessInfo = new System.Diagnostics.ProcessStartInfo(zingCompiler);
                string zingFileNameFull = Path.Combine(outputDirName, zingFileName);
                zcProcessInfo.Arguments = string.Format("/nowarn:292 \"/out:{0}\\{1}\" \"{2}\"", outputDirName, dllFileName, zingFileNameFull);
                zcProcessInfo.UseShellExecute = false;
                zcProcessInfo.CreateNoWindow = true;
                zcProcessInfo.RedirectStandardOutput = true;
                Log.WriteMessage(string.Format("Compiling {0} to {1} ...", zingFileName, dllFileName), SeverityKind.Info);
                zcProcess = System.Diagnostics.Process.Start(zcProcessInfo);
                zcProcess.WaitForExit();
            }

            if (zcProcess.ExitCode != 0)
            {
                Log.WriteMessage("Zc failed to compile the generated code", SeverityKind.Error);
                Log.WriteMessage(zcProcess.StandardOutput.ReadToEnd(), SeverityKind.Error);
                return false;
            }
            return true;
        }

        private bool PrintZingFile(AST<Model> m, Env env, string outputDirName)
        {
            var progName = new ProgramName(Path.Combine(outputDirName, m.Node.Name + "_ZingModel.4ml"));
            var zingProgram = Factory.Instance.MkProgram(progName);
            //// Set the renderer of the Zing program so terms can be converted to text.
            var zingProgramConfig = (AST<Config>)zingProgram.FindAny(new NodePred[]
                {
                    NodePredFactory.Instance.MkPredicate(NodeKind.Program),
                    NodePredFactory.Instance.MkPredicate(NodeKind.Config)
                });
            Contract.Assert(zingProgramConfig != null);
            var binPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
            zingProgramConfig = Factory.Instance.AddSetting(
                zingProgramConfig,
                Factory.Instance.MkId(Configuration.Parse_ActiveRenderSetting),
                Factory.Instance.MkCnst(typeof(ZingParser.Parser).Name + " at " + Path.Combine(binPath.FullName, "ZingParser.dll")));
            zingProgram = (AST<Program>)Factory.Instance.ToAST(zingProgramConfig.Root);
            zingProgram = Factory.Instance.AddModule(zingProgram, m);

            //var sw = new StreamWriter("out.4ml");
            //zingProgram.Print(sw);
            //sw.Flush();

            //// Install and render the program.
            InstallResult instResult;
            Task<RenderResult> renderTask;
            var didStart = CompilerEnv.Install(zingProgram, out instResult);
            Contract.Assert(didStart);
            PrintResult(instResult);
            if (!instResult.Succeeded)
                return false;
            didStart = CompilerEnv.Render(progName, m.Node.Name, out renderTask);
            Contract.Assert(didStart);
            renderTask.Wait();
            Contract.Assert(renderTask.Result.Succeeded);
            var rendered = renderTask.Result.Module;

            var fileQuery = new NodePred[]
            {
                NodePredFactory.Instance.Star,
                NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact),
                NodePredFactory.Instance.MkPredicate(NodeKind.FuncTerm) &
                NodePredFactory.Instance.MkNamePredicate("File")
            };

            var success = true;
            rendered.FindAll(
                fileQuery,
                (p, n) =>
                {
                    success = PrintZingFile(n, outputDirName) && success;
                });

            InstallResult uninstallResult;
            var uninstallDidStart = CompilerEnv.Uninstall(new ProgramName[] { progName }, out uninstallResult);
            Contract.Assert(uninstallDidStart && uninstallResult.Succeeded);

            return success;
        }

        private bool PrintZingFile(Node n, string outputDirName)
        {
            var file = (FuncTerm)n;
            string fileName;
            Quote fileBody;
            using (var it = file.Args.GetEnumerator())
            {
                it.MoveNext();
                fileName = ((Cnst)it.Current).GetStringValue();
                it.MoveNext();
                fileBody = (Quote)it.Current;
            }
            Log.WriteMessage(string.Format("Writing {0} ...", fileName), SeverityKind.Info);

            try
            {
                var fullPath = Path.Combine(outputDirName, fileName);
                using (var sw = new System.IO.StreamWriter(fullPath))
                {
                    foreach (var c in fileBody.Contents)
                    {
                        Factory.Instance.ToAST(c).Print(sw);
                    }
                    try
                    {
                        var asm = Assembly.GetExecutingAssembly();
                        using (var sr = new StreamReader(asm.GetManifestResourceStream("Pc.Zing.Prt.zing")))
                        {
                            sw.Write(sr.ReadToEnd());
                        }
                        using (var sr = new StreamReader(asm.GetManifestResourceStream("Pc.Zing.PrtTypes.zing")))
                        {
                            sw.Write(sr.ReadToEnd());
                        }
                        using (var sr = new StreamReader(asm.GetManifestResourceStream("Pc.Zing.PrtValues.zing")))
                        {
                            sw.Write(sr.ReadToEnd());
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Unable to load resources: " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Log.WriteMessage(string.Format("Could not save file {0} - {1}", fileName, e.Message), SeverityKind.Error);
                return false;
            }

            return true;
        }

        private void PrintResult(InstallResult result)
        {
            foreach (var f in result.Flags)
            {
                if (f.Item2.Severity != SeverityKind.Error)
                {
                    continue;
                }
                Log.WriteMessage(string.Format(
                    "{0} ({1}, {2}): {3} - {4}",
                    f.Item1.Node.Name,
                    f.Item2.Span.StartLine,
                    f.Item2.Span.StartCol,
                    f.Item2.Severity,
                    f.Item2.Message), SeverityKind.Error);
            }
        }

        private bool CreateRootModelWithTypes(ProgramName RootProgramName, AST<Model> RootModel, out ProgramName RootProgramNameWithTypes, out AST<Model> RootModelWithTypes)
        {
            LoadManifestProgram("Pc.Domains.PWithInferredTypes.4ml");
            var transApply = Factory.Instance.MkModApply(Factory.Instance.MkModRef(P2InfTypesTransform, null, MkReservedModuleLocation(P2InfTypesTransform)));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkModRef(RootModel.Node.Name, null, RootProgramName.ToString()));
            var RootModuleWithTypes = RootModel.Node.Name + "_WithTypes";
            var transStep = Factory.Instance.AddLhs(Factory.Instance.MkStep(transApply), Factory.Instance.MkId(RootModuleWithTypes));
            Task<ApplyResult> apply;
            Formula.Common.Rules.ExecuterStatistics stats;
            List<Flag> applyFlags;
            CompilerEnv.Apply(transStep, false, false, out applyFlags, out apply, out stats);
            apply.RunSynchronously();
            RootProgramNameWithTypes = new ProgramName(Path.Combine(Environment.CurrentDirectory, RootModel.Node.Name + "_WithTypes.4ml"));
            var extractTask = apply.Result.GetOutputModel(
                RootModuleWithTypes,
                RootProgramNameWithTypes,
                null);
            extractTask.Wait();
            RootModelWithTypes = (AST<Model>)extractTask.Result.FindAny(
                new NodePred[] { NodePredFactory.Instance.MkPredicate(NodeKind.Program), NodePredFactory.Instance.MkPredicate(NodeKind.Model) });
            Contract.Assert(RootModelWithTypes != null);
            InstallResult instResult;
            AST<Program> modelProgram = MkProgWithSettings(RootProgramNameWithTypes, new KeyValuePair<string, object>(Configuration.Proofs_KeepLineNumbersSetting, "TRUE"));
            bool progressed = CompilerEnv.Install(Factory.Instance.AddModule(modelProgram, RootModelWithTypes), out instResult);
            Contract.Assert(progressed && instResult.Succeeded, GetFirstMessage(from t in instResult.Flags select t.Item2));
            return true;
        }

        /// <summary>
        /// Run static analysis on the program.
        /// </summary>
        private bool Check(ProgramName RootProgramName, string inputModule)
        {
            Task<QueryResult> task;

            using (new PerfTimer("Compiler analyzing " + Path.GetFileName(inputModule)))
            {
                //// Run static analysis on input program.
                List<Flag> queryFlags;
                Formula.Common.Rules.ExecuterStatistics stats;

                var canStart = CompilerEnv.Query(
                    RootProgramName,
                    inputModule,
                    new AST<Body>[] { Factory.Instance.AddConjunct(Factory.Instance.MkBody(), Factory.Instance.MkFind(null, Factory.Instance.MkId(inputModule + ".requires"))) },
                    true,
                    false,
                    out queryFlags,
                    out task,
                    out stats);
                Contract.Assert(canStart);
                if (canStart)
                {
                    foreach (Flag f in queryFlags)
                    {
                        AddFlag(f);
                    }
                    task.RunSynchronously();
                }
                else
                {
                    MessageBox.Show("Debug me !!!");
                    throw new Exception("Compiler.Query cannot start, is another compile running in parallel?");
                }
            }

            // Enumerate typing errors
            using (new PerfTimer("Compiler error reporting " + Path.GetFileName(inputModule)))
            {
                AddErrors(RootProgramName, task.Result, "DupNmdSubE(_, _, _, _)", 1);
                AddErrors(RootProgramName, task.Result, "PurityError(_, _)", 1);
                AddErrors(RootProgramName, task.Result, "SpecError(_, _)", 1);
                AddErrors(RootProgramName, task.Result, "LValueError(_, _)", 1);
                AddErrors(RootProgramName, task.Result, "BadLabelError(_)", 0);
                AddErrors(RootProgramName, task.Result, "PayloadError(_)", 0);
                AddErrors(RootProgramName, task.Result, "TypeDefError(_)", 0);
                AddErrors(RootProgramName, task.Result, "FunRetError(_)", 0);

                AddErrors(RootProgramName, task.Result, "FunDeclQualifierError(_, _)", 1);
                AddErrors(RootProgramName, task.Result, "FunCallQualifierError(_, _, _)", 2);
                AddErrors(RootProgramName, task.Result, "SendQualifierError(_, _)", 1);
                AddErrors(RootProgramName, task.Result, "UnavailableVarAccessError(_, _, _)", 1);
                AddErrors(RootProgramName, task.Result, "UnavailableParameterError(_, _)", 0);

                //// Enumerate structural errors
                AddErrors(RootProgramName, task.Result, "OneDeclError(_)", 0);
                AddErrors(RootProgramName, task.Result, "TwoDeclError(_, _)", 1);
                AddErrors(RootProgramName, task.Result, "DeclFunError(_, _)", 1);
                AddErrors(RootProgramName, task.Result, "ExportInterfaceError(_)", 0);

                // this one is slow, so we do it last.
                AddErrors(RootProgramName, task.Result, "TypeOf(_, _, ERROR)", 1);

                if (Options.printTypeInference)
                {
                    AddTerms(RootProgramName, task.Result, "TypeOf(_, _, _)", SeverityKind.Info, 0, "inferred type: ", 1, 2);
                }
            }
            return task.Result.Conclusion == LiftedBool.True;
        }

        private void AddErrors(ProgramName RootProgramName,  QueryResult result, string errorPattern, int locationIndex = -1)
        {
            List<Flag> queryFlags;
            foreach (var p in result.EnumerateProofs(errorPattern, out queryFlags, 1))
            {
                if (!p.HasRuleClass(ErrorClassName))
                {
                    continue;
                }
#if DEBUG_DGML

                graph = new Graph();
                DumpTermGraph(graph, p.Conclusion);
#endif
                var errorMsg = GetMessageFromProof(p);
                if (locationIndex >= 0)
                {
                    foreach (var loc in p.ComputeLocators())
                    {
                        var exprLoc = loc[locationIndex];
                        AddFlag(new Flag(
                            SeverityKind.Error,
                            exprLoc.Span,
                            errorMsg,
                            TypeErrorCode,
                            exprLoc.Span.Program));
                    }
                }
                else
                {
                    AddFlag(new Flag(
                        SeverityKind.Error,
                        default(Span),
                        errorMsg,
                        TypeErrorCode,
                        RootProgramName));
                }
            }

            foreach (var f in queryFlags)
            {
                AddFlag(f);
            }
        }

#if DEBUG_DGML
        Graph graph;

        private void DumpTermGraph(Graph graph, Term term)
        {
            this.graph = graph;
            GraphNode node = graph.Nodes.GetOrCreate(term.GetHashCode().ToString(), term.Symbol.PrintableName, null);
            Walk(term, node);
            graph.Save(@"d:\temp\terms.dgml");
        }

        private void Walk(Term term, GraphNode node)
        {
            foreach (var arg in term.Args)
            {
                GraphNode argNode = graph.Nodes.GetOrCreate(arg.GetHashCode().ToString(), arg.Symbol.PrintableName, null);
                graph.Links.GetOrCreate(node, argNode);
                Walk(arg, argNode);
            }
        }
#endif

        private void AddTerms(
            ProgramName RootProgramName,
            QueryResult result,
            string termPattern,
            SeverityKind severity,
            int msgCode,
            string msgPrefix,
            int locationIndex = -1,
            int printIndex = -1)
        {
            List<Flag> queryFlags;
            foreach (var p in result.EnumerateProofs(termPattern, out queryFlags, 1))
            {
                if (locationIndex >= 0)
                {
                    foreach (var loc in p.ComputeLocators())
                    {
                        var sw = new System.IO.StringWriter();
                        sw.Write(msgPrefix);
                        sw.Write(" ");
                        if (printIndex < 0)
                        {
                            p.Conclusion.PrintTerm(sw);
                        }
                        else
                        {
                            p.Conclusion.Args[printIndex].PrintTerm(sw);
                        }

                        var exprLoc = loc[locationIndex];
                        AddFlag(new Flag(
                            severity,
                            exprLoc.Span,
                            sw.ToString(),
                            msgCode,
                            exprLoc.Span.Program));
                    }
                }
                else
                {
                    var sw = new System.IO.StringWriter();
                    sw.Write(msgPrefix);
                    sw.Write(" ");
                    if (printIndex < 0)
                    {
                        p.Conclusion.PrintTerm(sw);
                    }
                    else
                    {
                        p.Conclusion.Args[printIndex].PrintTerm(sw);
                    }

                    AddFlag(new Flag(
                        severity,
                        default(Span),
                        sw.ToString(),
                        msgCode,
                        RootProgramName));
                }
            }

            foreach (var f in queryFlags)
            {
                AddFlag(f);
            }
        }

        private static string GetMessageFromProof(ProofTree p)
        {
            foreach (var cls in p.RuleClasses)
            {
                if (cls.StartsWith(MsgPrefix))
                {
                    return cls.Substring(MsgPrefix.Length).Trim();

                }
            }

            return "Unknown error";
        }

        private static AST<Program> MkProgWithSettings(
            ProgramName name,
            params KeyValuePair<string, object>[] settings)
        {
            var prog = Factory.Instance.MkProgram(name);

            var configQuery = new NodePred[]
            {
                NodePredFactory.Instance.MkPredicate(NodeKind.Program),
                NodePredFactory.Instance.MkPredicate(NodeKind.Config)
            };

            var config = (AST<Config>)prog.FindAny(configQuery);
            Contract.Assert(config != null);
            if (settings != null)
            {
                foreach (var kv in settings)
                {
                    if (kv.Value is string)
                    {
                        config = Factory.Instance.AddSetting(config, Factory.Instance.MkId(kv.Key), Factory.Instance.MkCnst((string)kv.Value));
                    }
                    else if (kv.Value is int)
                    {
                        config = Factory.Instance.AddSetting(config, Factory.Instance.MkId(kv.Key), Factory.Instance.MkCnst((int)kv.Value));
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            return (AST<Program>)Factory.Instance.ToAST(config.Root);
        }

        private void LoadManifestProgram(string manifestName)
        {
            // ManifestPrograms is a static field; so lock must be held while reading and updating it.
            lock (ManifestPrograms)
            {
                InstallResult result;
                var tuple = ManifestPrograms[manifestName];
                if (tuple.Item2) return;
                var program = tuple.Item1;
                CompilerEnv.Install(program, out result);

                if (!result.Succeeded)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Error: Could not load program: " + program);
                    foreach (var pair in result.Flags)
                    {
                        sb.AppendLine(FormatError(pair.Item2));
                    }
                    throw new Exception("Error: Could not load resources");
                }
                ManifestPrograms[manifestName] = Tuple.Create<AST<Program>, bool>(program, true);
            }
        }

        private static AST<Program> ParseManifestProgram(string manifestName, string programName)
        {
            var execDir = (new FileInfo(Assembly.GetExecutingAssembly().Location)).DirectoryName;
            Task<ParseResult> parseTask = null;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                string programStr;
                using (var sr = new StreamReader(asm.GetManifestResourceStream(manifestName)))
                {
                    programStr = sr.ReadToEnd();
                }

                parseTask = Factory.Instance.ParseText(new ProgramName(Path.Combine(execDir, programName)), programStr);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to load resources: " + e.Message);
            }

            if (!parseTask.Result.Succeeded)
            {
                throw new Exception("Unable to load resources");
            }

            return parseTask.Result.Program;
        }

        private string MkReservedModuleLocation(string resModule)
        {
            return Path.Combine(
                (new FileInfo(Assembly.GetExecutingAssembly().Location)).DirectoryName,
                ReservedModuleToLocation[resModule]);
        }

        /// <summary>
        /// Makes a legal formula module name that does not clash with other modules installed
        /// by the compiler. Module name is based on the input filename.
        /// NOTE: Expected to be a function from filename -> safe module name
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string MkSafeModuleName(string filename)
        {
            try
            {
                var fileInfo = new FileInfo(filename);
                var mangledName = string.Empty;
                var fileOnlyName = fileInfo.Name;
                char c;
                for (int i = 0; i < fileOnlyName.Length; ++i)
                {
                    c = fileOnlyName[i];
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        mangledName += c;
                    }
                    else if (c == '.')
                    {
                        mangledName += '_';
                    }
                }

                if (mangledName == string.Empty || !char.IsLetter(mangledName[0]))
                {
                    mangledName = "file_" + mangledName;
                }

                while (ReservedModuleToLocation.ContainsKey(mangledName))
                {
                    mangledName += "_";
                }

                return mangledName;
            }
            catch
            {
                return "unknown";
            }
        }
        
        public bool GenerateC(ProgramName RootProgramName, AST<Model> RootModel)
        {
            using (new PerfTimer("Compiler generating C " + Path.GetFileName(RootProgramName.ToString())))
            {
                LoadManifestProgram("Pc.Domains.C.4ml");
                LoadManifestProgram("Pc.Domains.PLink.4ml");
                LoadManifestProgram("Pc.Domains.P2CProgram.4ml");
                return InternalGenerateC(RootProgramName, RootModel);
            }
        }

        bool InternalGenerateC(ProgramName RootProgramName, AST<Model> RootModel)
        {
            string RootFileName = RootProgramName.ToString();
            string fileName = Path.GetFileNameWithoutExtension(RootFileName);
            
            //// Apply the P2C transform.
            var transApply = Factory.Instance.MkModApply(Factory.Instance.MkModRef(P2CTransform, null, MkReservedModuleLocation(P2CTransform)));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkModRef(RootModel.Node.Name, null, RootProgramName.ToString()));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkCnst(fileName));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkId(Options.eraseModel ? "TRUE" : "FALSE"));
            var transStep = Factory.Instance.AddLhs(Factory.Instance.MkStep(transApply), Factory.Instance.MkId(RootModel.Node.Name + "_CModel"));
            transStep = Factory.Instance.AddLhs(transStep, Factory.Instance.MkId(RootModel.Node.Name + "_LinkModel"));

            List<Flag> appFlags;
            Task<ApplyResult> apply;
            Formula.Common.Rules.ExecuterStatistics stats;
            CompilerEnv.Apply(transStep, false, false, out appFlags, out apply, out stats);
            apply.RunSynchronously();
            foreach (Flag f in appFlags)
            {
                AddFlag(f);
            }

            //// Extract the result
            var progName = new ProgramName(Path.Combine(Environment.CurrentDirectory, RootModel.Node.Name + "_CModel.4ml"));
            var extractTask = apply.Result.GetOutputModel(RootModel.Node.Name + "_CModel", progName, AliasPrefix);
            extractTask.Wait();
            var cProgram = extractTask.Result;
            Contract.Assert(cProgram != null);
            var success = Render(cProgram, RootModel.Node.Name + "_CModel", progName);
 
            //// Extract the link model
            var linkProgName = new ProgramName(Path.Combine(Environment.CurrentDirectory, RootModel.Node.Name + "_LinkModel.4ml"));
            var linkExtractTask = apply.Result.GetOutputModel(RootModel.Node.Name + "_LinkModel", linkProgName, null);
            linkExtractTask.Wait();
            var linkModel = linkExtractTask.Result.FindAny(
                                new NodePred[] { NodePredFactory.Instance.MkPredicate(NodeKind.Program), NodePredFactory.Instance.MkPredicate(NodeKind.Model) });
            Contract.Assert(linkModel != null);

            string outputFileName = Path.ChangeExtension(fileName, ".4ml");
            Log.WriteMessage(string.Format("Writing {0} ...", outputFileName), SeverityKind.Info);
            string outputDirName = Options.outputDir == null ? Environment.CurrentDirectory : Options.outputDir;
            StreamWriter wr = new StreamWriter(File.Create(Path.Combine(outputDirName, outputFileName)));
            linkModel.Print(wr);
            wr.Close();
            return success;
        }

        private bool Render(AST<Program> cProgram, string moduleName, ProgramName progName)
        {
            //// Set the renderer of the C program so terms can be converted to text.
            var cProgramConfig = (AST<Config>)cProgram.FindAny(new NodePred[]
                {
                    NodePredFactory.Instance.MkPredicate(NodeKind.Program),
                    NodePredFactory.Instance.MkPredicate(NodeKind.Config)
                });
            Contract.Assert(cProgramConfig != null);
            var binPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;

            cProgramConfig = Factory.Instance.AddSetting(
                cProgramConfig,
                Factory.Instance.MkId(Configuration.ParsersCollectionName + ".C"),
                Factory.Instance.MkCnst(typeof(CParser.Parser).Name + " at " + Path.Combine(binPath.FullName, "CParser.dll")));

            cProgramConfig = Factory.Instance.AddSetting(
                cProgramConfig,
                Factory.Instance.MkId(Configuration.Parse_ActiveRenderSetting),
                Factory.Instance.MkCnst("C"));

            cProgram = (AST<Program>)Factory.Instance.ToAST(cProgramConfig.Root);

            //// Install and render the program.
            InstallResult instResult;
            Task<RenderResult> renderTask;
            bool didStart = false;
            didStart = CompilerEnv.Install(cProgram, out instResult);
            Contract.Assert(didStart && instResult.Succeeded);
            if (!instResult.Succeeded)
            {
                return false;
            }
            didStart = CompilerEnv.Render(cProgram.Node.Name, moduleName, out renderTask);
            Contract.Assert(didStart);
            renderTask.Wait();
            Contract.Assert(renderTask.Result.Succeeded);

            InstallResult uninstallResult;
            var uninstallDidStart = CompilerEnv.Uninstall(new ProgramName[] { progName }, out uninstallResult);
            Contract.Assert(uninstallDidStart && uninstallResult.Succeeded);

            var fileQuery = new NodePred[]
            {
                NodePredFactory.Instance.Star,
                NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact),
                NodePredFactory.Instance.MkPredicate(NodeKind.FuncTerm) &
                NodePredFactory.Instance.MkNamePredicate("File")
            };

            var success = true;
            renderTask.Result.Module.FindAll(
                fileQuery,
                (p, n) =>
                {
                    success = PrintFile(string.Empty, n) && success;
                });
            return success;
        }

        private static AST<Program> ParseFormulaFile(string fileName)
        {
            Task<ParseResult> parseTask = null;
            try
            {
                string programStr;
                using (var sr = new StreamReader(fileName))
                {
                    programStr = sr.ReadToEnd();
                }

                parseTask = Factory.Instance.ParseText(new ProgramName(fileName), programStr);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to parse file: " + e.Message);
            }

            if (!parseTask.Result.Succeeded)
            {
                throw new Exception("Unable to parse file");
            }

            return parseTask.Result.Program;
        }

        public bool Link(ICompilerOutput log, CommandLineOptions options)
        {
            this.Log = log;
            this.Options = options;
            var linkModel = Factory.Instance.MkModel(
                                    "OutputLinker",
                                    false,
                                    Factory.Instance.MkModRef(PLinkDomain, null, MkReservedModuleLocation(PLinkDomain)),
                                    ComposeKind.Extends);

            try
            {
                foreach (var fileName in options.inputFileNames)
                {
                    var program = ParseFormulaFile(fileName);
                    program.FindAll(
                        new NodePred[]
                        {
                        NodePredFactory.Instance.Star,
                        NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact)
                        },
                        (path, n) =>
                        {
                            linkModel = Factory.Instance.AddFact(linkModel, (AST<ModelFact>)Factory.Instance.ToAST(n));
                        });
                }
            }
            catch (Exception)
            {
                return false;
            }

            var linkProgName = new ProgramName(Path.Combine(Environment.CurrentDirectory, "LinkModel.4ml"));
            using (new PerfTimer("Compiler linking"))
            {
                LoadManifestProgram("Pc.Domains.C.4ml");
                LoadManifestProgram("Pc.Domains.PLink.4ml");
                InternalLink(linkProgName, linkModel);
            }
            return true;
        }

        private void InternalLink(ProgramName linkProgramName, AST<Model> linkModel)
        {
            InstallResult instResult;
            AST<Program> modelProgram = MkProgWithSettings(linkProgramName, new KeyValuePair<string, object>(Configuration.Proofs_KeepLineNumbersSetting, "TRUE"));
            bool progressed = CompilerEnv.Install(Factory.Instance.AddModule(modelProgram, linkModel), out instResult);
            Contract.Assert(progressed && instResult.Succeeded, GetFirstMessage(from t in instResult.Flags select t.Item2));

            var transApply = Factory.Instance.MkModApply(Factory.Instance.MkModRef(PLinkTransform, null, MkReservedModuleLocation(PLinkDomain)));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkModRef(linkModel.Node.Name, null, linkProgramName.ToString()));
            var transStep = Factory.Instance.AddLhs(Factory.Instance.MkStep(transApply), Factory.Instance.MkId("CLinkModel"));

            List<Flag> appFlags;
            Task<ApplyResult> apply;
            Formula.Common.Rules.ExecuterStatistics stats;
            CompilerEnv.Apply(transStep, false, false, out appFlags, out apply, out stats);
            apply.RunSynchronously();
            foreach (Flag f in appFlags)
            {
                AddFlag(f);
            }

            var progName = new ProgramName(Path.Combine(Environment.CurrentDirectory, "CLinkModel.4ml"));
            var extractTask = apply.Result.GetOutputModel("CLinkModel", progName, null);
            extractTask.Wait();
            var cProgram = extractTask.Result;
            Contract.Assert(cProgram != null);
            var success = Render(cProgram, "CLinkModel", progName);

            InstallResult uninstallResult;
            var uninstallDidStart = CompilerEnv.Uninstall(new ProgramName[] { linkProgramName }, out uninstallResult);
            Contract.Assert(uninstallDidStart && uninstallResult.Succeeded);
        }

        private bool PrintFile(string filePrefix, Node n)
        {
            var file = (FuncTerm)n;
            string fileName;
            string shortFileName;
            Quote fileBody;
            using (var it = file.Args.GetEnumerator())
            {
                it.MoveNext();
                shortFileName = filePrefix + ((Cnst)it.Current).GetStringValue();
                fileName = Path.Combine(Options.outputDir == null ? Environment.CurrentDirectory : Options.outputDir, shortFileName);
                it.MoveNext();
                fileBody = (Quote)it.Current;
            }

            Log.WriteMessage(string.Format("Writing {0} ...", shortFileName), SeverityKind.Info);

            try
            {
                using (var sw = new System.IO.StreamWriter(fileName))
                {
                    foreach (var c in fileBody.Contents)
                    {
                        Factory.Instance.ToAST(c).Print(sw);
                    }
                }
            }
            catch (Exception e)
            {
                AddFlag(
                    new Flag(
                        SeverityKind.Error,
                        default(Span),
                        string.Format("Could not save file {0} - {1}", fileName, e.Message),
                        1));
                return false;
            }

            return true;
        }

        private AST<Model> MkZingOutputModel()
        {
            var mod = Factory.Instance.MkModel(
                "OutputZing",
                false,
                Factory.Instance.MkModRef(ZingDomain, null, MkReservedModuleLocation(ZingDomain)),
                ComposeKind.Extends);

            var conf = (AST<Config>)mod.FindAny(
                new NodePred[]
                {
                    NodePredFactory.Instance.MkPredicate(NodeKind.AnyNodeKind),
                    NodePredFactory.Instance.MkPredicate(NodeKind.Config)
                });

            var myDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            conf = Factory.Instance.AddSetting(
                conf,
                Factory.Instance.MkId("parsers.Zing"),
                Factory.Instance.MkCnst("Parser at " + myDir + "\\ZingParser.dll"));
            conf = Factory.Instance.AddSetting(
                conf,
                Factory.Instance.MkId("parse_ActiveRenderer"),
                Factory.Instance.MkCnst("Zing"));

            return (AST<Model>)Factory.Instance.ToAST(conf.Root);
        }

        private struct FlagSorter : IComparer<Flag>
        {
            public int Compare(Flag x, Flag y)
            {
                if (x.Severity != y.Severity)
                {
                    return ((int)x.Severity) - ((int)y.Severity);
                }

                int cmp;
                if (x.ProgramName == null && y.ProgramName != null)
                {
                    return -1;
                }
                else if (y.ProgramName == null && x.ProgramName != null)
                {
                    return 1;
                }
                else if (x.ProgramName != null && y.ProgramName != null)
                {
                    cmp = string.Compare(x.ProgramName.ToString(), y.ProgramName.ToString());
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                }

                if (x.Span.StartLine != y.Span.StartLine)
                {
                    return x.Span.StartLine < y.Span.StartLine ? -1 : 1;
                }

                if (x.Span.StartCol != y.Span.StartCol)
                {
                    return x.Span.StartCol < y.Span.StartCol ? -1 : 1;
                }

                cmp = string.Compare(x.Message, y.Message);
                if (cmp != 0)
                {
                    return cmp;
                }

                if (x.Code != y.Code)
                {
                    return x.Code < y.Code ? -1 : 1;
                }

                return 0;
            }
        }
    }
}
