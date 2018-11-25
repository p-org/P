using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class UnnamedTupleExpr : IPExpr
    {
        public UnnamedTupleExpr(ParserRuleContext sourceLocation, IReadOnlyList<IPExpr> tupleFields)
        {
            SourceLocation = sourceLocation;
            TupleFields = tupleFields;
            Type = new TupleType(tupleFields.Select(f => f.Type).ToArray());
        }

        public IReadOnlyList<IPExpr> TupleFields { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }
    }
}