using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.Types
{
    public class NamedTupleType : PLanguageType
    {
        private readonly IDictionary<string, NamedTupleEntry> lookupTable;

        public NamedTupleType(IReadOnlyList<NamedTupleEntry> fields) : base(TypeKind.NamedTuple)
        {
            Types = new List<PLanguageType>(fields.Select(f => f.Type).ToArray());
            Fields = fields;
            lookupTable = fields.ToDictionary(f => f.Name, f => f);
            OriginalRepresentation =
                $"({string.Join(",", Fields.Select(tn => $"{tn.Name}:{tn.Type.OriginalRepresentation}"))})";
            CanonicalRepresentation =
                $"({string.Join(",", Fields.Select(tn => $"{tn.Name}:{tn.Type.CanonicalRepresentation}"))})";
            AllowedPermissions = Fields.Any(f => f.Type.AllowedPermissions == null)
                ? null
                : new Lazy<IReadOnlyList<PEvent>>(
                    () => Fields.SelectMany(f => f.Type.AllowedPermissions.Value).ToList());
        }

        public IEnumerable<string> Names => Fields.Select(f => f.Name);
        public IReadOnlyList<PLanguageType> Types { get; }
        public IReadOnlyList<NamedTupleEntry> Fields { get; }

        public override string OriginalRepresentation { get; }
        public override string CanonicalRepresentation { get; }

        public override Lazy<IReadOnlyList<PEvent>> AllowedPermissions { get; }

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
                    FieldNo = f.FieldNo,
                    Type = f.Type.Canonicalize()
                })
                .ToArray());
        }

        public bool LookupEntry(string name, out NamedTupleEntry entry)
        {
            return lookupTable.TryGetValue(name, out entry);
        }
    }
}