using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class KeysExpr : IPExpr
    {
        public KeysExpr(ParserRuleContext sourceLocation, IPExpr expr, PLanguageType type)
        {
            SourceLocation = sourceLocation;
            Expr = expr;
            Type = type;
        }

        public ParserRuleContext SourceLocation { get; }
        public IPExpr Expr { get; }
        public PLanguageType Type { get; }
    }
}
