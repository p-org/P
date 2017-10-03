using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public class DefaultExpr : IPExpr
    {
        public DefaultExpr(PLanguageType type) { Type = type; }

        public PLanguageType Type { get; }
    }
}
