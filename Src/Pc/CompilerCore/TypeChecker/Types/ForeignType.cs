using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class ForeignType : PLanguageType
    {
        public ForeignType(string name) : base(TypeKind.Foreign)
        {
            OriginalRepresentation = name;
            CanonicalRepresentation = name;
        }

        public override string OriginalRepresentation { get; }
        public override string CanonicalRepresentation { get; }

        public override IReadOnlyList<PEvent> AllowedPermissions { get; } = new List<PEvent>();

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            return otherType.Canonicalize() is ForeignType other &&
                   CanonicalRepresentation == other.CanonicalRepresentation;
        }

        public override PLanguageType Canonicalize()
        {
            return this;
        }
    }
}
