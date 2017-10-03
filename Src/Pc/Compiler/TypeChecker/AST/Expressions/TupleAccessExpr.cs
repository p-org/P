using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class TupleAccessExpr : IPExpr
    {
        public TupleAccessExpr(IPExpr subExpr, int fieldNo, PLanguageType type)
        {
            SubExpr = subExpr;
            FieldNo = fieldNo;
            Type = type;
        }

        public IPExpr SubExpr { get; }
        public int FieldNo { get; }

        public PLanguageType Type { get; }
    }
}
