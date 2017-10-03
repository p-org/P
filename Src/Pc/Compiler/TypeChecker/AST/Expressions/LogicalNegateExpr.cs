using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class LogicalNegateExpr : IPExpr
    {
        public LogicalNegateExpr(IPExpr subExpr)
        {
            SubExpr = subExpr;
            Type = subExpr.Type;
        }

        public IPExpr SubExpr { get; }
        public PLanguageType Type { get; }
    }
}
