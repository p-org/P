using System.Collections.Generic;
using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc.Backend
{
    public interface ICodeGenerator
    {
        IReadOnlyList<CompiledFile> GenerateCode(ITranslationErrorHandler handler, Scope globalScope, ICompilerOutput log);
    }
}
