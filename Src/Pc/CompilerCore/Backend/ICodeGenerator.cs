using Plang.Compiler.TypeChecker;
using System.Collections.Generic;

namespace Plang.Compiler.Backend
{
    public interface ICodeGenerator
    {
        IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope);
    }
}