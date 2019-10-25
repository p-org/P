using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;
using System.Collections.Generic;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class NamedTupleExpr : IPExpr
    {
        public NamedTupleExpr(ParserRuleContext sourceLocation, IReadOnlyList<IPExpr> tupleFields, PLanguageType type)
        {
            SourceLocation = sourceLocation;
            TupleFields = tupleFields;
            Type = type;
        }

        public IReadOnlyList<IPExpr> TupleFields { get; }

        public ParserRuleContext SourceLocation { get; }

        public PLanguageType Type { get; }
    }
}