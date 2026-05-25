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
    ///     <see cref="Report"/> appends the diagnostic and returns. Callers are
    ///     responsible for substituting a recovery value (e.g.
    ///     <c>ErrorType.Instance</c> or <c>new ErrorExpr(ctx)</c>) so downstream
    ///     visitors can continue without NREs. The compiler flushes the full
    ///     list after type-checking completes and exits with a non-zero code
    ///     if any errors were collected.
    ///
    /// Phase 1 wiring only — no visitor currently calls <see cref="Report"/>.
    /// Existing <c>throw handler.X(...)</c> sites remain unchanged. Phase 2
    /// will convert them to <c>handler.Diagnostics.Report(handler.X(...))</c>.
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
