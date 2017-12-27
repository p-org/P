using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface ILinearRef : IPExpr
    {
        LinearType LinearType { get; }
        Variable Variable { get; }
    }
}
