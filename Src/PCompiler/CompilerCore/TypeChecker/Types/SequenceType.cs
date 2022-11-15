using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.Types
{
    public class SequenceType : PLanguageType
    {
        public SequenceType(PLanguageType elementType) : base(TypeKind.Sequence)
        {
            ElementType = elementType;
        }

        public PLanguageType ElementType { get; }

        public override string OriginalRepresentation => $"seq[{ElementType.OriginalRepresentation}]";
        public override string CanonicalRepresentation => $"seq[{ElementType.CanonicalRepresentation}]";

        public override Lazy<IReadOnlyList<PEvent>> AllowedPermissions => ElementType.AllowedPermissions;

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            // Copying semantics: Can assign to a sequence variable if the other sequence's elements are subtypes of this sequence's elements.
            return otherType.Canonicalize() is SequenceType other && ElementType.IsAssignableFrom(other.ElementType);
        }

        public override PLanguageType Canonicalize()
        {
            return new SequenceType(ElementType.Canonicalize());
        }
    }
}