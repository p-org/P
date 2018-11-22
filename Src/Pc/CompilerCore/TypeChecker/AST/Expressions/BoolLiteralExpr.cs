using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class BoolLiteralExpr : IStaticTerm<bool>
    {
        public BoolLiteralExpr(ParserRuleContext sourceLocation, bool value)
        {
            SourceLocation = sourceLocation;
            Value = value;
        }

        public bool Value { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
        public ParserRuleContext SourceLocation { get; }
    }
}