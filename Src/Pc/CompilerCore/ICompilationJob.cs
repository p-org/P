using System.Collections.Generic;
using System.IO;
using Microsoft.Pc.Backend;
using Microsoft.Pc.TypeChecker;

namespace Microsoft.Pc
{
    public interface ICompilationJob
    {
        string ProjectName { get; }
        ICompilerOutput Output { get; }
        ICodeGenerator Backend { get; }
        IReadOnlyList<FileInfo> InputFiles { get; }
        ILocationResolver LocationResolver { get; }
        ITranslationErrorHandler Handler { get; }
    }
}