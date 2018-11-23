using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public class FloatLiteralExpr : IStaticTerm<double>
    {
        public FloatLiteralExpr(ParserRuleContext sourceLocation, double value)
        {
            Value = value;
            SourceLocation = sourceLocation;
        }

        public double Value { get; }
        public ParserRuleContext SourceLocation { get; }
        public PLanguageType Type { get; } = PrimitiveType.Float;
    }
}