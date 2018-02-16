using Microsoft.Pc.TypeChecker.AST.Declarations;
using System.Collections.Generic;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class TypeDefType : PLanguageType
    {
        public TypeDefType(TypeDef typeDef) : base(TypeKind.TypeDef) { TypeDefDecl = typeDef; }

        public TypeDef TypeDefDecl { get; }

        public override string OriginalRepresentation => TypeDefDecl.Name;

        public override string CanonicalRepresentation => TypeDefDecl.Type.CanonicalRepresentation;

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            return TypeDefDecl.Type.IsAssignableFrom(otherType);
        }

        public override PLanguageType Canonicalize() { return TypeDefDecl.Type.Canonicalize(); }

        public override IEnumerable<PEvent> AllowedPermissions() { return TypeDefDecl.Type.Canonicalize().AllowedPermissions(); }
    }
}
