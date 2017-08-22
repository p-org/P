using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Pc.TypeChecker
{
    internal class NamedTupleType : PLanguageType
    {
        private readonly IDictionary<string, NamedTupleEntry> lookupTable;

        public NamedTupleType(IReadOnlyList<NamedTupleEntry> fields) : base(TypeKind.NamedTuple)
        {
            Fields = fields;
            lookupTable = fields.ToDictionary(f => f.Name, f => f);
        }

        public IEnumerable<PLanguageType> Types => Fields.Select(f => f.Type);
        public IEnumerable<string> Names => Fields.Select(f => f.Name);
        public IReadOnlyList<NamedTupleEntry> Fields { get; }

        public override string OriginalRepresentation =>
            $"({string.Join(",", Fields.Select(tn => $"{tn.Name}:{tn.Type.OriginalRepresentation}"))})";

        public override string CanonicalRepresentation =>
            $"({string.Join(",", Fields.Select(tn => $"{tn.Name}:{tn.Type.CanonicalRepresentation}"))})";

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            var other = otherType as NamedTupleType;
            return other != null && Fields.Count == other.Fields.Count && Names.SequenceEqual(other.Names) 
                && Types.Zip(other.Types, (myT, otherT) => myT.IsAssignableFrom(otherT)).All(x => x);
        }

        public bool LookupEntry(string name, out NamedTupleEntry entry)
        {
            return lookupTable.TryGetValue(name, out entry);
        }
    }
}