using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.Types
{
    public class SetType : PLanguageType
    {
        public SetType(PLanguageType elementType) : base(TypeKind.Set)
        {
            ElementType = elementType;
        }

        public PLanguageType ElementType { get; }

        public override string OriginalRepresentation => $"set[{ElementType.OriginalRepresentation}]";
        public override string CanonicalRepresentation => $"set[{ElementType.CanonicalRepresentation}]";

        public override Lazy<IReadOnlyList<PEvent>> AllowedPermissions => ElementType.AllowedPermissions;

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            // Copying semantics: both the other key and value types must be subtypes of this key/value type.
            return otherType.Canonicalize() is SetType other &&
                   ElementType.IsAssignableFrom(other.ElementType);
        }

        public override PLanguageType Canonicalize()
        {
            return new SetType(ElementType.Canonicalize());
        }
    }
}