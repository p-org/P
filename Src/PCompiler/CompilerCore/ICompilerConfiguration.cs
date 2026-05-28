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
        bool Debug { get; }
        int Timeout { get; }
        string CheckOnly { get; }
        IList<string> TargetProofBlocks { get; }
        int Parallelism { get; }

        /// <summary>
        /// When true (the default), the type checker collects diagnostics and
        /// continues instead of throwing on the first error. See
        /// <see cref="IDiagnosticCollector"/> for the contract.
        /// Users opt OUT of collecting via the CLI flag
        /// <c>--strict-errors</c> / <c>-se</c>, which restores the legacy
        /// abort-on-first behavior.
        /// </summary>
        bool ContinueOnError { get; }

        /// <summary>
        /// The diagnostic collector for this compilation. Same instance as
        /// <c>Handler.Diagnostics</c>. Construction-time: callers should pass
        /// <see cref="ContinueOnError"/> into the collector constructor.
        /// </summary>
        IDiagnosticCollector Diagnostics { get; }
    }
}