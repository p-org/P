using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class ContainsExpr : IPExpr
    {
        public ContainsExpr(ParserRuleContext sourceLocation, IPExpr item, IPExpr collection)
        {
            SourceLocation = sourceLocation;
            Item = item;
            Collection = collection;
        }

        public IPExpr Item { get; }
        public IPExpr Collection { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
        public ParserRuleContext SourceLocation { get; }
    }
}