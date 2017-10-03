using Microsoft.Pc.TypeChecker.AST;

namespace Microsoft.Pc.TypeChecker.Types
{
    public class EnumType : PLanguageType
    {
        public EnumType(PEnum enumDecl) : base(TypeKind.Enum) { EnumDecl = enumDecl; }

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
    }
}
