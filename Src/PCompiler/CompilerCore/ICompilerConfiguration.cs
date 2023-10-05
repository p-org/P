using System.Collections.Generic;
using System.IO;
using Plang.Compiler.Backend;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler
{
    public interface ICompilerConfiguration
    {
        string ProjectName { get; }
        string PObservePackageName { get; }
        DirectoryInfo ProjectRootPath { get; }
        IList<CompilerOutput> OutputLanguages { get; }
        ICompilerOutput Output { get; set; }
        DirectoryInfo OutputDirectory { get; set; }
        ICodeGenerator Backend { get; set; }
        IList<string> InputPFiles { get; }
        IList<string> InputForeignFiles { get; }
        IList<string> ProjectDependencies { get; }
        ILocationResolver LocationResolver { get; }
        ITranslationErrorHandler Handler { get; }
    }
}