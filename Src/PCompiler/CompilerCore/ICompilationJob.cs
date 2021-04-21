using Plang.Compiler.Backend;
using Plang.Compiler.TypeChecker;
using System.Collections.Generic;
using System.IO;

namespace Plang.Compiler
{
    public interface ICompilationJob
    {
        string ProjectName { get; }
        DirectoryInfo ProjectRootPath { get; }
        bool GenerateSourceMaps { get; }
        CompilerOutput OutputLanguage { get; }
        ICompilerOutput Output { get; }
        DirectoryInfo OutputDirectory { get; }
        DirectoryInfo AspectjOutputDirectory { get; }
        ICodeGenerator Backend { get; }
        IReadOnlyList<FileInfo> InputFiles { get; }
        IReadOnlyList<string> ProjectDependencies { get; }
        ILocationResolver LocationResolver { get; }
        ITranslationErrorHandler Handler { get; }
    }
}