namespace Microsoft.Pc
{
    using System;
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

    public enum LivenessOption { None, Standard, Mace };

    public class Compiler
    {
        public bool Compile(string inputFileName)
        {
            List<Flag> flags;
            var result = Compile(inputFileName, out flags);
            Compiler.WriteFlags(flags, Options.shortFileNames);
            if (!result)
            {
                Console.WriteLine("Compilation failed");
            }
            return result;
        }

        public static void WriteFlags(List<Flag> flags, bool shortFileNames)
        {
            if (shortFileNames)
            {
                var envParams = new EnvParams(
                    new Tuple<EnvParamKind, object>(EnvParamKind.Msgs_SuppressPaths, true));
                foreach (var f in flags)
                {
                    WriteMessageLine(
                        string.Format("{0} ({1}, {2}): {3}",
                        f.ProgramName == null ? "?" : f.ProgramName.ToString(envParams),
                        f.Span.StartLine,
                        f.Span.StartCol,
                        f.Message), f.Severity);
                }
            }
            else
            {
                foreach (var f in flags)
                {
                    WriteMessageLine(
                        string.Format("{0} ({1}, {2}): {3}",
                        f.ProgramName == null ? "?" : (f.ProgramName.Uri.IsFile ? f.ProgramName.Uri.LocalPath : f.ProgramName.ToString()),
                        f.Span.StartLine,
                        f.Span.StartCol,
                        f.Message), f.Severity);
                }
            }
        }

        public static void WriteMessageLine(string msg, SeverityKind severity)
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

        private const string PDomain = "P";
        private const string CDomain = "C";
        private const string ZingDomain = "Zing";
        private const string P2InfTypesTransform = "P2PWithInferredTypes";
        private const string P2CTransform = "P2CProgram";
        private const string AliasPrefix = "p_compiler__";
        private const string MsgPrefix = "msg:";
        private const string ErrorClassName = "error";
        private const int TypeErrorCode = 1;

        private static readonly Tuple<string, string>[] ManifestPrograms = new Tuple<string, string>[]
        {
            new Tuple<string, string>("Pc.Domains.P.4ml", "Domains\\P.4ml"),
            new Tuple<string, string>("Pc.Domains.C.4ml", "Domains\\C.4ml"),
            new Tuple<string, string>("Pc.Domains.Zing.4ml", "Domains\\Zing.4ml"),
            new Tuple<string, string>("Pc.Transforms.PWithInferredTypes.4ml", "Transforms\\PWithInferredTypes.4ml"),
            new Tuple<string, string>("Pc.Transforms.P2CProgram.4ml", "Transforms\\P2CProgram.4ml"),
        };

        private static readonly Dictionary<string, string> ReservedModuleToLocation;

        static Compiler()
        {
            ReservedModuleToLocation = new Dictionary<string, string>();
            ReservedModuleToLocation.Add(PDomain, "Domains\\P.4ml");
            ReservedModuleToLocation.Add(CDomain, "Domains\\C.4ml");
            ReservedModuleToLocation.Add(ZingDomain, "Domains\\Zing.4ml");
            ReservedModuleToLocation.Add(P2InfTypesTransform, "Transforms\\PWithInferredTypes.4ml");
            ReservedModuleToLocation.Add(P2CTransform, "Transforms\\P2CProgram.4ml");
        }

        public Env CompilerEnv
        {
            get;
            private set;
        }

        public string RootFileName
        {
            get;
            private set;
        }

        public HashSet<ProgramName> InputProgramNames
        {
            get;
            private set;
        }

        public ProgramName RootProgramName
        {
            get;
            private set;
        }

        public CommandLineOptions Options
        {
            get;
            set;
        }

        public string RootModule
        {
            get;
            private set;
        }

        public List<Tuple<string, AST<Model>>> AllModels
        {
            get;
            private set;
        }

        public Dictionary<string, ProgramName> SeenFileNames
        {
            get;
            private set;
        }

        public Dictionary<string, PProgram> ParsedPrograms // used only by PVisualizer
        {
            get;
            private set;
        }

