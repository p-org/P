using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Plang.Compiler
{
    /// <summary>
    /// Default <see cref="IDiagnosticCollector"/>. See interface docs for the
    /// strict/collecting mode contract.
    /// </summary>
    public sealed class DefaultDiagnosticCollector : IDiagnosticCollector
    {
        private readonly List<Exception> diagnostics = new List<Exception>();

        // ReadOnlyCollection<T> is a *live* wrapper over the backing list — it
        // reflects subsequent Report() additions while rejecting mutators if
        // a caller downcasts (Add/Clear throw NotSupportedException). Cached
        // once at construction so frequent reads of Diagnostics (e.g. during
        // Compiler.cs's flush pass) don't allocate a fresh wrapper each call.
        private readonly ReadOnlyCollection<Exception> diagnosticsView;

        public DefaultDiagnosticCollector(bool continueOnError = false)
        {
            ContinueOnError = continueOnError;
            diagnosticsView = diagnostics.AsReadOnly();
        }

        public bool ContinueOnError { get; }

        public IReadOnlyList<Exception> Diagnostics => diagnosticsView;

        public bool HasErrors => diagnostics.Count > 0;

        public void Report(Exception diagnostic)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            if (!ContinueOnError)
            {
                throw diagnostic;
            }

            diagnostics.Add(diagnostic);
        }
    }
}
