using System.Collections.Generic;
using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc.Backend
{
    public class P3CodeGenerator : ICodeGenerator
    {
        public IReadOnlyList<CompiledFile> GenerateCode(ITranslationErrorHandler handler, ICompilerOutput log, string projectName,
                                                        Scope globalScope)
        {
            log.WriteMessage("P3 code generation not implemented.", SeverityKind.Error);
            return new List<CompiledFile>();
        }
    }
}