        public Compiler(CommandLineOptions options)
        {
            currTime = DateTime.UtcNow;
            Options = options;
            SeenFileNames = new Dictionary<string, ProgramName>(StringComparer.OrdinalIgnoreCase);
            EnvParams envParams = null;
            if (options.shortFileNames)
            {
                envParams = new EnvParams(
                    new Tuple<EnvParamKind, object>(EnvParamKind.Msgs_SuppressPaths, true));    
            }

            CompilerEnv = new Env(envParams);
            InitEnv(CompilerEnv);
            Profile("Compiler loading");
        }

        public Compiler(bool shortFileNames)
        {
            currTime = DateTime.UtcNow;
            SeenFileNames = new Dictionary<string, ProgramName>(StringComparer.OrdinalIgnoreCase);
            EnvParams envParams = null;
            if (shortFileNames)
            {
                envParams = new EnvParams(new Tuple<EnvParamKind, object>(EnvParamKind.Msgs_SuppressPaths, true));    
            }
            CompilerEnv = new Env(envParams);
            InitEnv(CompilerEnv);
            Profile("Compiler loading");
        }

        private DateTime currTime;
        private void Profile(string msg)
        {
            if (Options == null)
                return;
            if (Options.profile)
            {
                var nextTime = DateTime.UtcNow;
                Console.WriteLine("{0}: {1}s", msg, nextTime.Subtract(currTime).TotalSeconds);
                currTime = nextTime;
            }
        }

        public bool Compile(string inputFileName, out List<Flag> flags)
        {
            InputProgramNames = new HashSet<ProgramName>();
            flags = new List<Flag>();
            RootFileName = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, inputFileName));
            try
            {
                RootProgramName = new ProgramName(RootFileName);
            }
            catch (Exception e)
            {
                flags.Add(
                    new Flag(
                        SeverityKind.Error,
                        default(Span),
                        Constants.BadFile.ToString(string.Format("{0} : {1}", inputFileName, e.Message)),
                        Constants.BadFile.Code));
                return false;
            }

            HashSet<string> crntEventNames = new HashSet<string>();
            HashSet<string> crntMachineNames = new HashSet<string>();

            InstallResult uninstallResult;
            var uninstallDidStart = CompilerEnv.Uninstall(SeenFileNames.Values, out uninstallResult);
            // Contract.Assert(uninstallDidStart && uninstallResult.Succeeded);

