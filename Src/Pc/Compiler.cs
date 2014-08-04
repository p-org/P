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
    using Microsoft.Formula.API.Nodes;

    internal class Compiler
    {
        private const string PDomain = "P";
        private const string CDomain = "C";

        private static readonly Tuple<string, string>[] ManifestPrograms = new Tuple<string, string>[]
        {
            new Tuple<string, string>("Pc.Domains.P.4ml", "Domains\\P.4ml"),
            new Tuple<string, string>("Pc.Domains.C.4ml", "Domains\\C.4ml")
        };

        private static readonly Dictionary<string, string> ReservedModuleToLocation;

        static Compiler()
        {
            ReservedModuleToLocation = new Dictionary<string, string>();
            ReservedModuleToLocation.Add(PDomain, "Domains\\P.4ml");
            ReservedModuleToLocation.Add(CDomain, "Domains\\P.4ml");
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
            var parser = new Parser.Parser();
            PProgram prog;
            var result = parser.ParseFile(
                new ProgramName(Path.Combine(Environment.CurrentDirectory, InputFile)), 
                out flags, 
                out prog);

            if (!result)
            {
                return false;
            }

            AST<Model> model;
            ParsedProgram = prog;
            result = Factory.Instance.MkModel(
                MkSafeModuleName(InputFile), 
                PDomain, 
                prog.Terms, 
                out model,
                null,
                MkReservedModuleLocation(PDomain));
            Contract.Assert(result);

            model.Print(Console.Out);

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
    }
}
