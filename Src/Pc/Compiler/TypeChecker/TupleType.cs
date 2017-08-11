using System.Linq;

namespace Microsoft.Pc.TypeChecker
{
    internal class TupleType : PLanguageType
    {
        public TupleType(string name, PLanguageType[] types) : base(
            name,
            TypeKind.Tuple,
            $"({string.Join(",", types.Select(ty => ty.OriginalRepresentation))})")
        {
            Types = types;
        }

        public PLanguageType[] Types { get; }
    }
}