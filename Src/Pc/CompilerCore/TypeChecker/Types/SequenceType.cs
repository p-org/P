using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class SequenceType : PLanguageType
    {
        public SequenceType(PLanguageType elementType) : base(TypeKind.Sequence)
        {
            ElementType = elementType;
            _allowedPermissions = new Lazy<IReadOnlyList<PEvent>>(() =>
            {
                return ElementType.AllowedPermissions;
            });
        }


        public PLanguageType ElementType { get; }

        public override string OriginalRepresentation => $"seq[{ElementType.OriginalRepresentation}]";
        public override string CanonicalRepresentation => $"seq[{ElementType.CanonicalRepresentation}]";

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            // Copying semantics: Can assign to a sequence variable if the other sequence's elements are subtypes of this sequence's elements.
            return otherType.Canonicalize() is SequenceType other && ElementType.IsAssignableFrom(other.ElementType);
        }

        public override PLanguageType Canonicalize() { return new SequenceType(ElementType.Canonicalize()); }

        private Lazy<IReadOnlyList<PEvent>> _allowedPermissions;
        public override IReadOnlyList<PEvent> AllowedPermissions => _allowedPermissions.Value;

    }
}
