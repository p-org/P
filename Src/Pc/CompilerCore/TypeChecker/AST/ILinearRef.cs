using Microsoft.Pc.TypeChecker.AST.Expressions;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface ILinearRef : IVariableRef
    {
        LinearType LinearType { get; }
    }
}