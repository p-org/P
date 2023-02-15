using System.Collections.Generic;
using System.IO;
using Plang.Compiler.Backend;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler
{
    public interface ICompilerConfiguration
    {
        string ProjectName { get; }
        DirectoryInfo ProjectRootPath { get; }
        CompilerOutput OutputLanguage { get; }
        ICompilerOutput Output { get; }
        DirectoryInfo OutputDirectory { get; }
        ICodeGenerator Backend { get; }
        IList<string> InputFiles { get; }
        IList<string> ProjectDependencies { get; }
        ILocationResolver LocationResolver { get; }
        ITranslationErrorHandler Handler { get; }
    }
}