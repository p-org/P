using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class UnnamedTupleExpr : IPExpr
    {
        public UnnamedTupleExpr(ParserRuleContext sourceLocation, IPExpr[] tupleFields)
        {
            SourceLocation = sourceLocation;
            TupleFields = tupleFields;
            Type = new TupleType(tupleFields.Select(f => f.Type).ToArray());
        }

        public IPExpr[] TupleFields { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }
    }
}
