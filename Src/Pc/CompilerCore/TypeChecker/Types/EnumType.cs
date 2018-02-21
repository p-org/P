using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class EnumType : PLanguageType
    {
        public EnumType(PEnum enumDecl) : base(TypeKind.Enum)
        {
            EnumDecl = enumDecl;
            _allowedPermissions = new Lazy<IReadOnlyList<PEvent>>(() =>
            {
                return new List<PEvent>();
            });
        }

        public PEnum EnumDecl { get; }

        public override string OriginalRepresentation => EnumDecl.Name;
        public override string CanonicalRepresentation => EnumDecl.Name;

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            // can only assign to an enum variable of the same enum type.
            // enum declarations are always reference-equal
            return (otherType as EnumType)?.EnumDecl == EnumDecl;
        }

        public override PLanguageType Canonicalize() { return this; }

        private Lazy<IReadOnlyList<PEvent>> _allowedPermissions;
        public override IReadOnlyList<PEvent> AllowedPermissions => _allowedPermissions.Value;
    }
}
