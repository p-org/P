using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class GreaterThanExpr : IPExpr
    {
        public GreaterThanExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }
}
