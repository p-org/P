using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.Types
{
    public class TupleType : PLanguageType
    {
        public TupleType(params PLanguageType[] types) : base(TypeKind.Tuple)
        {
            Types = new List<PLanguageType>(types);
            OriginalRepresentation = $"({string.Join(",", Types.Select(type => type.OriginalRepresentation))})";
            CanonicalRepresentation = $"({string.Join(",", Types.Select(type => type.CanonicalRepresentation))})";
            AllowedPermissions = Types.Any(t => t.AllowedPermissions == null)
                ? null
                : new Lazy<IReadOnlyList<PEvent>>(() => Types.SelectMany(t => t.AllowedPermissions.Value).ToList());
        }

        // Lifts a TupleType into an equivalent NamedTupleType, where the names of each field are numbers
        // starting from 0 (matching how non-NamedTuples are accessed in P and extracted code).
        public NamedTupleType ToNamedTuple()
        {
            var fields = Types.Select((t, i) =>
            {
                var e = new NamedTupleEntry();
                e.Name = Constants.UnnamedTupleFieldPrefix + i;
                e.FieldNo = i;
                e.Type = t;
                return e;
            }).ToList();

            return new NamedTupleType(fields);
        }

        public IReadOnlyList<PLanguageType> Types { get; }

        public override string OriginalRepresentation { get; }

        public override string CanonicalRepresentation { get; }

        public override Lazy<IReadOnlyList<PEvent>> AllowedPermissions { get; }

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            // Tuples must be of the same size, and other tuple's fields must subtype this one's
            return otherType.Canonicalize() is TupleType other &&
                   Types.Count == other.Types.Count &&
                   Types.Zip(other.Types, (myT, otherT) => myT.IsAssignableFrom(otherT))
                       .All(x => x);
        }

        public override PLanguageType Canonicalize()
        {
            return new TupleType(Types.Select(t => t.Canonicalize()).ToArray());
        }
    }
}