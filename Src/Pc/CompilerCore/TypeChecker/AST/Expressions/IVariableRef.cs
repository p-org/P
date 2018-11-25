using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.Expressions
{
    public interface IVariableRef : IExprTerm
    {
        Variable Variable { get; }
    }
}