            SeenFileNames = new Dictionary<string, ProgramName>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, PProgram> parsedPrograms = new Dictionary<string, PProgram>(StringComparer.OrdinalIgnoreCase);
            Queue<string> parserWorkQueue = new Queue<string>();
            SeenFileNames[RootFileName] = RootProgramName;
            InputProgramNames.Add(RootProgramName);
            parserWorkQueue.Enqueue(RootFileName);
            while (parserWorkQueue.Count > 0)
            {
                PProgram prog;
                List<string> includedFileNames;
                List<Flag> parserFlags;
                string currFileName = parserWorkQueue.Dequeue();
                var parser = new Parser.Parser();
                var result = parser.ParseFile(SeenFileNames[currFileName], Options, crntEventNames, crntMachineNames, out parserFlags, out prog, out includedFileNames);
                flags.AddRange(parserFlags);
                if (!result)
                {
                    return false;
                }

                parsedPrograms.Add(currFileName, prog);

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
                        flags.Add(
                            new Flag(
                                SeverityKind.Error,
                                default(Span),
                                Constants.BadFile.ToString(string.Format("{0} : {1}", fullFileName, e.Message)),
                                Constants.BadFile.Code));
                        return false;
                    }
                    SeenFileNames[fullFileName] = programName;
                    InputProgramNames.Add(programName);
                    parserWorkQueue.Enqueue(fullFileName);
                }
            }

            ParsedPrograms = parsedPrograms;

            //// Step 1. Serialize the parsed object graph into a Formula model and install it. Should not fail.
            InstallResult instResult;
            AST<Program> modelProgram;
            bool progressed;
            AST<Model> rootModel = null;
            RootModule = null;
            AllModels = new List<Tuple<string, AST<Model>>>();
            foreach (var kv in parsedPrograms)
            {
                AST<Model> model;
                var inputModule = MkSafeModuleName(kv.Key);
                var mkModelResult = Factory.Instance.MkModel(
                    inputModule,
                    PDomain,
                    kv.Value.Terms,
                    out model,
                    null,
                    MkReservedModuleLocation(PDomain),
                    kv.Key == RootFileName && parsedPrograms.Count > 1 ? ComposeKind.Includes : ComposeKind.None);

                Contract.Assert(mkModelResult);
                string srcFileName = kv.Key;
                if (Options.shortFileNames)
                {
                    srcFileName = Path.GetFileName(srcFileName);
                }
                if (kv.Key == RootFileName)
                {
                    Contract.Assert(rootModel == null);
                    rootModel = model;
                    RootModule = inputModule;
                    if (SeenFileNames.Count > 1)
                    {
                        foreach (var kvp in SeenFileNames)
                        {
                            if (kvp.Key == RootFileName)
                            {
                                continue;
                            }

                            rootModel = Formula.API.Factory.Instance.AddModelCompose(
                                            rootModel,
                                            Formula.API.Factory.Instance.MkModRef(
                                                MkSafeModuleName(kvp.Key),
                                                null,
                                                kvp.Value.ToString()));                                              
                        }
                    }
                    AllModels.Add(new Tuple<string, AST<Model>>(srcFileName, rootModel));
                }
                else
                {
                    modelProgram = MkProgWithSettings(SeenFileNames[kv.Key], new KeyValuePair<string, object>(Configuration.Proofs_KeepLineNumbersSetting, "TRUE"));
                    progressed = CompilerEnv.Install(Factory.Instance.AddModule(modelProgram, model), out instResult);
                    Contract.Assert(progressed && instResult.Succeeded);
                    AllModels.Add(new Tuple<string, AST<Model>>(srcFileName, model));
                }
            }

            modelProgram = MkProgWithSettings(RootProgramName, new KeyValuePair<string, object>(Configuration.Proofs_KeepLineNumbersSetting, "TRUE"));
            progressed = CompilerEnv.Install(Factory.Instance.AddModule(modelProgram, rootModel), out instResult);
            Contract.Assert(progressed && instResult.Succeeded);

            if (Options.outputFormula)
            {
                string outputDirName = Options.outputDir == null ? Environment.CurrentDirectory : Options.outputDir;
                StreamWriter wr = new StreamWriter(File.Create(Path.Combine(outputDirName, "output.4ml")));
                foreach (var tuple in AllModels)
                {
                    var model = tuple.Item2;
                    model.Print(wr);
                }
                wr.Close();
            }

            //// Step 2. Perform static analysis.
            if (!Check(RootModule, flags))
            {
                return false;
            }

            if (Options.analyzeOnly)
            {
                return true;
            }

            //// Step 3. Generate outputs
            return (Options.noCOutput ? true : GenerateC(flags)) && (Options.noZingOutput ? true : GenerateZing(flags)); 
        }

        public bool GenerateZing()
        {
            return GenerateZing(new List<Flag>());
        }

        public bool GenerateZing(List<Flag> flags)
        {
            var transApply = Factory.Instance.MkModApply(Factory.Instance.MkModRef(P2InfTypesTransform, null, MkReservedModuleLocation(P2InfTypesTransform)));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkModRef(RootModule, null, RootProgramName.ToString()));
            var transStep = Factory.Instance.AddLhs(Factory.Instance.MkStep(transApply), Factory.Instance.MkId(RootModule + "_WithTypes"));
            Task<ApplyResult> apply;
            Formula.Common.Rules.ExecuterStatistics stats;
            List<Flag> applyFlags;
            CompilerEnv.Apply(transStep, false, false, out applyFlags, out apply, out stats);
            apply.RunSynchronously();
            var extractTask = apply.Result.GetOutputModel(
                RootModule + "_WithTypes",
                new ProgramName(Path.Combine(Environment.CurrentDirectory, RootModule + "_WithTypes.4ml")),
                null);
            extractTask.Wait();
            var modelWithTypes = extractTask.Result.FindAny(
                new NodePred[] { NodePredFactory.Instance.MkPredicate(NodeKind.Program), NodePredFactory.Instance.MkPredicate(NodeKind.Model) });
            Contract.Assert(modelWithTypes != null);

            AST<Model> zingModel = MkZingOutputModel();

            string fileName = Path.GetFileNameWithoutExtension(RootFileName);
            if (Options.outputFileName != null)
            {
                fileName = Options.outputFileName;
            }
            string zingFileName = fileName + ".zing";
            string dllFileName = fileName + ".dll";           
            string outputDirName = Options.outputDir == null ? Environment.CurrentDirectory : Options.outputDir;

            new PToZing(this, AllModels, (AST<Model>)modelWithTypes).GenerateZing(zingFileName, ref zingModel);
            Profile("Generating Zing");

            if (!PrintZingFile(zingModel, CompilerEnv, outputDirName))
                return false;
            var binPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
            var zcProcessInfo = new System.Diagnostics.ProcessStartInfo(Path.Combine(binPath.FullName, "zc.exe"));
            string zingFileNameFull = Path.Combine(outputDirName, zingFileName);
            zcProcessInfo.Arguments = string.Format("/nowarn:292 \"/out:{0}\\{1}\" \"{2}\"", outputDirName, dllFileName, zingFileNameFull);
            zcProcessInfo.UseShellExecute = false;
            zcProcessInfo.CreateNoWindow = true;
            zcProcessInfo.RedirectStandardOutput = true;
            Console.WriteLine("Compiling {0} to {1} ...", zingFileName, dllFileName);
            var zcProcess = System.Diagnostics.Process.Start(zcProcessInfo);
            zcProcess.WaitForExit();
            Profile("Compiling Zing");
            if(zcProcess.ExitCode != 0)
            {
                Console.WriteLine("Zc failed to compile the generated code :");
                Console.WriteLine(zcProcess.StandardOutput.ReadToEnd());
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
            // Contract.Assert(uninstallDidStart && uninstallResult.Succeeded);

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
            Console.WriteLine("Writing {0} ...", fileName);

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
                Console.WriteLine("Could not save file {0} - {1}", fileName, e.Message);
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

                Console.WriteLine(
                    "{0} ({1}, {2}): {3} - {4}",
                    f.Item1.Node.Name,
                    f.Item2.Span.StartLine,
                    f.Item2.Span.StartCol,
                    f.Item2.Severity,
                    f.Item2.Message);
            }
        }

        /// <summary>
        /// Run static analysis on the program.
        /// </summary>
        private bool Check(string inputModule, List<Flag> flags)
        {
            //// Run static analysis on input program.
            List<Flag> queryFlags;
            Task<QueryResult> task;
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
            flags.AddRange(queryFlags);
            task.RunSynchronously();

            var errors = new SortedSet<Flag>(default(FlagSorter));
            //// Enumerate typing errors
            AddErrors(task.Result, "TypeOf(_, _, ERROR)", errors, 1);
            AddErrors(task.Result, "DupNmdSubE(_, _, _, _)", errors, 1);
            AddErrors(task.Result, "PurityError(_, _)", errors, 1);
            AddErrors(task.Result, "MonitorError(_, _)", errors, 1);
            AddErrors(task.Result, "LValueError(_, _)", errors, 1);
            AddErrors(task.Result, "BadLabelError(_)", errors, 0);
            AddErrors(task.Result, "PayloadError(_)", errors, 0);
            AddErrors(task.Result, "TypeDefError(_)", errors, 0);
            AddErrors(task.Result, "FunRetError(_)", errors, 0);

            AddErrors(task.Result, "FunDeclQualifierError(_, _)", errors, 1);
            AddErrors(task.Result, "FunCallQualifierError(_, _, _)", errors, 2);
            AddErrors(task.Result, "SendQualifierError(_, _)", errors, 1);
            AddErrors(task.Result, "UnavailableVarAccessError(_, _, _)", errors, 1);
            AddErrors(task.Result, "UnavailableParameterError(_, _)", errors, 0);

            //// Enumerate structural errors
            AddErrors(task.Result, "missingDecl", errors);
            AddErrors(task.Result, "OneDeclError(_)", errors, 0);
            AddErrors(task.Result, "TwoDeclError(_, _)", errors, 1);
            AddErrors(task.Result, "DeclFunError(_, _)", errors, 1);

            if (Options.printTypeInference)
            {
                AddTerms(task.Result, "TypeOf(_, _, _)", errors, SeverityKind.Info, 0, "inferred type: ", 1, 2);
            }

            flags.AddRange(errors);
            Profile("Type checking");
            return task.Result.Conclusion == LiftedBool.True;
        }

        private void AddErrors(QueryResult result, string errorPattern, SortedSet<Flag> errors, int locationIndex = -1)
        {
            List<Flag> queryFlags;
            foreach (var p in result.EnumerateProofs(errorPattern, out queryFlags, 1))
            {
                if (!p.HasRuleClass(ErrorClassName))
                {
                    continue;
                }

                var errorMsg = GetMessageFromProof(p);
                if (locationIndex >= 0)
                {
                    foreach (var loc in p.ComputeLocators())
                    {
                        var exprLoc = loc[locationIndex];
                        errors.Add(new Flag(
                            SeverityKind.Error,
                            exprLoc.Span,
                            errorMsg,
                            TypeErrorCode,
                            InputProgramNames.Contains(exprLoc.Program) ? exprLoc.Program : null));
                    }
                }
                else
                {
                    errors.Add(new Flag(
                        SeverityKind.Error,
                        default(Span),
                        errorMsg,
                        TypeErrorCode,
                        RootProgramName));
                }
            }

            foreach (var f in queryFlags)
            {
                errors.Add(f);
            }
        }

        private void AddTerms(
            QueryResult result, 
            string termPattern, 
            SortedSet<Flag> flags, 
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
                        flags.Add(new Flag(
                            severity,
                            exprLoc.Span,
                            sw.ToString(),
                            msgCode,
                            InputProgramNames.Contains(exprLoc.Program) ? exprLoc.Program : null));
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

                    flags.Add(new Flag(
                        severity,
                        default(Span),
                        sw.ToString(),
                        msgCode,
                        RootProgramName));
                }
            }

            foreach (var f in queryFlags)
            {
                flags.Add(f);
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

        private static void InitEnv(Env env)
        {
            //// Load into the environment all the domains and transforms.
            //// Domains are installed under ExecutingLocation\Domains
            //// Transforms are installed under ExecutingLocation\Transforms

            var execDir = (new FileInfo(Assembly.GetExecutingAssembly().Location)).DirectoryName;
            foreach (var mp in ManifestPrograms)
            {
                InstallResult result;
                env.Install(LoadManifestProgram(mp.Item1, Path.Combine(execDir, mp.Item2)), out result);
                if (!result.Succeeded)
                {
                    throw new Exception("Error: Could not load resources");
                }
            }
        }

        private static AST<Program> LoadManifestProgram(string manifestName, string programName)
        {
            Task<ParseResult> parseTask = null;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                string programStr;
                using (var sr = new StreamReader(asm.GetManifestResourceStream(manifestName)))
                {
                    programStr = sr.ReadToEnd();
                }

                parseTask = Factory.Instance.ParseText(new ProgramName(programName), programStr);
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

        private static string MkReservedModuleLocation(string resModule)
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
        private static string MkSafeModuleName(string filename)
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

        /// <summary>
        /// Users may create several declarations with the same name. This method gives these different aliases
        /// to avoid failure of model compilation.
        /// </summary>
        private static string MkSafeAlias(string name, Dictionary<string, int> occurrenceMap)
        {
            if (!occurrenceMap.ContainsKey(name))
            {
                occurrenceMap.Add(name, 1);
                return name;
            }

            int n = occurrenceMap[name];
            occurrenceMap[name] = n + 1;
            return string.Format("{0}_{1}", name, n);
        }

        private static string MkQualifiedAlias(Domains.P_Root.QualifiedName qualName)
        {
            Contract.Requires(qualName != null);
            var alias = ((Domains.P_Root.StringCnst)qualName.name).Value;
            var qualifier = qualName.qualifier as Domains.P_Root.QualifiedName;
            if (qualifier == null)
            {
                return alias;
            }
            else
            {
                return string.Format("{0}_{1}", MkQualifiedAlias(qualifier), alias);
            }
        }

        private Dictionary<Microsoft.Formula.API.Generators.ICSharpTerm, string> MkDeclAliases(PProgram program)
        {
            Contract.Requires(program != null);
            var aliases = new Dictionary<Microsoft.Formula.API.Generators.ICSharpTerm, string>();
            var occurrenceMap = new Dictionary<string, int>();

            foreach (var m in program.Machines)
            {
                aliases.Add(m, MkSafeAlias("machdecl__" + ((Domains.P_Root.StringCnst)m.name).Value, occurrenceMap));
            }

            foreach (var e in program.Events)
            {
                aliases.Add(e, MkSafeAlias("evdecl__" + ((Domains.P_Root.StringCnst)e.name).Value, occurrenceMap));
            }

            foreach (var v in program.Variables)
            {
                aliases.Add(v, MkSafeAlias(string.Format("vardecl__{0}__{1}", ((Domains.P_Root.StringCnst)((Domains.P_Root.MachineDecl)v.owner).name).Value, ((Domains.P_Root.StringCnst)v.name).Value), occurrenceMap));
            }

            foreach (var f in program.Functions)
            {
                aliases.Add(f, MkSafeAlias(string.Format("fundecl__{0}__{1}", ((Domains.P_Root.StringCnst)((Domains.P_Root.MachineDecl)f.owner).name).Value, ((Domains.P_Root.StringCnst)f.name).Value), occurrenceMap));
            }

            foreach (var af in program.AnonFunctions)
            {
                aliases.Add(af, MkSafeAlias("afundecl__", occurrenceMap));
            }

            foreach (var s in program.States)
            {
                aliases.Add(s, MkSafeAlias(string.Format("statedecl__{0}__{1}", ((Domains.P_Root.StringCnst)((Domains.P_Root.MachineDecl)s.owner).name).Value, MkQualifiedAlias((Domains.P_Root.QualifiedName)s.name)), occurrenceMap));
            }

            return aliases;
        }

        public bool GenerateC()
        {
            return GenerateC(new List<Flag>());
        }

        public bool GenerateC(List<Flag> flags)
        {
            string fileName = Path.GetFileNameWithoutExtension(RootFileName);
            if (Options.outputFileName != null)
            {
                fileName = Options.outputFileName;
            }
            //// Apply the P2C transform.
            var transApply = Factory.Instance.MkModApply(Factory.Instance.MkModRef(P2CTransform, null, MkReservedModuleLocation(P2CTransform)));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkModRef(RootModule, null, RootProgramName.ToString()));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkCnst(fileName));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkId(Options.noSourceInfo ? "TRUE" : "FALSE"));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkId(Options.cTest ? "TRUE" : "FALSE"));
            var transStep = Factory.Instance.AddLhs(Factory.Instance.MkStep(transApply), Factory.Instance.MkId(RootModule + "_CModel"));

            List<Flag> appFlags;
            Task<ApplyResult> apply;
            Formula.Common.Rules.ExecuterStatistics stats;
            CompilerEnv.Apply(transStep, false, false, out appFlags, out apply, out stats);
            apply.RunSynchronously();

            flags.AddRange(appFlags);
            //// Extract the result
            var progName = new ProgramName(Path.Combine(Environment.CurrentDirectory, RootModule + "_CModel.4ml"));
            var extractTask = apply.Result.GetOutputModel(RootModule + "_CModel", progName, AliasPrefix);
            extractTask.Wait();

            var cProgram = extractTask.Result;
            Contract.Assert(cProgram != null);
            
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

            //// cProgram.Print(System.Console.Out);

            //// Install and render the program.
            InstallResult instResult;
            Task<RenderResult> renderTask;
            var didStart = CompilerEnv.Install(cProgram, out instResult);
            Contract.Assert(didStart && instResult.Succeeded);
            didStart = CompilerEnv.Render(cProgram.Node.Name, RootModule + "_CModel", out renderTask);
            Contract.Assert(didStart);
            renderTask.Wait();
            Contract.Assert(renderTask.Result.Succeeded);

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
                    success = PrintFile(string.Empty, n, flags) && success;
                });

            InstallResult uninstallResult; 
            var uninstallDidStart = CompilerEnv.Uninstall(new ProgramName[] { progName }, out uninstallResult);
            // Contract.Assert(uninstallDidStart && uninstallResult.Succeeded);
            Profile("Generating C");
            return success;
        }

        private bool PrintFile(string filePrefix, Node n, List<Flag> flags)
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

            Console.WriteLine("Writing {0} ...", shortFileName);

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
                flags.Add(
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
