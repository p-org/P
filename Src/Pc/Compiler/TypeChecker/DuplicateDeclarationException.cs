using System;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker
{
    public class DuplicateDeclarationException : Exception
    {
        public DuplicateDeclarationException(IPDecl conflictingNameNode, IPDecl existingDeclarationNode)
        {
            ConflictingNameNode = conflictingNameNode;
            ExistingDeclarationNode = existingDeclarationNode;
        }

        public IPDecl ConflictingNameNode { get; }

        public IPDecl ExistingDeclarationNode { get; }
    }
}