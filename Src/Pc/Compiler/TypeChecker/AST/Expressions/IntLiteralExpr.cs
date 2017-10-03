using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class IntLiteralExpr : IPExpr
    {
        public IntLiteralExpr(int value) { Value = value; }

        public int Value { get; }
        public PLanguageType Type { get; } = PrimitiveType.Int;
    }
}
