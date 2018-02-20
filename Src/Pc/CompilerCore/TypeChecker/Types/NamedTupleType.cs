using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class NamedTupleType : TupleType
    {
        private readonly IDictionary<string, NamedTupleEntry> lookupTable;

        public NamedTupleType(IReadOnlyList<NamedTupleEntry> fields) : base(fields.Select(f => f.Type).ToArray())
        {
            Fields = fields;
            lookupTable = fields.ToDictionary(f => f.Name, f => f);
            OriginalRepresentation =
                $"({string.Join(",", Fields.Select(tn => $"{tn.Name}:{tn.Type.OriginalRepresentation}"))})";
            CanonicalRepresentation =
                $"({string.Join(",", Fields.Select(tn => $"{tn.Name}:{tn.Type.CanonicalRepresentation}"))})";
            _allowedPermissions = new Lazy<IReadOnlyList<PEvent>>(() =>
            {
                return Fields.SelectMany(f => f.Type.AllowedPermissions).ToList();
            });
        }

        public IEnumerable<string> Names => Fields.Select(f => f.Name);
        public IReadOnlyList<NamedTupleEntry> Fields { get; }

        public override string OriginalRepresentation { get; }
        public override string CanonicalRepresentation { get; }

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

        private Lazy<IReadOnlyList<PEvent>> _allowedPermissions;
        public override IReadOnlyList<PEvent> AllowedPermissions => _allowedPermissions.Value;
    }
}
