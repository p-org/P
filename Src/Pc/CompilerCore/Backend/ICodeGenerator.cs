using System.Collections.Generic;
using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc.Backend
{
    public interface ICodeGenerator
    {
        IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope);
    }
}
