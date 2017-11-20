using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class NamedTupleType : TupleType
    {
        private readonly IDictionary<string, NamedTupleEntry> lookupTable;

        public NamedTupleType(IReadOnlyList<NamedTupleEntry> fields) : base(fields.Select(f => f.Type).ToArray())
        {
            Fields = fields;
            lookupTable = fields.ToDictionary(f => f.Name, f => f);
        }
        
        public string[] Names => Fields.Select(f => f.Name).ToArray();
        public IReadOnlyList<NamedTupleEntry> Fields { get; }

        public override string OriginalRepresentation =>
            $"({string.Join(",", Fields.Select(tn => $"{tn.Name}:{tn.Type.OriginalRepresentation}"))})";

        public override string CanonicalRepresentation =>
            $"({string.Join(",", Fields.Select(tn => $"{tn.Name}:{tn.Type.CanonicalRepresentation}"))})";

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            return otherType.Canonicalize() is NamedTupleType other &&
                   Fields.Count == other.Fields.Count &&
                   Names.SequenceEqual(other.Names) &&
                   Types.Zip(other.Types, (myT, otherT) => myT.IsAssignableFrom(otherT)).All(x => x);
        }

        public override PLanguageType Canonicalize()
        {
            return new NamedTupleType(Fields.Select(f => new NamedTupleEntry
                                            {
                                                Name = f.Name,
                                                Type = f.Type.Canonicalize()
                                            })
                                            .ToList());
        }

        public bool LookupEntry(string name, out NamedTupleEntry entry)
        {
            return lookupTable.TryGetValue(name, out entry);
        }
    }
}
