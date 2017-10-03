using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class FloatLiteralExpr : IPExpr
    {
        public FloatLiteralExpr(double value) { Value = value; }
        public double Value { get; }
        public PLanguageType Type { get; } = PrimitiveType.Float;
    }
}
