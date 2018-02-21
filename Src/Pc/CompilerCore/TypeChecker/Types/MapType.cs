using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.Types
{
    internal class MapType : PLanguageType
    {
        private readonly Lazy<IReadOnlyList<PEvent>> allowedPermissions;

        public MapType(PLanguageType keyType, PLanguageType valueType) : base(TypeKind.Map)
        {
            KeyType = keyType;
            ValueType = valueType;
            allowedPermissions =
                new Lazy<IReadOnlyList<PEvent>>(() => KeyType
                                                      .AllowedPermissions.Concat(ValueType.AllowedPermissions)
                                                      .ToList());
        }

        public PLanguageType KeyType { get; }
        public PLanguageType ValueType { get; }

        public override string OriginalRepresentation =>
            $"map[{KeyType.OriginalRepresentation},{ValueType.OriginalRepresentation}]";

        public override string CanonicalRepresentation =>
            $"map[{KeyType.CanonicalRepresentation},{ValueType.CanonicalRepresentation}]";

        public override IReadOnlyList<PEvent> AllowedPermissions => allowedPermissions.Value;

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
    }
}
