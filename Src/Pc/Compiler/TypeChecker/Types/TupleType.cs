using System.Linq;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class TupleType : PLanguageType
    {
        public TupleType(PLanguageType[] types) : base(TypeKind.Tuple) { Types = types; }

        public PLanguageType[] Types { get; }

        public override string OriginalRepresentation =>
            $"({string.Join(",", Types.Select(type => type.OriginalRepresentation))})";

        public override string CanonicalRepresentation =>
            $"({string.Join(",", Types.Select(type => type.CanonicalRepresentation))})";

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            // Tuples must be of the same size, and other tuple's fields must subtype this one's 
            var other = otherType.Canonicalize() as TupleType;
            return other != null &&
                   Types.Length == other.Types.Length &&
                   Types
                       .Zip(other.Types, (myT, otherT) => myT.IsAssignableFrom(otherT))
                       .All(x => x);
        }

        public override PLanguageType Canonicalize()
        {
            return new TupleType(Types.Select(t => t.Canonicalize()).ToArray());
        }
    }
}
