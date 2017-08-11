using System;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker
{
    internal class DuplicateDeclarationException : Exception
    {
        public DuplicateDeclarationException(ParserRuleContext conflictingNameNode, ParserRuleContext existingDeclarationNode)
        {
            ConflictingNameNode = conflictingNameNode;
            ExistingDeclarationNode = existingDeclarationNode;
        }

        public ParserRuleContext ConflictingNameNode { get; }

        public ParserRuleContext ExistingDeclarationNode { get; }
    }
}