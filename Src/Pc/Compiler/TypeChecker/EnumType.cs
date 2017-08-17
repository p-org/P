namespace Microsoft.Pc.TypeChecker {
    public class EnumType : PLanguageType
    {
        public PEnum EnumDecl { get; }

        public EnumType(PEnum enumDecl) : base(TypeKind.Enum)
        {
            EnumDecl = enumDecl;
        }

        public override string OriginalRepresentation => EnumDecl.Name;
        public override string CanonicalRepresentation => EnumDecl.Name;
    }
}