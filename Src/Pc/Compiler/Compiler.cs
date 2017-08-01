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
    using Microsoft.Formula.Compiler;
    using Microsoft.Pc.Parser;
    using Microsoft.Pc.Domains;
    using System.Diagnostics;
    using Formula.Common.Terms;
#if DEBUG_DGML
    using VisualStudio.GraphModel;
#endif
    using System.Windows.Forms;


    public class Compiler : ICompiler
    {
        private const string PDomain = "P";
        private const string PLinkDomain = "PLink";
        private const string PLinkTransform = "PLink2C";
        private const string CDomain = "C";
        private const string ZingDomain = "Zing";
        private const string P2InfTypesTransform = "P2PWithInferredTypes";
        private const string P2CTransform = "P2CProgram";
        private const string AliasPrefix = "p_compiler__";

        private Dictionary<string, string> ReservedModuleToLocation;
        private HashSet<string> LoadedManifestPrograms;

        void InitManifestPrograms()
        {
            ReservedModuleToLocation = new Dictionary<string, string>
            {
                {PDomain, "P.4ml"},
                {PLinkDomain, "PLink.4ml"},
                {CDomain, "C.4ml"},
                {ZingDomain, "Zing.4ml"},
                {P2InfTypesTransform, "PWithInferredTypes.4ml"},
                {P2CTransform, "P2CProgram.4ml"}
            };

            LoadedManifestPrograms = new HashSet<string>();
        }

        public ICompilerOutput Log { get; set; }

        public IProfiler Profiler { get; set; }

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

        private ErrorReporter errorReporter;

        public Compiler(bool shortFileNames)
        {
            InitManifestPrograms();
            EnvParams envParams = null;
            if (shortFileNames)
            {
                envParams = new EnvParams(new Tuple<EnvParamKind, object>(EnvParamKind.Msgs_SuppressPaths, true));
            }
            CompilerEnv = new Env(envParams);
            this.Profiler = new NullProfiler();
        }

        public bool Compile(ICompilerOutput log, CommandLineOptions options)
        {
            if (options.profile)
            {
                this.Profiler = new ConsoleProfiler(log);
            }
            this.Log = log;
            this.Options = options;
            this.errorReporter = new ErrorReporter();
            PProgramTopDeclNames topDeclNames = new PProgramTopDeclNames();
            PProgram parsedProgram = new PProgram();
            List<ProgramName> inputProgramNames = new List<ProgramName>();
            foreach (var inputFileName in Options.inputFileNames)
            {
                ProgramName inputProgramName;
                ParsePProgram(topDeclNames, parsedProgram, inputFileName, out inputProgramName);
                if (inputProgramName != null)
                {
                    inputProgramNames.Add(inputProgramName);
                }
            }
            if (errorReporter.errors.Count > 0)
            {
                errorReporter.PrintErrors(Log, Options);
                Log.WriteMessage("Compilation failed", SeverityKind.Error);
                return false;
            }
            string outputDirName = Options.outputDir;
            if (!Directory.Exists(outputDirName))
            {
                Directory.CreateDirectory(outputDirName);
            }
            string unitFileName = Options.unitName;
            ProgramName unitProgramName = new ProgramName(unitFileName);
            AST<Program> unitProgram;
            AST<Model> unitModel;

            using (this.Profiler.Start("Compiler installing", Path.GetFileName(unitFileName)))
            {
                // Load all dependencies of P.4ml in order
                LoadManifestProgram("P.4ml");

                // Serialize the parsed object graph into a Formula model and install it. Should not fail.
                var mkModelResult = Factory.Instance.MkModel(
                    MkSafeModuleName(unitProgramName.ToString()),
                    PDomain,
                    parsedProgram.Terms,
                    out unitModel,
                    null,
                    MkReservedModuleLocation(PDomain));
                Contract.Assert(mkModelResult);

                var PUnitTerm = Factory.Instance.MkFuncTerm(Factory.Instance.MkId("PUnit"));
                PUnitTerm = Factory.Instance.AddArg(PUnitTerm, Factory.Instance.MkCnst(unitProgramName.Uri.LocalPath));
                unitModel = Factory.Instance.AddFact(unitModel, Factory.Instance.MkModelFact(null, PUnitTerm));
                foreach (var inputProgramName in inputProgramNames)
                {
                    var PUnitContainsTerm = Factory.Instance.MkFuncTerm(Factory.Instance.MkId("PUnitContains"));
                    PUnitContainsTerm = Factory.Instance.AddArg(PUnitContainsTerm, Factory.Instance.MkCnst(unitProgramName.Uri.LocalPath));
                    PUnitContainsTerm = Factory.Instance.AddArg(PUnitContainsTerm, Factory.Instance.MkCnst(inputProgramName.Uri.LocalPath));
                    unitModel = Factory.Instance.AddFact(unitModel, Factory.Instance.MkModelFact(null, PUnitContainsTerm));
                }
                foreach (var fileName in Options.dependencies)
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
                            ModelFact mf = (ModelFact)n;
                            FuncTerm ft = mf.Match as FuncTerm;
                            string ftName = (ft.Function as Id).Name;
                            if (ftName == "EventDecl" ||
                                ftName == "EventSet" ||
                                ftName == "TypeDef" ||
                                ftName == "EnumTypeDef" ||
                                ftName == "InterfaceTypeDecl" ||
                                ftName == "FunProtoDecl" ||
                                ftName == "GlobalFunCreates" ||
                                ftName == "MachineProtoDecl")
                            {
                                unitModel = Factory.Instance.AddFact(unitModel, (AST<ModelFact>)Factory.Instance.ToAST(n));
                            }
                        });
                    var DependsOnTerm = Factory.Instance.MkFuncTerm(Factory.Instance.MkId("DependsOn"));
                    DependsOnTerm = Factory.Instance.AddArg(DependsOnTerm, Factory.Instance.MkCnst(unitProgramName.Uri.LocalPath));
                    DependsOnTerm = Factory.Instance.AddArg(DependsOnTerm, Factory.Instance.MkCnst(program.Node.Name.Uri.LocalPath));
                    unitModel = Factory.Instance.AddFact(unitModel, Factory.Instance.MkModelFact(null, DependsOnTerm));
                }

                InstallResult instResult;
                unitProgram = MkProgWithSettings(unitProgramName, new KeyValuePair<string, object>(Configuration.Proofs_KeepLineNumbersSetting, "TRUE"));
                // CompilerEnv only expects one call to Install at a time.
                bool progressed = CompilerEnv.Install(Factory.Instance.AddModule(unitProgram, unitModel), out instResult);
                Contract.Assert(progressed && instResult.Succeeded, GetFirstMessage(from t in instResult.Flags select t.Item2));

                if (Options.outputFormula)
                {
                    StreamWriter wr = new StreamWriter(File.Create(Path.Combine(outputDirName, Path.GetFileNameWithoutExtension(unitFileName) + "_model.4ml")));
                    unitModel.Print(wr);
                    wr.Close();
                }
            }

            bool rc;
            if (Options.compilerOutput == CompilerOutput.Zing)
            {
                rc = GenerateZing(unitProgramName, unitModel, errorReporter.idToSourceInfo);
            }
            else
            {
                rc = GenerateCode(unitProgramName, unitModel, Options.dependencies, errorReporter.idToSourceInfo);
            }
            UninstallProgram(unitProgramName);
            errorReporter.PrintErrors(Log, Options);
            if (!rc)
            {
                Log.WriteMessage("Compilation failed", SeverityKind.Error);
            }
            return rc;
        }

        public static bool IsFileSystemCaseInsensitive { get; }

        static Compiler()
        {
            string fileUpperCase = Path.GetTempPath() + "TEST 4481D0EF-9458-4CA0-802B-DD706A811E3B";
            string fileLowerCase = Path.GetTempPath() + "test 4481d0ef-9458-4ca0-802b-dd706a811e3b";
            if (File.Exists(fileUpperCase))
            {
                File.Delete(fileUpperCase);
            }
            File.CreateText(fileLowerCase).Close();
            IsFileSystemCaseInsensitive = File.Exists(fileUpperCase);
            File.Delete(fileLowerCase);
        }

        public void ParsePProgram(PProgramTopDeclNames topDeclNames, PProgram parsedProgram, string inputFileName, out ProgramName inputProgramName)
        {
            inputProgramName = null;
            using (this.Profiler.Start("Compiler parsing", Path.GetFileName(inputFileName)))
            {
                try
                {
                    inputProgramName = new ProgramName(inputFileName);
                    List<Flag> parserFlags;
                    var parser = new Parser.PParser();
                    var result = parser.ParseFile(inputProgramName, Options, topDeclNames, parsedProgram, errorReporter.idToSourceInfo, out parserFlags);
                    foreach (Flag f in parserFlags)
                    {
                        errorReporter.AddFlag(f);
                    }
                }
                catch (Exception e)
                {
                    errorReporter.AddFlag(
                        new Flag(
                            SeverityKind.Error,
                            default(Span),
                            Constants.BadFile.ToString($"{inputFileName} : {e.Message}"),
                            Constants.BadFile.Code));
                }
            }
        }

        void UninstallProgram(ProgramName programName)
        {
            InstallResult uninstallResult;
            var uninstallDidStart = CompilerEnv.Uninstall(new ProgramName[] { programName }, out uninstallResult);
            Contract.Assert(uninstallDidStart && uninstallResult.Succeeded);
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

        public bool GenerateZing(ProgramName RootProgramName, AST<Model> RootModel, Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo)
        {
            AST<Model> rootModelWithTypes;
            using (this.Profiler.Start("Compiler generating model with types", Path.GetFileName(RootModel.Node.Name)))
            {
                ProgramName rootProgramNameWithTypes;
                if (!CreateRootModelWithTypes(RootProgramName, RootModel, out rootProgramNameWithTypes, out rootModelWithTypes))
                {
                    return false;
                }
            }

            string RootFileName = RootProgramName.ToString();
            AST<Model> zingModel;
            string fileName = Path.GetFileNameWithoutExtension(RootFileName);
            string zingFileName = fileName + ".zing";
            string dllFileName = fileName + ".dll";

            using (this.Profiler.Start("Generating Zing", zingFileName))
            {
                // Load all dependencies of Zing.4ml in order
                LoadManifestProgram("Zing.4ml");

                zingModel = MkZingOutputModel();
                var pToZing = new PToZing(this, rootModelWithTypes, idToSourceInfo);
                bool success = pToZing.GenerateZing(zingFileName, ref zingModel);
                if (!success)
                {
                    return false;
                }
            }

            if (!RenderZing(zingModel, CompilerEnv))
                return false;

            Process zcProcess = null;

            using (this.Profiler.Start("Compiling Zing", zingFileName))
            {
                var binPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
                string zingCompiler = Path.Combine(binPath.FullName, "zc.exe");
                if (!File.Exists(zingCompiler))
                {
                    Log.WriteMessage("Cannot find Zc.exe, did you build it ?", SeverityKind.Error);
                    return false;
                }
                var zcProcessInfo = new System.Diagnostics.ProcessStartInfo(zingCompiler);
                string zingFileNameFull = Path.Combine(Options.outputDir, zingFileName);
                zcProcessInfo.Arguments = $"/nowarn:292 \"/out:{Options.outputDir}\\{dllFileName}\" \"{zingFileNameFull}\"";
                zcProcessInfo.UseShellExecute = false;
                zcProcessInfo.CreateNoWindow = true;
                zcProcessInfo.RedirectStandardOutput = true;
                Log.WriteMessage($"Compiling {zingFileName} to {dllFileName} ...", SeverityKind.Info);
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

        private bool RenderZing(AST<Model> m, Env env)
        {
            var progName = new ProgramName(Path.Combine(Options.outputDir, m.Node.Name + "_ZingModel.4ml"));
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

            List<FuncTerm> nodes = SortedFiles(zingProgram, m.Node.Name);
            var success = true;
            foreach (var node in nodes)
            {
                success = PrintZingFile(node) && success;
            }
            return success;
        }

        private bool PrintZingFile(FuncTerm file)
        {
            string fileName;
            Quote fileBody;
            using (var it = file.Args.GetEnumerator())
            {
                it.MoveNext();
                fileName = ((Cnst)it.Current).GetStringValue();
                it.MoveNext();
                fileBody = (Quote)it.Current;
            }
            Log.WriteMessage($"Writing {fileName} ...", SeverityKind.Info);

            try
            {
                var fullPath = Path.Combine(Options.outputDir, fileName);
                using (var sw = new System.IO.StreamWriter(fullPath))
                {
                    foreach (var c in fileBody.Contents)
                    {
                        Factory.Instance.ToAST(c).Print(sw);
                    }
                    try
                    {
                        var asm = Assembly.GetExecutingAssembly();
                        using (var sr = new StreamReader(asm.GetManifestResourceStream("Microsoft.Pc.Zing.Prt.zing")))
                        {
                            sw.Write(sr.ReadToEnd());
                        }
                        using (var sr = new StreamReader(asm.GetManifestResourceStream("Microsoft.Pc.Zing.PrtTypes.zing")))
                        {
                            sw.Write(sr.ReadToEnd());
                        }
                        using (var sr = new StreamReader(asm.GetManifestResourceStream("Microsoft.Pc.Zing.PrtValues.zing")))
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
                Log.WriteMessage($"Could not save file {fileName} - {e.Message}", SeverityKind.Error);
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
                Log.WriteMessage(
                    $"{f.Item1.Node.Name} ({f.Item2.Span.StartLine}, {f.Item2.Span.StartCol}): {f.Item2.Severity} - {f.Item2.Message}", SeverityKind.Error);
            }
        }

        private static string AliasFunc(Symbol s)
        {
            if (s.PrintableName.EndsWith("FunDecl"))
                return "FunDecl";
            else if (s.PrintableName.EndsWith("AnonFunDecl"))
                return "AnonFunDecl";
            else
                return null;
        }

        private bool CreateRootModelWithTypes(ProgramName RootProgramName, AST<Model> RootModel, out ProgramName RootProgramNameWithTypes, out AST<Model> RootModelWithTypes)
        {
            // Load all dependenciesof PWithInferredTypes.4ml in order
            LoadManifestProgram("P.4ml");
            LoadManifestProgram("PWithInferredTypes.4ml");

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
                AliasFunc);
            extractTask.Wait();
            RootModelWithTypes = (AST<Model>)extractTask.Result.FindAny(
                new NodePred[] { NodePredFactory.Instance.MkPredicate(NodeKind.Program), NodePredFactory.Instance.MkPredicate(NodeKind.Model) });
            Contract.Assert(RootModelWithTypes != null);
            return AddCompilerErrorFlags(extractTask.Result);
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

        private void LoadManifestProgram(string programName)
        {
            string manifestName = "Microsoft.Pc.Domains." + programName;
            lock (LoadedManifestPrograms)
            {
                InstallResult result;
                if (LoadedManifestPrograms.Contains(programName)) return;
                var program = ParseManifestProgram(manifestName, programName);
                CompilerEnv.Install(program, out result);

                if (!result.Succeeded)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Error: Could not load program: " + program);
                    foreach (var pair in result.Flags)
                    {
                        sb.AppendLine(ErrorReporter.FormatError(pair.Item2, Options));
                    }
                    throw new Exception("Error: Could not load resources");
                }
                LoadedManifestPrograms.Add(programName);
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

        public bool GenerateCode(
            ProgramName RootProgramName,
            AST<Model> RootModel,
            List<string> importedFiles,
            Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo)
        {
            using (this.Profiler.Start("Compiler generating code", Path.GetFileName(RootProgramName.ToString())))
            {
                // Load all dependencies of P2CProgram.4ml in order
                LoadManifestProgram("P.4ml");
                LoadManifestProgram("C.4ml");
                LoadManifestProgram("PLink.4ml");
                LoadManifestProgram("PWithInferredTypes.4ml");
                LoadManifestProgram("P2CProgram.4ml");
                return InternalGenerateCode(RootProgramName, RootModel, importedFiles, idToSourceInfo);
            }
        }

        private AST<Node> GenerateImportFileNames(List<string> importedFiles)
        {
            AST<Node> ret = Factory.Instance.MkId("NIL");
            foreach (var f in importedFiles)
            {
                var name = Path.GetFileNameWithoutExtension(f);
                var ft = Factory.Instance.MkFuncTerm(Factory.Instance.MkId("in.StringList"));
                ret = Factory.Instance.AddArg(Factory.Instance.AddArg(ft, Factory.Instance.MkCnst(name)), ret);
            }
            return ret;
        }

        bool InternalGenerateCode(
            ProgramName RootProgramName,
            AST<Model> RootModel,
            List<string> importedFiles,
            Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo)
        {
            string RootFileName = RootProgramName.ToString();
            string fileName = Path.GetFileNameWithoutExtension(RootFileName);

            //// Apply the P2C transform.
            var transApply = Factory.Instance.MkModApply(Factory.Instance.MkModRef(P2CTransform, null, MkReservedModuleLocation(P2CTransform)));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkModRef(RootModel.Node.Name, null, RootProgramName.ToString()));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkCnst(fileName));
            transApply = Factory.Instance.AddArg(transApply, GenerateImportFileNames(importedFiles));
            var transStep = Factory.Instance.AddLhs(Factory.Instance.MkStep(transApply), Factory.Instance.MkId(RootModel.Node.Name + "_InfModel"));
            transStep = Factory.Instance.AddLhs(transStep, Factory.Instance.MkId(RootModel.Node.Name + "_CModel"));
            transStep = Factory.Instance.AddLhs(transStep, Factory.Instance.MkId(RootModel.Node.Name + "_LinkModel"));

            List<Flag> appFlags;
            Task<ApplyResult> apply;
            Formula.Common.Rules.ExecuterStatistics stats;
            CompilerEnv.Apply(transStep, false, false, out appFlags, out apply, out stats);
            apply.RunSynchronously();
            foreach (Flag f in appFlags)
            {
                errorReporter.AddFlag(f);
            }

            //// Extract the inferred types model
            var iprogName = new ProgramName(Path.Combine(Environment.CurrentDirectory, RootModel.Node.Name + "_InfModel.4ml"));
            var iExtractTask = apply.Result.GetOutputModel(RootModel.Node.Name + "_InfModel", iprogName, AliasFunc);
            iExtractTask.Wait();
            var iProgram = iExtractTask.Result;
            Contract.Assert(iProgram != null);
            if (!AddCompilerErrorFlags(iProgram))
                return false;

            if (Options.compilerOutput != CompilerOutput.C)
            {
                var iModel = (AST<Model>)iProgram.FindAny(
                                        new NodePred[] {
                                            NodePredFactory.Instance.MkPredicate(NodeKind.Program),
                                            NodePredFactory.Instance.MkPredicate(NodeKind.Model) });
                Contract.Assert(iModel != null);
                string csharpFileName = fileName + ".cs";
                switch (Options.compilerOutput) {
                    case CompilerOutput.CSharp:
                        var pToCSharp = new PToCSharpCompiler(this, iModel, idToSourceInfo, csharpFileName);
                        pToCSharp.GenerateCSharp();
                        break;
                    case CompilerOutput.PSharp:
                        var pToPSharp = new PToPSharpCompiler(this, iModel, idToSourceInfo);
                        string code = pToPSharp.GenerateCode();
                        File.WriteAllText(Path.Combine(Options.outputDir, csharpFileName), code);
                        Log.WriteMessage($"Writing {csharpFileName} ...", SeverityKind.Info);
                        break;
                    case CompilerOutput.PThree:
                        throw new Exception("P3 not yet implemented");
                }
            }
            else
            {
                //// Extract the C model
                var cProgName = new ProgramName(Path.Combine(Environment.CurrentDirectory, RootModel.Node.Name + "_CModel.4ml"));
                var cExtractTask = apply.Result.GetOutputModel(RootModel.Node.Name + "_CModel", cProgName, AliasPrefix);
                cExtractTask.Wait();
                var cProgram = cExtractTask.Result;
                Contract.Assert(cProgram != null);
                var success = RenderC(cProgram, RootModel.Node.Name + "_CModel");
                Contract.Assert(success);
            }

            //// Extract the link model
            var linkProgName = new ProgramName(Path.Combine(Environment.CurrentDirectory, RootModel.Node.Name + "_LinkModel.4ml"));
            Task<AST<Program>> linkExtractTask = apply.Result.GetOutputModel(RootModel.Node.Name + "_LinkModel", linkProgName, (string) null);
            linkExtractTask.Wait();
            AST<Node> linkModel = linkExtractTask.Result.FindAny(
                                new NodePred[] { NodePredFactory.Instance.MkPredicate(NodeKind.Program), NodePredFactory.Instance.MkPredicate(NodeKind.Model) });
            Contract.Assert(linkModel != null);
            string linkFileName = Path.ChangeExtension(fileName, ".4ml");
            Log.WriteMessage($"Writing {linkFileName} ...", SeverityKind.Info);
            var wr = new StreamWriter(File.Create(Path.Combine(Options.outputDir, linkFileName)));
            linkModel.Print(wr);
            wr.Close();

            return true;
        }

        private List<FuncTerm> SortedFiles(AST<Program> program, string moduleName)
        {
            InstallResult instResult;
            Task<RenderResult> renderTask;
            bool didStart = false;
            didStart = CompilerEnv.Install(program, out instResult);
            Contract.Assert(didStart && instResult.Succeeded);
            didStart = CompilerEnv.Render(program.Node.Name, moduleName, out renderTask);
            Contract.Assert(didStart);
            renderTask.Wait();
            Contract.Assert(renderTask.Result.Succeeded);

            InstallResult uninstallResult;
            var uninstallDidStart = CompilerEnv.Uninstall(new ProgramName[] { program.Node.Name }, out uninstallResult);
            Contract.Assert(uninstallDidStart && uninstallResult.Succeeded);

            var fileQuery = new NodePred[]
            {
                NodePredFactory.Instance.Star,
                NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact),
                NodePredFactory.Instance.MkPredicate(NodeKind.FuncTerm) &
                NodePredFactory.Instance.MkNamePredicate("File")
            };

            List<FuncTerm> nodes = new List<FuncTerm>();
            renderTask.Result.Module.FindAll(
                fileQuery,
                (p, n) =>
                {
                    nodes.Add((FuncTerm)n);
                });
            nodes.Sort(delegate (FuncTerm ft1, FuncTerm ft2)
            {
                string name1 = ((Cnst)PTranslation.GetArgByIndex(ft1, 0)).GetStringValue();
                string name2 = ((Cnst)PTranslation.GetArgByIndex(ft2, 0)).GetStringValue();
                return name1.CompareTo(name2);
            });
            return nodes;
        }

        private bool RenderC(AST<Program> cProgram, string moduleName)
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

            List<FuncTerm> nodes = SortedFiles(cProgram, moduleName);
            var success = true;
            foreach (FuncTerm node in nodes)
            {
                success = PrintCFile(node) && success;
            }
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

        public bool ParseLinkProgram(string inputFileName, out LProgram parsedProgram, out ProgramName RootProgramName)
        {
            parsedProgram = new LProgram();
            try
            {
                RootProgramName = new ProgramName(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, inputFileName)));
            }
            catch (Exception e)
            {
                errorReporter.AddFlag(
                    new Flag(
                        SeverityKind.Error,
                        default(Span),
                        Constants.BadFile.ToString($"{inputFileName} : {e.Message}"),
                        Constants.BadFile.Code));
                RootProgramName = null;
                return false;
            }

            LProgramTopDeclNames topDeclNames = new LProgramTopDeclNames();
            List<Flag> parserFlags;
            var parser = new LParser();
            var result = parser.ParseFile(RootProgramName, topDeclNames, parsedProgram, errorReporter.idToSourceInfo, out parserFlags);
            foreach (Flag f in parserFlags)
            {
                errorReporter.AddFlag(f);
            }
            if (!result)
            {
                RootProgramName = null;
                return false;
            }

            return true;
        }

        public bool Link(ICompilerOutput log, CommandLineOptions options)
        {
            if (options.profile)
            {
                this.Profiler = new ConsoleProfiler(log);
            }
            this.Log = log;
            this.Options = options;
            this.errorReporter = new ErrorReporter();
            var linkModel = Factory.Instance.MkModel(
                                    "OutputLinker",
                                    false,
                                    Factory.Instance.MkModRef(PLinkDomain, null, MkReservedModuleLocation(PLinkDomain)),
                                    ComposeKind.Extends);

            try
            {
                // compile the P file into formula file 
                var plinkFile = options.inputFileNames.Count == 1 ? options.inputFileNames[0] : "";

                using (this.Profiler.Start("Linker parsing and installing", Path.GetFileName(plinkFile)))
                {
                    LProgram linkProgram;
                    ProgramName RootProgramName;
                    AST<Model> RootModel = null;
                    if (options.inputFileNames.Count == 1)
                    {
                        if (!ParseLinkProgram(plinkFile, out linkProgram, out RootProgramName))
                        {
                            errorReporter.PrintErrors(Log, Options);
                            Log.WriteMessage("Parsing failed", SeverityKind.Error);
                            return false;
                        }

                        //// Step 0. Load all dependencies of PLink.4ml in order
                        LoadManifestProgram("C.4ml");
                        LoadManifestProgram("PLink.4ml");

                        //// Step 1. Serialize the parsed object graph into a Formula model and install it. Should not fail.
                        var mkModelResult = Factory.Instance.MkModel(
                            MkSafeModuleName(RootProgramName.ToString()),
                            PLinkDomain,
                            linkProgram.Terms,
                            out RootModel,
                            null,
                            MkReservedModuleLocation(PLinkDomain),
                            ComposeKind.None);

                        Contract.Assert(mkModelResult);

                        AST<Program> modelProgram = MkProgWithSettings(RootProgramName, new KeyValuePair<string, object>(Configuration.Proofs_KeepLineNumbersSetting, "TRUE"));
                        RootModel.FindAll(
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
                    foreach (var fileName in options.dependencies)
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

                    // Dump out the formula file corresponding to linker
                    if (options.outputFormula)
                    {
                        string outputDirName = Options.outputDir;
                        StreamWriter wr = new StreamWriter(File.Create(Path.Combine(outputDirName, "output.4ml")));
                        linkModel.Print(wr);
                        wr.Close();
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            var linkProgName = new ProgramName(Path.Combine(Environment.CurrentDirectory, "LinkModel.4ml"));
            using (this.Profiler.Start("Linker analyzing", linkProgName.ToString()))
            {
                return InternalLink(linkProgName, linkModel, options.dependencies);
            }
        }

        private bool InternalLink(ProgramName linkProgramName, AST<Model> linkModel, List<string> importedFiles)
        {
            InstallResult instResult;
            AST<Program> modelProgram = MkProgWithSettings(linkProgramName, new KeyValuePair<string, object>(Configuration.Proofs_KeepLineNumbersSetting, "TRUE"));
            bool progressed = CompilerEnv.Install(Factory.Instance.AddModule(modelProgram, linkModel), out instResult);
            Contract.Assert(progressed && instResult.Succeeded, GetFirstMessage(from t in instResult.Flags select t.Item2));

            var transApply = Factory.Instance.MkModApply(Factory.Instance.MkModRef(PLinkTransform, null, MkReservedModuleLocation(PLinkDomain)));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkModRef(linkModel.Node.Name, null, linkProgramName.ToString()));
            transApply = Factory.Instance.AddArg(transApply, GenerateImportFileNames(importedFiles));
            var transStep = Factory.Instance.AddLhs(Factory.Instance.MkStep(transApply), Factory.Instance.MkId("ErrorModel"));
            transStep = Factory.Instance.AddLhs(transStep, Factory.Instance.MkId("CLinkModel"));

            List<Flag> appFlags;
            Task<ApplyResult> apply;
            Formula.Common.Rules.ExecuterStatistics stats;
            CompilerEnv.Apply(transStep, false, false, out appFlags, out apply, out stats);
            apply.RunSynchronously();
            foreach (Flag f in appFlags)
            {
                errorReporter.AddFlag(f);
            }

            bool success = true;
            Task<AST<Program>> extractTask;

            var errorProgName = new ProgramName(Path.Combine(Environment.CurrentDirectory, "ErrorModel.4ml"));
            string errorAliasPrefix = null;
            extractTask = apply.Result.GetOutputModel("ErrorModel", errorProgName, errorAliasPrefix);
            extractTask.Wait();
            var errorProgram = extractTask.Result;
            Contract.Assert(errorProgram != null);
            string outputDirName = Options.outputDir;
            if (Options.outputFormula)
            {
                StreamWriter wr = new StreamWriter(File.Create(Path.Combine(outputDirName, "outputError.4ml")));
                errorProgram.Print(wr);
                wr.Close();
            }
            success = AddLinkerErrorFlags(errorProgram);

            errorReporter.PrintErrors(Log, Options);
            if (!success)
            {
                Log.WriteMessage("Linking failed", SeverityKind.Error);
                UninstallProgram(linkProgramName);
                return false;
            }
            if(Options.compilerOutput == CompilerOutput.CSharp)
            {
                var linker = new PToCSharpLinker(Log, errorProgram, Options.dependencies);
                success = linker.GenerateCSharpLinkerOutput(outputDirName);
            }
            else
            {
                var progName = new ProgramName(Path.Combine(Environment.CurrentDirectory, "CLinkModel.4ml"));
                string linkerAliasPrefix = null;
                extractTask = apply.Result.GetOutputModel("CLinkModel", progName, linkerAliasPrefix);
                extractTask.Wait();
                var cProgram = extractTask.Result;
                Contract.Assert(cProgram != null);
                success = RenderC(cProgram, "CLinkModel");
            }
            UninstallProgram(linkProgramName);
            return success;
        }

        void AddCompilerErrorFlag(FuncTerm ft, ref int compileErrorCount)
        {
            string ftName = (ft.Function as Id).Name;
            if (!(ftName == "ZeroIdError" || ftName == "OneIdError" || ftName == "TwoIdError")) return;
            string errorMsg = (ft.Args.Last() as Cnst).GetStringValue();
            Span errorSpan1 = default(Span);
            Span errorSpan2 = default(Span);
            if (ftName == "ZeroIdError")
            {
                errorReporter.AddFlag(new Flag(SeverityKind.Error, errorSpan1, errorMsg, 1, errorSpan1.Program));
            }
            else if (ftName == "OneIdError")
            {
                int id;
                string file;
                if (!ErrorReporter.FindIdFromFuncTerm(ft.Args.First() as FuncTerm, out file, out id))
                {
                    Debug.Assert(false, "Did not find id");
                }
                errorSpan1 = errorReporter.idToSourceInfo[file][id].entrySpan;
                errorReporter.AddFlag(new Flag(SeverityKind.Error, errorSpan1, errorMsg, 1, errorSpan1.Program));
            }
            else // (ftName == "TwoIdError")
            {
                int id;
                string file;
                if (!ErrorReporter.FindIdFromFuncTerm(PTranslation.GetArgByIndex(ft, 0) as FuncTerm, out file, out id))
                {
                    Debug.Assert(false, "Did not find id");
                }
                errorSpan1 = errorReporter.idToSourceInfo[file][id].entrySpan;
                if (!ErrorReporter.FindIdFromFuncTerm(PTranslation.GetArgByIndex(ft, 1) as FuncTerm, out file, out id))
                {
                    Debug.Assert(false, "Did not find id");
                }
                errorSpan2 = errorReporter.idToSourceInfo[file][id].entrySpan;
                errorReporter.AddFlag(new Flag(SeverityKind.Error, errorSpan1, errorMsg, 1, errorSpan1.Program));
            }
            compileErrorCount++;
        }

        bool AddCompilerErrorFlags(AST<Program> program)
        {
            int compileErrorCount = 0;
            program.FindAll(
                        new NodePred[]
                        {
                        NodePredFactory.Instance.Star,
                        NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact)
                        },
                        (path, n) =>
                        {
                            ModelFact mf = (ModelFact)n;
                            AddCompilerErrorFlag((FuncTerm)mf.Match, ref compileErrorCount);
                        });
            return compileErrorCount == 0;
        }

        void AddLinkerErrorFlag(FuncTerm ft, ref int linkErrorCount)
        {
            /* Rules:
            (1) The last arg of the error term is always the error message. 
            (2) The first arg if of type Id is the span info otherwise the span info is default.
            */
            if ((ft.Function as Id).Name.ToString().StartsWith("CSharp"))
            {
                //Console.WriteLine("Ignored");
                return;
            }

            Span errorSpan = default(Span);
            //check if the first argument is Id.
            var firstArg = ft.Args.First();
            int id;
            string file;
            if (firstArg is FuncTerm && ErrorReporter.FindIdFromFuncTerm((firstArg as FuncTerm), out file, out id) && errorReporter.idToSourceInfo.ContainsKey(file))
            {
                SourceInfo sourceInfo = errorReporter.idToSourceInfo[file][id];
                errorSpan = sourceInfo.entrySpan;
            }

            string errorMessage;
            using (var it = ft.Args.GetEnumerator())
            {
                it.MoveNext();
                //ignore the first term as its already accounted for.

                //there can be 3 arguments or 4
                if (ft.Args.Count == 3)
                {
                    it.MoveNext();
                    var name = it.Current is Cnst ? ((Cnst)it.Current).GetStringValue() : "HALT";
                    it.MoveNext();
                    string msg = (it.Current as Cnst).GetStringValue();
                    errorMessage = String.Format("{1}: ({0})", name, msg);
                }
                else
                {
                    it.MoveNext();
                    string name1 = (it.Current as Cnst).GetStringValue();
                    it.MoveNext();
                    string name2 = (it.Current as Cnst).GetStringValue();
                    it.MoveNext();
                    string msg = (it.Current as Cnst).GetStringValue();
                    errorMessage = String.Format("{2}: ({0}, {1})", name1, name2, msg);
                }
                //Add Flags
                errorReporter.AddFlag(new Flag(
                     SeverityKind.Error,
                     errorSpan,
                     errorMessage,
                     1,
                     errorSpan.Program));
                linkErrorCount++;
            }
        }

        bool AddLinkerErrorFlags(AST<Program> program)
        {
            int linkErrorCount = 0;
            program.FindAll(
                        new NodePred[]
                        {
                        NodePredFactory.Instance.Star,
                        NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact)
                        },
                        (path, n) =>
                        {
                            ModelFact mf = (ModelFact)n;
                            AddLinkerErrorFlag((FuncTerm)mf.Match, ref linkErrorCount);
                        });
            return linkErrorCount == 0;
        }

        private bool PrintCFile(FuncTerm file)
        {
            string fileName;
            string shortFileName;
            Quote fileBody;
            using (var it = file.Args.GetEnumerator())
            {
                it.MoveNext();
                shortFileName = ((Cnst)it.Current).GetStringValue();
                fileName = Path.Combine(Options.outputDir, shortFileName);
                it.MoveNext();
                fileBody = (Quote)it.Current;
            }

            Log.WriteMessage($"Writing {shortFileName} ...", SeverityKind.Info);

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
                errorReporter.AddFlag(
                    new Flag(
                        SeverityKind.Error,
                        default(Span),
                        $"Could not save file {fileName} - {e.Message}",
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

            var myDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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
    }
}
