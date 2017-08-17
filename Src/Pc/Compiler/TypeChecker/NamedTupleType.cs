using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Pc.TypeChecker
{
    internal class NamedTupleType : PLanguageType
    {
        public NamedTupleType(IReadOnlyList<TypedName> fields) : base(TypeKind.NamedTuple)
        {
            Fields = fields;
        }

        public IEnumerable<PLanguageType> Types => Fields.Select(f => f.Type);
        public IEnumerable<string> Names => Fields.Select(f => f.Name);
        public IReadOnlyList<TypedName> Fields { get; }

        public override string OriginalRepresentation =>
            $"({string.Join(",", Fields.Select(tn => $"{tn.Name}:{tn.Type.OriginalRepresentation}"))})";

        public override string CanonicalRepresentation =>
            $"({string.Join(",", Fields.Select(tn => $"{tn.Name}:{tn.Type.CanonicalRepresentation}"))})";
    }

    public class TypedName
    {
        public string Name { get; set; }
        public PLanguageType Type { get; set; }
    }
}