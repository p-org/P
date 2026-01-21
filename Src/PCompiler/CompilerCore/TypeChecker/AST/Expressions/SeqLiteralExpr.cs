using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;
using System.Collections.Generic;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class SeqLiteralExpr(ParserRuleContext sourceLocation, List<IPExpr> values, PLanguageType type)
        : IExprTerm
    {
        public List<IPExpr> Value { get; } = values;

        public ParserRuleContext SourceLocation { get; } = sourceLocation;
        public PLanguageType Type { get; } = type;
    }
}