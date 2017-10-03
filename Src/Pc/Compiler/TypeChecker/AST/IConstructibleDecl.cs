using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IConstructibleDecl : IPDecl
    {
        PLanguageType PayloadType { get; }
    }
}
