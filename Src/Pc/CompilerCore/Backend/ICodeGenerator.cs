using System.Collections.Generic;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler.Backend
{
    public interface ICodeGenerator
    {
        IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope);
    }
}