using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class SignNegateExpr : IPExpr
    {
        public SignNegateExpr(IPExpr subExpr)
        {
            SubExpr = subExpr;
            Type = subExpr.Type;
        }

        public IPExpr SubExpr { get; }

        public PLanguageType Type { get; }
    }
}
