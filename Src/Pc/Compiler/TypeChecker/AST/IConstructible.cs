using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IConstructible
    {
        PLanguageType PayloadType { get; }
    }
}
