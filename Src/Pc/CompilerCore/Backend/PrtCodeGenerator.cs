using System.Collections.Generic;
using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc.Backend
{
    public class PrtCodeGenerator : ICodeGenerator
    {
        static PrtCodeGenerator()
        {
            TargetLanguage.RegisterCodeGenerator(CompilerOutput.C, new PrtCodeGenerator());
        }

        public IReadOnlyList<CompiledFile> GenerateCode(ITranslationErrorHandler handler, Scope globalScope, ICompilerOutput log)
        {
            log.WriteMessage("Prt code generation not implemented.", SeverityKind.Error);
            return new List<CompiledFile>();
        }
    }
}
