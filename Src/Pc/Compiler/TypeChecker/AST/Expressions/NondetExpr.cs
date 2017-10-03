using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class NondetExpr : IPExpr
    {
        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }
}
