using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.CSharp
{
    internal class CompilationContext : CompilationContextBase
    {
        public CompilationContext(ICompilerConfiguration job)
            : base(job)
        {
            Names = new PCheckerNameManager("PGEN_");

            FileName = $"{ProjectName}.cs";
            GlobalFunctionClassName = "GlobalFunctions";
        }

        public PCheckerNameManager Names { get; }

        /// <summary>
        /// Field-name arrays of named tuple types, keyed by the C# array initializer, so that every
        /// tuple of a type shares one array instead of allocating it on construction.
        /// </summary>
        public Dictionary<string, string> FieldNameArrays { get; } = new Dictionary<string, string>();

        public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;

        public string GlobalFunctionClassName { get; }

        public string FileName { get; }

        public string GetStaticMethodQualifiedName(Function function)
        {
            return $"{GlobalFunctionClassName}.{Names.GetNameForDecl(function)}";
        }
    }
}