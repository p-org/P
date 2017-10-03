using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class UnnamedTupleExpr : IPExpr
    {
        public UnnamedTupleExpr(IPExpr[] tupleFields, PLanguageType type)
        {
            TupleFields = tupleFields;
            Type = type;
        }

        public IPExpr[] TupleFields { get; }

        public PLanguageType Type { get; }
    }
}
