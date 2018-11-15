using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.PSharp
{
    internal class CompilationContext : CompilationContextBase
    {
        public PSharpNameManager Names { get; }

        public CompilationContext(ICompilationJob job)
            : base(job)
        {
            Names = new PSharpNameManager("PGEN_");

            FileName = $"{ProjectName}.cs";
            GlobalFunctionClassName = $"GlobalFunctions";
        }

        public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;

        public string GetStaticMethodQualifiedName(Function function)
        {
            return $"{GlobalFunctionClassName}.{Names.GetNameForDecl(function)}";
        }

        public string GlobalFunctionClassName { get; }

        public string FileName { get; }
    }
}