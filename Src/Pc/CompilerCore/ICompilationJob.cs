using System.Collections.Generic;
using System.IO;
using Plang.Compiler.Backend;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler
{
    public interface ICompilationJob
    {
        string ProjectName { get; }
        bool GenerateSourceMaps { get; }
        ICompilerOutput Output { get; }
        ICodeGenerator Backend { get; }
        IReadOnlyList<FileInfo> InputFiles { get; }
        ILocationResolver LocationResolver { get; }
        ITranslationErrorHandler Handler { get; }
    }
}