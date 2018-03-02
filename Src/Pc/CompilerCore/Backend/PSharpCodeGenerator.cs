using System.Collections.Generic;
using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc.Backend
{
    public class PSharpCodeGenerator : ICodeGenerator
    {
        public IReadOnlyList<CompiledFile> GenerateCode(ITranslationErrorHandler handler, ICompilerOutput log, string projectName,
                                                        Scope globalScope)
        {
            log.WriteMessage("P# code generation not implemented.", SeverityKind.Error);
            return new List<CompiledFile>();
        }
    }
}