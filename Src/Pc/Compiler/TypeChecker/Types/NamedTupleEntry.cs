using Microsoft.Pc.TypeChecker.AST;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class NamedTupleEntry : ITypedName
    {
        public string Name { get; set; }
        public PLanguageType Type { get; set; }
    }
}
