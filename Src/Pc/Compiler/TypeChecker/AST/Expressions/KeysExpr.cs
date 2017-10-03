using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class KeysExpr : IPExpr
    {
        public KeysExpr(IPExpr expr, PLanguageType type)
        {
            Expr = expr;
            Type = type;
        }

        public IPExpr Expr { get; }
        public PLanguageType Type { get; }
    }
}
