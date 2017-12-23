using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public interface ILinearRef : IPExpr
    {
        LinearType LinearType { get; }
        Variable Variable { get; }
    }
}
