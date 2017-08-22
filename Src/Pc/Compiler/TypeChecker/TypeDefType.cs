namespace Microsoft.Pc.TypeChecker {
    public class TypeDefType : PLanguageType
    {
        public TypeDef TypeDefDecl { get; }

        public TypeDefType(TypeDef typeDef) : base(TypeKind.TypeDef)
        {
            TypeDefDecl = typeDef;
        }

        public override string OriginalRepresentation => TypeDefDecl.Name;

        public override string CanonicalRepresentation => TypeDefDecl.Type.CanonicalRepresentation;
        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            return TypeDefDecl.Type.IsAssignableFrom(otherType);
        }
    }
}