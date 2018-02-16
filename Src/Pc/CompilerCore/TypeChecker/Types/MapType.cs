using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using System.Linq;

namespace Microsoft.Pc.TypeChecker.Types
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

        public override string OriginalRepresentation =>
            $"map[{KeyType.OriginalRepresentation},{ValueType.OriginalRepresentation}]";

        public override string CanonicalRepresentation =>
            $"map[{KeyType.CanonicalRepresentation},{ValueType.CanonicalRepresentation}]";

        public override bool IsAssignableFrom(PLanguageType otherType)
        {
            // Copying semantics: both the other key and value types must be subtypes of this key/value type.
            return otherType.Canonicalize() is MapType other &&
                   KeyType.IsAssignableFrom(other.KeyType) &&
                   ValueType.IsAssignableFrom(other.ValueType);
        }

        public override PLanguageType Canonicalize()
        {
            return new MapType(KeyType.Canonicalize(), ValueType.Canonicalize());
        }

        public override IEnumerable<PEvent> AllowedPermissions()
        {
            return KeyType.AllowedPermissions().Concat(ValueType.AllowedPermissions());
        }
    }
}
