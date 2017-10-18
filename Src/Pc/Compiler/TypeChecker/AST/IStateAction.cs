using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IStateAction
    {
        PEvent Trigger { get; }
    }
}
