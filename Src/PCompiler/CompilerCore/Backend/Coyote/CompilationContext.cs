using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;
using System.Collections.Generic;

namespace Plang.Compiler.Backend.Coyote
{
    internal class CompilationContext : CompilationContextBase
    {
        public CompilationContext(ICompilationJob job)
            : base(job)
        {
            Names = new CoyoteNameManager("PGEN_");

            FileName = $"{ProjectName}.cs";
            GlobalFunctionClassName = "GlobalFunctions";
        }

        public CoyoteNameManager Names { get; }

        public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;

        public string GlobalFunctionClassName { get; }

        public string FileName { get; }

        public string GetStaticMethodQualifiedName(Function function)
        {
            return $"{GlobalFunctionClassName}.{Names.GetNameForDecl(function)}";
        }
    }
}