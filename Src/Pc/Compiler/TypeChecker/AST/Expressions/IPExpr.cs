using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public interface IPExpr
    {
        PLanguageType Type { get; }
    }
}
