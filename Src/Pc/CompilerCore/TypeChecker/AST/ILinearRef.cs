using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.TypeChecker.AST
{
    public interface ILinearRef : IVariableRef
    {
        LinearType LinearType { get; }
    }
}