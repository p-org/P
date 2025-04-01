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

        public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;

        public string GlobalFunctionClassName { get; }

        public string FileName { get; }

        public string GetStaticMethodQualifiedName(Function function)
        {
            return $"{GlobalFunctionClassName}.{Names.GetNameForDecl(function)}";
        }
    }
}