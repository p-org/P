using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public interface IPExpr : IPAST
    {
        PLanguageType Type { get; }
    }
}
