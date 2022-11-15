using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.Types
{
    public abstract class PLanguageType
    {
        protected PLanguageType(TypeKind kind)
        {
            TypeKind = kind;
        }

        /// <summary>
        ///     The category of type this is (eg. sequence, map, base)
        /// </summary>
        public TypeKind TypeKind { get; }

        /// <summary>
        ///     Original representation of the type in P.
        /// </summary>
        public abstract string OriginalRepresentation { get; }

        /// <summary>
        ///     Representation of the type with typedefs and event sets expanded.
        /// </summary>
        public abstract string CanonicalRepresentation { get; }

        /// <summary>
        ///     represents the permissions embedded in a type
        /// </summary>
        public abstract Lazy<IReadOnlyList<PEvent>> AllowedPermissions { get; }

        public abstract bool IsAssignableFrom(PLanguageType otherType);

        public bool IsSameTypeAs(PLanguageType otherType)
        {
            return IsAssignableFrom(otherType) && otherType.IsAssignableFrom(this);
        }

        public override bool Equals(object obj)
        {
            return !(obj is null) && (this == obj || obj.GetType() == GetType() && IsSameTypeAs((PLanguageType)obj));
        }

        public override int GetHashCode()
        {
            return CanonicalRepresentation.GetHashCode();
        }

        public abstract PLanguageType Canonicalize();

        public static bool TypeIsOfKind(PLanguageType type, TypeKind kind)
        {
            return type.Canonicalize().TypeKind.Equals(kind);
        }
    }
}