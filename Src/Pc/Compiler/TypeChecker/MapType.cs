namespace Microsoft.Pc.TypeChecker
{
    internal class MapType : PLanguageType
    {
        public MapType(string name, PLanguageType keyType, PLanguageType valueType) : base(
            name,
            TypeKind.Map,
            $"map[{keyType.OriginalRepresentation},{valueType.OriginalRepresentation}]")
        {
            KeyType = keyType;
            ValueType = valueType;
        }

        public PLanguageType KeyType { get; }
        public PLanguageType ValueType { get; }
    }
}