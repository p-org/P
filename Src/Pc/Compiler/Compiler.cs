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
    using Microsoft.Pc.Parser;
    using Microsoft.Pc.Domains;
    using System.Diagnostics;
    using Formula.Common.Terms;
#if DEBUG_DGML
    using VisualStudio.GraphModel;
#endif
    using System.Windows.Forms;
    using System.Xml.Linq;

    public enum CompilerOutput { C0, CSharp, Zing };

    public enum LivenessOption { None, Standard, Sampling };


    public class SourceInfo
    {
        public Span entrySpan;
        public Span exitSpan;

        public SourceInfo(Span entrySpan, Span exitSpan)
        {
            this.entrySpan = entrySpan;
            this.exitSpan = exitSpan;
        }
    }

    public interface IProfiler
    {
        IDisposable Start(string operation, string message);
    }

    public class NullProfiler : IProfiler
    {
        public IDisposable Start(string operation, string message)
        {
            return null;
        }
    }

    public class ConsoleProfiler : IProfiler
    {
        ICompilerOutput Log;

        public ConsoleProfiler(ICompilerOutput log)
        {
            this.Log = log;
        }

        public IDisposable Start(string operation, string message)
        {
            return new ConsoleProfileWatcher(Log, operation, message);
        }

        class ConsoleProfileWatcher : IDisposable
        {
            Stopwatch watch = new Stopwatch();
            string operation;
            string message;
            ICompilerOutput Log;

            public ConsoleProfileWatcher(ICompilerOutput log, string operation, string message)
            {
                this.Log = log;
                this.operation = operation;
                this.message = message;
                watch.Start();
            }
            public void Dispose()
            {
                watch.Stop();
                string msg = string.Format("{0}: {1} {2} {3}", DateTime.Now.ToShortTimeString(), watch.Elapsed.ToString(), operation, message);
                Log.WriteMessage(msg, SeverityKind.Info);
            }
        }

    }

    public class XmlProfiler : IProfiler
    {
        XDocument data;
        XElement current;

        public XmlProfiler()
        {
            data = new XDocument(new XElement("data"));
        }

        public XDocument Data { get { return this.data; } }

        public IDisposable Start(string operation, string message)
        {
            XElement e = new XElement("operation", new XAttribute("name", operation), new XAttribute("description", message));
            if (current == null)
            {
                data.Root.Add(e);
            }
            else
            {
                current.Add(e);
            }
            current = e;
            return new XmlProfileWatcher(this, e, operation, message);
        }

        private void Finish(XElement e, DateTime timestamp, TimeSpan elapsed)
        {
            e.Add(new XAttribute("timestsamp", timestamp));
            e.Add(new XAttribute("elapsed", elapsed));
            if (current == e)
            {
                current = current.Parent;
            }
        }

        class XmlProfileWatcher : IDisposable
        {
            XmlProfiler owner;
            XElement e;
            Stopwatch watch = new Stopwatch();
            string operation;
            string message;

            public XmlProfileWatcher(XmlProfiler owner, XElement e, string operation, string message)
            {
                this.e = e;
                this.owner = owner;
                this.operation = operation;
                this.message = message;
                watch.Start();
            }
            public void Dispose()
            {
                watch.Stop();
                owner.Finish(e, DateTime.Now, watch.Elapsed);
            }
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

    public class ErrorReporter
    {
        private const string MsgPrefix = "msg:";
        private const int TypeErrorCode = 1;
        private const string ErrorClassName = "error";

        public SortedSet<Flag> errors;
        public Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo;

        public ErrorReporter()
        {
            this.idToSourceInfo = new Dictionary<string, Dictionary<int, SourceInfo>>();
            this.errors = new SortedSet<Flag>(default(FlagSorter));
        }

        public void PrintErrors(ICompilerOutput Log, CommandLineOptions Options)
        {
            foreach (var f in errors)
            {
                PrintFlag(f, Log, Options);
            }
        }

        public void AddFlag(Flag f)
        {
            if (errors.Contains(f))
            {
                return;
            }
            errors.Add(f);
        }

        public void PrintFlag(Flag f, ICompilerOutput Log, CommandLineOptions Options)
        {
            Log.WriteMessage(FormatError(f, Options), f.Severity);
        }

        public static string FormatError(Flag f, CommandLineOptions Options)
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

        public static bool FindIdFromTerm(Term term, out string fileName, out int id)
        {
            id = 0;
            fileName = "";
            if (term.Args.Count() == 0) return false;
            var idTerm = term.Args.Last();
            var symbol = idTerm.Args[0].Symbol as BaseCnstSymb;
            if (symbol == null) return false;
            if (symbol.CnstKind != CnstKind.Numeric) return false;
            id = (int)((Rational)symbol.Raw).Numerator;

            symbol = idTerm.Args[1].Symbol as BaseCnstSymb;
            if (symbol == null) return false;
            if (symbol.CnstKind != CnstKind.String) return false;
            fileName = (string)((Cnst)(symbol).Raw).GetStringValue();
            return true;
        }

        public static bool FindIdFromFuncTerm(FuncTerm idTerm, out string fileName, out int id)
        {
            id = 0;
            fileName = "";
            if (idTerm.Args.Count() == 0) return false;
            var symbol = idTerm.Args.ElementAt(0) as Cnst;
            if (symbol == null) return false;
            if (symbol.CnstKind != CnstKind.Numeric) return false;
            id = (int)(symbol.GetNumericValue()).Numerator;

            symbol = (idTerm.Args.ElementAt(1) as FuncTerm).Args.ElementAt(0) as Cnst;
            if (symbol == null) return false;
            if (symbol.CnstKind != CnstKind.String) return false;
            fileName = (string)(symbol).GetStringValue();
            return true;
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

        private Dictionary<string, string> ReservedModuleToLocation;
        private HashSet<string> LoadedManifestPrograms;

        void InitManifestPrograms()
        {
            ReservedModuleToLocation = new Dictionary<string, string>();
            ReservedModuleToLocation.Add(PDomain, "P.4ml");
            ReservedModuleToLocation.Add(PLinkDomain, "PLink.4ml");
            ReservedModuleToLocation.Add(CDomain, "C.4ml");
            ReservedModuleToLocation.Add(ZingDomain, "Zing.4ml");
            ReservedModuleToLocation.Add(P2InfTypesTransform, "PWithInferredTypes.4ml");
            ReservedModuleToLocation.Add(P2CTransform, "P2CProgram.4ml");

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
            options.eraseModel = options.compilerOutput != CompilerOutput.C0;
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
                    MkReservedModuleLocation(PDomain),
                    ComposeKind.None);
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
                                ftName == "ModelType" ||
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
                    StreamWriter wr = new StreamWriter(File.Create(Path.Combine(outputDirName, Path.GetFileName(unitFileName) + ".4ml")));
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

        private static bool isFileSystemCaseInsensitive;

        public static bool IsFileSystemCaseInsensitive
        {
            get
            {
                return isFileSystemCaseInsensitive;
            }
        }

        static Compiler()
        {
            string fileUpperCase = Path.GetTempPath() + "TEST 4481D0EF-9458-4CA0-802B-DD706A811E3B";
            string fileLowerCase = Path.GetTempPath() + "test 4481d0ef-9458-4ca0-802b-dd706a811e3b";
            if (File.Exists(fileUpperCase))
            {
                File.Delete(fileUpperCase);
            }
            File.CreateText(fileLowerCase).Close();
            isFileSystemCaseInsensitive = File.Exists(fileUpperCase);
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
                            Constants.BadFile.ToString(string.Format("{0} : {1}", inputFileName, e.Message)),
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
            ProgramName RootProgramNameWithTypes;
            AST<Model> RootModelWithTypes;
            using (this.Profiler.Start("Compiler generating model with types", Path.GetFileName(RootModel.Node.Name)))
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

            using (this.Profiler.Start("Generating Zing", zingFileName))
            {
                // Load all dependencies of Zing.4ml in order
                LoadManifestProgram("Zing.4ml");

                zingModel = MkZingOutputModel();
                var pToZing = new PToZing(this, RootModelWithTypes, idToSourceInfo);
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
                zcProcessInfo.Arguments = string.Format("/nowarn:292 \"/out:{0}\\{1}\" \"{2}\"", Options.outputDir, dllFileName, zingFileNameFull);
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
            Log.WriteMessage(string.Format("Writing {0} ...", fileName), SeverityKind.Info);

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
            Func<Symbol, string> aliasPrefixFunc = (x => AliasFunc(x));
            var extractTask = apply.Result.GetOutputModel(
                RootModuleWithTypes,
                RootProgramNameWithTypes,
                aliasPrefixFunc);
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
            string manifestName = "Pc.Domains." + programName;
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
            using (this.Profiler.Start("Compiler generating C", Path.GetFileName(RootProgramName.ToString())))
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

            {
                //// Extract the inferred types model
                var iprogName = new ProgramName(Path.Combine(Environment.CurrentDirectory, RootModel.Node.Name + "_InfModel.4ml"));
                Func<Symbol, string> aliasPrefixFunc = (x => AliasFunc(x));
                var extractTask = apply.Result.GetOutputModel(RootModel.Node.Name + "_InfModel", iprogName, aliasPrefixFunc);
                extractTask.Wait();
                var iProgram = extractTask.Result;
                Contract.Assert(iProgram != null);
                if (!AddCompilerErrorFlags(iProgram))
                    return false;
                if (Options.compilerOutput == CompilerOutput.CSharp)
                {
                    var iModel = (AST<Model>)iProgram.FindAny(
                                            new NodePred[] {
                                            NodePredFactory.Instance.MkPredicate(NodeKind.Program),
                                            NodePredFactory.Instance.MkPredicate(NodeKind.Model) });
                    Contract.Assert(iModel != null);
                    string csharpFileName = fileName + ".cs";
                    var pToCSharp = new PToCSharpCompiler(this, iModel, idToSourceInfo, csharpFileName);
                    pToCSharp.GenerateCSharp();
                }
            }

            {
                //// Extract the C model
                var progName = new ProgramName(Path.Combine(Environment.CurrentDirectory, RootModel.Node.Name + "_CModel.4ml"));
                var extractTask = apply.Result.GetOutputModel(RootModel.Node.Name + "_CModel", progName, AliasPrefix);
                extractTask.Wait();
                var cProgram = extractTask.Result;
                Contract.Assert(cProgram != null);
                var success = RenderC(cProgram, RootModel.Node.Name + "_CModel");
                Contract.Assert(success);
            }

            {
                //// Extract the link model
                var linkProgName = new ProgramName(Path.Combine(Environment.CurrentDirectory, RootModel.Node.Name + "_LinkModel.4ml"));
                string linkerAliasPrefix = null;
                var linkExtractTask = apply.Result.GetOutputModel(RootModel.Node.Name + "_LinkModel", linkProgName, linkerAliasPrefix);
                linkExtractTask.Wait();
                var linkModel = linkExtractTask.Result.FindAny(
                                    new NodePred[] { NodePredFactory.Instance.MkPredicate(NodeKind.Program), NodePredFactory.Instance.MkPredicate(NodeKind.Model) });
                Contract.Assert(linkModel != null);
                string outputFileName = Path.ChangeExtension(fileName, ".4ml");
                Log.WriteMessage(string.Format("Writing {0} ...", outputFileName), SeverityKind.Info);
                string outputDirName = Options.outputDir;
                StreamWriter wr = new StreamWriter(File.Create(Path.Combine(outputDirName, outputFileName)));
                linkModel.Print(wr);
                wr.Close();
            }

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
                        Constants.BadFile.ToString(string.Format("{0} : {1}", inputFileName, e.Message)),
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

                using (this.Profiler.Start("Linker parsing", Path.GetFileName(plinkFile)))
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
                errorReporter.AddFlag(
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
    }
}
