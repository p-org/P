using System;

namespace Microsoft.Pc.TypeChecker {
    public class MissingDeclarationException : Exception
    {
        public MissingDeclarationException(IPDecl declaration)
            : base($"Could not find declaration for {declaration.Name}")
        {
            Declaration = declaration;
        }

        public IPDecl Declaration { get; }
    }
}