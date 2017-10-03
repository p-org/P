using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class BoolLiteralExpr : IPExpr
    {
        public BoolLiteralExpr(bool value) { Value = value; }

        public bool Value { get; }

        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }
}
