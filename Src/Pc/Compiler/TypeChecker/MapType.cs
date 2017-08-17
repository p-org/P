namespace Microsoft.Pc.TypeChecker
{
    internal class MapType : PLanguageType
    {
        public MapType(PLanguageType keyType, PLanguageType valueType) : base(TypeKind.Map)
        {
            KeyType = keyType;
            ValueType = valueType;
        }

        public PLanguageType KeyType { get; }
        public PLanguageType ValueType { get; }
        public override string OriginalRepresentation => $"map[{KeyType.OriginalRepresentation},{ValueType.OriginalRepresentation}]";
        public override string CanonicalRepresentation => $"map[{KeyType.CanonicalRepresentation},{ValueType.CanonicalRepresentation}]";
    }
}