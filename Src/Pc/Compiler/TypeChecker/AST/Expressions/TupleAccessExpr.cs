using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class TupleAccessExpr : IPExpr
    {
        public TupleAccessExpr(ParserRuleContext sourceLocation, IPExpr subExpr, int fieldNo, PLanguageType type)
        {
            SourceLocation = sourceLocation;
            SubExpr = subExpr;
            FieldNo = fieldNo;
            Type = type;
        }

        public IPExpr SubExpr { get; }
        public int FieldNo { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }
    }
}
