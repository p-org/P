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

        public IReadOnlyList<Exception> Diagnostics => diagnostics;

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
