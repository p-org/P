using System;
using System.Collections.Generic;

namespace Plang.Compiler
{
    /// <summary>
    /// Collects diagnostics (errors/warnings) produced by the compiler.
    ///
    /// Two modes:
    ///   - Strict (default, <see cref="ContinueOnError"/> == false):
    ///     <see cref="Report"/> re-throws the diagnostic immediately, preserving
    ///     the historical "fail on first error" behavior. The compiler unwinds
    ///     to the top-level catch in <c>Compiler.cs</c>.
    ///   - Collecting (<see cref="ContinueOnError"/> == true):
    ///     <see cref="Report"/> appends the diagnostic and returns. Callers
    ///     substitute a recovery value (e.g. <see cref="Plang.Compiler.TypeChecker.Types.ErrorType"/>.Instance or
    ///     <c>new ErrorExpr(ctx)</c>) so downstream visitors continue without
    ///     NREs. The compiler flushes the full list (sorted by source location)
    ///     after type-checking completes and exits with a non-zero code if any
    ///     errors were collected.
    ///
    /// Lifecycle:
    /// <list type="bullet">
    ///   <item><c>CompilerConfiguration.ReadContinueOnErrorEnvVar</c> seeds
    ///     <see cref="ContinueOnError"/> from the
    ///     <c>P_COMPILER_COLLECT_ERRORS</c> environment variable.</item>
    ///   <item>Visitors (<c>ExprVisitor</c>, <c>StatementVisitor</c>) report
    ///     through <c>handler.Diagnostics.Report(handler.X(...))</c>.</item>
    ///   <item><c>Analyzer.TolerantStep</c> wraps each gathering-pass iteration
    ///     so a TranslationException on one machine/function doesn't clobber
    ///     siblings' diagnostics.</item>
    ///   <item><c>Compiler.FlushCollectedDiagnostics</c> sorts by
    ///     <c>(file, line, column)</c> and emits each diagnostic with
    ///     <c>[Error:]</c> prefix to stderr.</item>
    /// </list>
    ///
    /// The contract is exercised by
    /// <c>Tst/UnitTests/TypeChecker/DiagnosticCollectorTest.cs</c>,
    /// <c>Tst/UnitTests/TypeChecker/Phase1DormancyTest.cs</c>, and
    /// <c>Tst/UnitTests/TypeChecker/MultiErrorAcceptanceTest.cs</c>.
    /// </summary>
    public interface IDiagnosticCollector
    {
        /// <summary>
        /// Record a diagnostic.
        /// In strict mode, throws <paramref name="diagnostic"/> immediately.
        /// In collecting mode, appends and returns.
        /// </summary>
        void Report(Exception diagnostic);

        /// <summary>
        /// All diagnostics collected so far, in the order they were reported.
        /// Empty in strict mode (since <see cref="Report"/> never returns).
        /// </summary>
        IReadOnlyList<Exception> Diagnostics { get; }

        /// <summary>
        /// True iff at least one diagnostic has been collected.
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// When true, <see cref="Report"/> appends instead of throwing.
        /// Set once at construction; not expected to change during a run.
        /// </summary>
        bool ContinueOnError { get; }
    }
}
