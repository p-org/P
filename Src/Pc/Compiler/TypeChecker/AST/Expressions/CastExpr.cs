using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class CastExpr : IPExpr
    {
        public CastExpr(IPExpr subExpr, PLanguageType type)
        {
            Type = type;
            SubExpr = subExpr;
        }

        public IPExpr SubExpr { get; }
        public PLanguageType Type { get; }
    }
}
