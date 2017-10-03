using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class FormalParameter : ITypedName
    {
        public string Name { get; set; }
        public PLanguageType Type { get; set; }
    }
}
