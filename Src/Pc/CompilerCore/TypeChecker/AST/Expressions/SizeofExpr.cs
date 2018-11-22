using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class SizeofExpr : IPExpr
    {
        public SizeofExpr(ParserRuleContext sourceLocation, IPExpr expr)
        {
            SourceLocation = sourceLocation;
            Expr = expr;
        }

        public IPExpr Expr { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; } = PrimitiveType.Int;
    }
}