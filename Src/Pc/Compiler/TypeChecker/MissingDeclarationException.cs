using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Microsoft.Pc.TypeChecker {
    public class MissingDeclarationException : Exception
    {
        public MissingDeclarationException(string declaration, ParserRuleContext location)
            : base($"Could not find declaration for {declaration}")
        {
            Declaration = declaration;
            Location = location;
        }

        public string Declaration { get; }
        public ParserRuleContext Location { get; }
    }
}