using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class TupleType : PLanguageType
    {
        public TupleType(params PLanguageType[] types) : base(TypeKind.Tuple)
        {
            Types = new List<PLanguageType>(types);
            OriginalRepresentation = $"({string.Join(",", Types.Select(type => type.OriginalRepresentation))})";
            CanonicalRepresentation = $"({string.Join(",", Types.Select(type => type.CanonicalRepresentation))})";
        }

        public IReadOnlyList<PLanguageType> Types { get; }

        public override string OriginalRepresentation { get; }

        public override string CanonicalRepresentation { get; }

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

        public override IEnumerable<PEvent> AllowedPermissions()
        {
            return Types.SelectMany(t => t.AllowedPermissions());
        }
    }
}
