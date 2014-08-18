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

    public enum LivenessOption { None, Standard, Mace };

    internal class Compiler
    {
        public LivenessOption liveness;

        private const string PDomain = "P";
        private const string CDomain = "C";
        private const string ZingDomain = "Zing";
        private const string P2InfTypesTransform = "P2PWithInferredTypes";
        private const string AliasPrefix = "p_compiler__";

        private static readonly Tuple<string, string>[] ManifestPrograms = new Tuple<string, string>[]
        {
            new Tuple<string, string>("Pc.Domains.P.4ml", "Domains\\P.4ml"),
            new Tuple<string, string>("Pc.Domains.C.4ml", "Domains\\C.4ml"),
            new Tuple<string, string>("Pc.Domains.Zing.4ml", "Domains\\Zing.4ml"),
            new Tuple<string, string>("Pc.Transforms.PWithInferredTypes.4ml", "Transforms\\PWithInferredTypes.4ml"),
        };

        private static readonly Dictionary<string, string> ReservedModuleToLocation;

        static Compiler()
        {
            ReservedModuleToLocation = new Dictionary<string, string>();
            ReservedModuleToLocation.Add(PDomain, "Domains\\P.4ml");
            ReservedModuleToLocation.Add(CDomain, "Domains\\P.4ml");
            ReservedModuleToLocation.Add(ZingDomain, "Domains\\Zing.4ml");
            ReservedModuleToLocation.Add(P2InfTypesTransform, "Transforms\\PWithInferredTypes.4ml");
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
            var parserFlags = new List<Flag>();
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
                null,
                MkReservedModuleLocation(PDomain));
            Contract.Assert(result);

            InstallResult instResult;
            var progressed = CompilerEnv.Install(Factory.Instance.AddModule(Factory.Instance.MkProgram(inputFile), model), out instResult);
            Contract.Assert(progressed && instResult.Succeeded);

            //// Step 3. Perform Zing compilation.
            GenerateZing(inputModule, inputFile);

            return true;
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

            modelWithTypes.Print(System.Console.Out);

            var outZingModel = MkZingOutputModel();
            new PToZing(this, (AST<Model>)modelWithTypes).GenerateZing(ref outZingModel);           
            outZingModel.Print(System.Console.Out);

            return outZingModel;
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
