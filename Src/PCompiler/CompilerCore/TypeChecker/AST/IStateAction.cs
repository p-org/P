using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST
{
    public interface IStateAction : IPAST
    {
        Event Trigger { get; }
    }
}