using System.Linq;

namespace Microsoft.Pc.TypeChecker
{
    internal class TupleType : PLanguageType
    {
        public TupleType(PLanguageType[] types) : base(TypeKind.Tuple)
        {
            Types = types;
        }

        public PLanguageType[] Types { get; }
        public override string OriginalRepresentation => $"({string.Join(",", Types.Select(type => type.OriginalRepresentation))})";
        public override string CanonicalRepresentation => $"({string.Join(",", Types.Select(type => type.CanonicalRepresentation))})";
    }
}