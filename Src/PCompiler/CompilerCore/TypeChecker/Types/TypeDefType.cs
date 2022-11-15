using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.Types
{
    public class TypeDefType : PLanguageType
    {
        public TypeDefType(TypeDef typeDef) : base(TypeKind.TypeDef)
        {
            TypeDefDecl = typeDef;
            AllowedPermissions = TypeDefDecl.Type.Canonicalize().AllowedPermissions;
        }

        public TypeDef TypeDefDecl { get; }

        public override string OriginalRepresentation => TypeDefDecl.Name;

        public override string CanonicalRepresentation => TypeDefDecl.Type.CanonicalRepresentation;
        public override Lazy<IReadOnlyList<PEvent>> AllowedPermissions { get; }

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