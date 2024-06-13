using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;
using System.Collections.Generic;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class SeqLiteralExpr : IExprTerm
    {
        public SeqLiteralExpr(ParserRuleContext sourceLocation, List<IPExpr> values, PLanguageType type)
        {
            SourceLocation = sourceLocation;
            Value = values;
            Type = type;
        }

        public List<IPExpr> Value { get; }

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; }
    }
}