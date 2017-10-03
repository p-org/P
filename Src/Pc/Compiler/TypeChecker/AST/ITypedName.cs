using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface ITypedName
    {
        string Name { get; set; }
        PLanguageType Type { get; set; }
    }
}
