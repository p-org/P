using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public interface IVariableRef : IExprTerm
    {
        Variable Variable { get; }
    }
}