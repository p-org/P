using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class FairNondetExpr : IPExpr
    {
        public PLanguageType Type { get; } = PrimitiveType.Bool;
    }
}
