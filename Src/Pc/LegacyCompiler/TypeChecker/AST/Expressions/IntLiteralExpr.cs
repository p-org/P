using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class IntLiteralExpr : IPExpr
    {
        public IntLiteralExpr(ParserRuleContext sourceLocation, int value)
        {
            SourceLocation = sourceLocation;
            Value = value;
        }

        public int Value { get; }

        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; } = PrimitiveType.Int;
    }
}
