using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class ValuesExpr : IPExpr
    {
        public ValuesExpr(IPExpr expr, PLanguageType type)
        {
            Expr = expr;
            Type = type;
        }

        public IPExpr Expr { get; }

        public PLanguageType Type { get; }
    }
}
