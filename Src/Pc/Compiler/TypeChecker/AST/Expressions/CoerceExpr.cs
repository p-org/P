using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class CoerceExpr : IPExpr
    {
        public IPExpr SubExpr { get; }
        public PLanguageType NewType { get; }

        public CoerceExpr(IPExpr subExpr, PLanguageType newType)
        {
            SubExpr = subExpr;
            NewType = newType;
        }

        public PLanguageType Type => NewType;
    }
}