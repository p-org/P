using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.Types
{
    public class EnumType : PLanguageType
    {
        public EnumType(PEnum enumDecl) : base(TypeKind.Enum)
        {
            EnumDecl = enumDecl;
        }

        public PEnum EnumDecl { get; }

        public override string OriginalRepresentation => EnumDecl.Name;
        public override string CanonicalRepresentation => EnumDecl.Name;

        public override Lazy<IReadOnlyList<PEvent>> AllowedPermissions =>
            new Lazy<IReadOnlyList<PEvent>>(() => new List<PEvent>());

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            // can only assign to an enum variable of the same enum type.
            // enum declarations are always reference-equal
            return (otherType as EnumType)?.EnumDecl == EnumDecl;
        }

        public override PLanguageType Canonicalize()
        {
            return this;
        }
    }
}