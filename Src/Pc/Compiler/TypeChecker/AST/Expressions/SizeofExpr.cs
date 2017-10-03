using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class SizeofExpr : IPExpr
    {
        public SizeofExpr(IPExpr expr) { Expr = expr; }

        public IPExpr Expr { get; }

        public PLanguageType Type { get; } = PrimitiveType.Int;
    }
}
