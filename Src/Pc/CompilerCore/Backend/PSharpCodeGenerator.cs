using System.Collections.Generic;
using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc.Backend
{
    public class PSharpCodeGenerator : ICodeGenerator
    {
        static PSharpCodeGenerator()
        {
            TargetLanguage.RegisterCodeGenerator(CompilerOutput.PSharp, new PSharpCodeGenerator());
        }

        public IReadOnlyList<CompiledFile> GenerateCode(ITranslationErrorHandler handler, Scope globalScope, ICompilerOutput log)
        {
            log.WriteMessage("P# code generation not implemented.", SeverityKind.Error);
            return new List<CompiledFile>();
        }
    }
}