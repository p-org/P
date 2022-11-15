using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.Types
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

        public override Lazy<IReadOnlyList<PEvent>> AllowedPermissions { get; } = null;

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