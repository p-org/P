using System;
using System.Collections.Generic;

namespace Plang.Compiler
{
    /// <summary>
    /// Default <see cref="IDiagnosticCollector"/>. See interface docs for the
    /// strict/collecting mode contract.
    /// </summary>
    public sealed class DefaultDiagnosticCollector : IDiagnosticCollector
    {
        private readonly List<Exception> diagnostics = new List<Exception>();

        public DefaultDiagnosticCollector(bool continueOnError = false)
        {
            ContinueOnError = continueOnError;
        }

        public bool ContinueOnError { get; }

        // AsReadOnly returns a live ReadOnlyCollection<T> wrapper: callers can
        // still observe new diagnostics as they're added, but mutating methods
        // throw NotSupportedException even if a caller downcasts. This
        // preserves the invariant that only Report() can change the list.
        public IReadOnlyList<Exception> Diagnostics => diagnostics.AsReadOnly();

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
