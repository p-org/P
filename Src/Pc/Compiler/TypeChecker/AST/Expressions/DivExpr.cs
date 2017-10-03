using System.Diagnostics;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class DivExpr : IPExpr
    {
        public DivExpr(IPExpr lhs, IPExpr rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
            Debug.Assert(Lhs.Type.IsSameTypeAs(Rhs.Type));
            Type = Lhs.Type;
        }

        public IPExpr Lhs { get; }
        public IPExpr Rhs { get; }

        public PLanguageType Type { get; }
    }
}
