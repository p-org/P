using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IPExpr : IPAST
    {
        PLanguageType Type { get; }
    }
}
