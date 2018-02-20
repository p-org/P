using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class ForeignType : PLanguageType
    {
        public ForeignType(string name) : base(TypeKind.Foreign)
        {
            OriginalRepresentation = name;
            CanonicalRepresentation = name;
            _allowedPermissions = new Lazy<IReadOnlyList<PEvent>>(() =>
            {
                return new List<PEvent>();
            });
        }

        public override string OriginalRepresentation { get; }
        public override string CanonicalRepresentation { get; }

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            return otherType.Canonicalize() is ForeignType other &&
                   CanonicalRepresentation == other.CanonicalRepresentation;
        }

        public override PLanguageType Canonicalize() { return this; }

        private Lazy<IReadOnlyList<PEvent>> _allowedPermissions;
        public override IReadOnlyList<PEvent> AllowedPermissions => _allowedPermissions.Value;
    }
}