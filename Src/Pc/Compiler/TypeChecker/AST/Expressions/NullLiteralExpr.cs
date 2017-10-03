using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class NullLiteralExpr : IPExpr
    {
        public PLanguageType Type { get; } = PrimitiveType.Null;
    }
}
