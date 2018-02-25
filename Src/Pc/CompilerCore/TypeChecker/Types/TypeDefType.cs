using System;
using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class TypeDefType : PLanguageType
    {
        private readonly Lazy<IReadOnlyList<PEvent>> allowedPermissions;

        public TypeDefType(TypeDef typeDef) : base(TypeKind.TypeDef)
        {
            TypeDefDecl = typeDef;
            allowedPermissions =
                new Lazy<IReadOnlyList<PEvent>>(() => TypeDefDecl.Type.Canonicalize().AllowedPermissions);
        }

        public TypeDef TypeDefDecl { get; }

        public override string OriginalRepresentation => TypeDefDecl.Name;

        public override string CanonicalRepresentation => TypeDefDecl.Type.CanonicalRepresentation;
        public override IReadOnlyList<PEvent> AllowedPermissions => allowedPermissions.Value;

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            return TypeDefDecl.Type.IsAssignableFrom(otherType);
        }

        public override PLanguageType Canonicalize()
        {
            return TypeDefDecl.Type.Canonicalize();
        }
    }
}
