namespace Microsoft.Pc.TypeChecker
{
    public class PrimitiveType : PLanguageType
    {
        public static readonly PrimitiveType Bool = new PrimitiveType("bool");
        public static readonly PrimitiveType Int = new PrimitiveType("int");
        public static readonly PrimitiveType Float = new PrimitiveType("float");
        public static readonly PrimitiveType Event = new PrimitiveType("event");
        public static readonly PrimitiveType Machine = new PrimitiveType("machine");
        public static readonly PrimitiveType Data = new PrimitiveType("data");
        public static readonly PrimitiveType Any = new PrimitiveType("any");
        public static readonly PrimitiveType Null = new PrimitiveType("null");

        private PrimitiveType(string name) : base(TypeKind.Base)
        {
            OriginalRepresentation = name;
            CanonicalRepresentation = name;
        }

        public override string OriginalRepresentation { get; }
        public override string CanonicalRepresentation { get; }
        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            // if this type is "any", then it's always good. Otherwise, the types have to match exactly.
            return CanonicalRepresentation.Equals("any") ||
                   CanonicalRepresentation.Equals(otherType.CanonicalRepresentation);
        }
    }
}