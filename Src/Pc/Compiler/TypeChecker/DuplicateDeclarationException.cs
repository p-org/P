using System;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker
{
    public class DuplicateDeclarationException : Exception
    {
        public DuplicateDeclarationException(IPDecl conflicting, IPDecl existing)
        {
            Conflicting = conflicting;
            Existing = existing;
        }

        public IPDecl Conflicting { get; }

        public IPDecl Existing { get; }
    }
}