using System.Collections.Generic;
using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc.Backend
{
    public class P3CodeGenerator : ICodeGenerator
    {
        static P3CodeGenerator()
        {
            TargetLanguage.RegisterCodeGenerator(CompilerOutput.PThree, new P3CodeGenerator());
        }

        public IReadOnlyList<CompiledFile> GenerateCode(ITranslationErrorHandler handler, Scope globalScope, ICompilerOutput log)
        {
            log.WriteMessage("P3 code generation not implemented.", SeverityKind.Error);
            return new List<CompiledFile>();
        }
    }
}
