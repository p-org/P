using System;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker
{
    internal class TypeConstructionException : Exception
    {
        public TypeConstructionException(string message, ParserRuleContext subtree, IToken location) : base(message)
        {
            Subtree = subtree;
            Location = location;
        }

        public ParserRuleContext Subtree { get; }
        public IToken Location { get; }
    }
}