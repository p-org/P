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

    internal class Compiler
    {
        public LivenessOption liveness;

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
            ReservedModuleToLocation.Add(CDomain, "Domains\\P.4ml");
            ReservedModuleToLocation.Add(ZingDomain, "Domains\\Zing.4ml");
            ReservedModuleToLocation.Add(P2InfTypesTransform, "Transforms\\PWithInferredTypes.4ml");
            ReservedModuleToLocation.Add(P2CTransform, "Transforms\\P2CProgram.4ml");
        }

        public Env CompilerEnv
        {
            get;
            private set;
        }

        public string InputFile
        {
            get;
            private set;
        }

        public PProgram ParsedProgram
        {
            get;
            private set;
        }

        public bool AttemptedCompile
        {
            get;
            private set;
        }

        public Compiler(string inputFile)
        {
            Contract.Assert(!string.IsNullOrEmpty(inputFile));
            InputFile = inputFile;
            CompilerEnv = new Env();
            InitEnv(CompilerEnv);
        }

        public bool Compile(out List<Flag> flags)
        {
            Contract.Requires(!AttemptedCompile);
            AttemptedCompile = true;

            //// Step 0. Make sure the filename is meaningful.
            flags = new List<Flag>();
            ProgramName inputFile = null;
            try
            {
                inputFile = new ProgramName(Path.Combine(Environment.CurrentDirectory, InputFile));
            }
            catch (Exception e)
            {
                flags.Add(
                    new Flag(
                        SeverityKind.Error,
                        default(Span),
                        Constants.BadFile.ToString(string.Format("{0} : {1}", InputFile, e.Message)),
                        Constants.BadFile.Code));
                return false;
            }

            //// Step 1. Attempt to parse the P program, and stop if parse fails.
            PProgram prog;
            List<Flag> parserFlags;
            var parser = new Parser.Parser();
            var result = parser.ParseFile(inputFile, out parserFlags, out prog);
            flags.AddRange(parserFlags);

            if (!result)
            {
                return false;
            }

            //// Step 2. Serialize the parsed object graph into a Formula model and install it. Should not fail.
            AST<Model> model;
            ParsedProgram = prog;
            var inputModule = MkSafeModuleName(InputFile);
            result = Factory.Instance.MkModel(
                inputModule, 
                PDomain, 
                prog.Terms, 
                out model,
                MkDeclAliases(prog),
                MkReservedModuleLocation(PDomain));
            Contract.Assert(result);

            InstallResult instResult;
            var modelProgram = MkProgWithSettings(inputFile, new KeyValuePair<string, object>(Configuration.Proofs_KeepLineNumbersSetting, "TRUE"));
            var progressed = CompilerEnv.Install(Factory.Instance.AddModule(modelProgram, model), out instResult);
            Contract.Assert(progressed && instResult.Succeeded);

            //// Step 3. Perform static analysis.
            if (!Check(inputModule, inputFile, flags))
            {
                return false;
            }

            //// GenerateC(inputModule, inputFile).Print(Console.Out);

            //// Step 4. Perform Zing compilation.
            GenerateZing(inputModule, inputFile);
            return true;
        }

        /// <summary>
        /// Run static analysis on the program.
        /// </summary>
        private bool Check(string inputModule, ProgramName inputProgram, List<Flag> flags)
        {
            //// Run static analysis on input program.
            List<Flag> queryFlags;
            Task<QueryResult> task;
            Formula.Common.Rules.ExecuterStatistics stats;
            var canStart = CompilerEnv.Query(
                inputProgram,
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
            AddErrors(task.Result, "TypeOf(_, _, ERROR)", inputProgram, errors);
            //// Enumerate duplicate definitions
            AddErrors(task.Result, "DuplicateEvent(_, _)", inputProgram, errors); 

            flags.AddRange(errors);
            return task.Result.Conclusion == LiftedBool.True;
        }

        private static void AddErrors(QueryResult result, string errorPattern, ProgramName inputProgram, SortedSet<Flag> errors)
        {
            List<Flag> queryFlags;
            foreach (var p in result.EnumerateProofs(errorPattern, out queryFlags, 1))
            {
                if (!p.HasRuleClass(ErrorClassName))
                {
                    continue;
                }

                var errorMsg = GetMessageFromProof(p);
                foreach (var loc in p.ComputeLocators())
                {
                    var exprLoc = loc[1];
                    errors.Add(new Flag(
                        SeverityKind.Error,
                        exprLoc.Span,
                        errorMsg,
                        TypeErrorCode,
                        ProgramName.Compare(inputProgram, exprLoc.Program) != 0 ? null : exprLoc.Program));
                }
            }

            foreach (var f in queryFlags)
            {
                errors.Add(f);
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

        private AST<Model> GenerateZing(string inputModelName, ProgramName inputProgram)
        {
            //// Get out the P program with type annotations. The Zing compiler is going to walk the AST.
            var transApply = Factory.Instance.MkModApply(Factory.Instance.MkModRef(P2InfTypesTransform, null, MkReservedModuleLocation(P2InfTypesTransform)));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkModRef(inputModelName, null, inputProgram.ToString()));
            var transStep = Factory.Instance.AddLhs(Factory.Instance.MkStep(transApply), Factory.Instance.MkId(inputModelName + "_WithTypes"));
            List<Flag> flags;
            Task<ApplyResult> apply;
            Formula.Common.Rules.ExecuterStatistics stats;
            CompilerEnv.Apply(transStep, false, false, out flags, out apply, out stats);
            apply.RunSynchronously();

            var extractTask = apply.Result.GetOutputModel(
                inputModelName + "_WithTypes",
                new ProgramName(Path.Combine(Environment.CurrentDirectory, inputModelName + "_WithTypes.4ml")),
                AliasPrefix);
            extractTask.Wait();

            var modelWithTypes = extractTask.Result.FindAny(
                new NodePred[] { NodePredFactory.Instance.MkPredicate(NodeKind.Program), NodePredFactory.Instance.MkPredicate(NodeKind.Model) });
            Contract.Assert(modelWithTypes != null);

            //// Visit modelWithTypes.
            modelWithTypes.Print(System.Console.Out);

            var outZingModel = MkZingOutputModel();
            new PToZing(this, (AST<Model>)modelWithTypes).GenerateZing(ref outZingModel);           
            outZingModel.Print(System.Console.Out);

            return outZingModel;
        }

        private AST<Model> GenerateC(string inputModelName, ProgramName inputProgram)
        {
            //// Apply the P2C transform.
            var transApply = Factory.Instance.MkModApply(Factory.Instance.MkModRef(P2CTransform, null, MkReservedModuleLocation(P2CTransform)));
            transApply = Factory.Instance.AddArg(transApply, Factory.Instance.MkModRef(inputModelName, null, inputProgram.ToString()));
            var transStep = Factory.Instance.AddLhs(Factory.Instance.MkStep(transApply), Factory.Instance.MkId(inputModelName + "_CModel"));
            List<Flag> flags;
            Task<ApplyResult> apply;
            Formula.Common.Rules.ExecuterStatistics stats;
            CompilerEnv.Apply(transStep, false, false, out flags, out apply, out stats);
            apply.RunSynchronously();

            //// Extract the result
            var extractTask = apply.Result.GetOutputModel(
                inputModelName + "_CModel",
                new ProgramName(Path.Combine(Environment.CurrentDirectory, inputModelName + "_CModel.4ml")),
                AliasPrefix);
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
                Factory.Instance.MkId(Configuration.Parse_ActiveRenderSetting),
                Factory.Instance.MkCnst(typeof(CParser.Parser).Name + " at " + Path.Combine(binPath.FullName, "CParser.dll")));
            cProgram = (AST<Program>)Factory.Instance.ToAST(cProgramConfig.Root);

            cProgram.Print(System.Console.Out);

            //// Install and render the program.
            InstallResult instResult;
            Task<RenderResult> renderTask;
            var didStart = CompilerEnv.Install(cProgram, out instResult);
            Contract.Assert(didStart && instResult.Succeeded);
            didStart = CompilerEnv.Render(cProgram.Node.Name, inputModelName + "_CModel", out renderTask);
            Contract.Assert(didStart);
            renderTask.Wait();
            Contract.Assert(renderTask.Result.Succeeded);
            return (AST<Model>)renderTask.Result.Module;
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

            foreach (var s in program.States)
            {
                aliases.Add(s, MkSafeAlias(string.Format("statedecl__{0}__{1}", ((Domains.P_Root.StringCnst)((Domains.P_Root.MachineDecl)s.owner).name).Value, MkQualifiedAlias((Domains.P_Root.QualifiedName)s.name)), occurrenceMap));
            }

            return aliases;
        }

        private struct FlagSorter : IComparer<Flag>
        {
            public int Compare(Flag x, Flag y)
            {
                if (x.Severity != y.Severity)
                {
                    return ((int)x.Severity) - ((int)y.Severity);
                }

                var cmp = string.Compare(x.ProgramName.ToString(), y.ProgramName.ToString());
                if (cmp != 0)
                {
                    return cmp;
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
