namespace Microsoft.Pc.TypeChecker
{
    public class PLanguageType
    {
        public PLanguageType(string name, TypeKind kind, string repr)
        {
            TypeName = name;
            TypeKind = kind;
            OriginalRepresentation = repr;
        }

        /// <summary>
        ///     Unique name for the type, optional to use in generated code.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        ///     The category of type this is (eg. sequence, map, base)
        /// </summary>
        public TypeKind TypeKind { get; set; }

        /// <summary>
        ///     Original representation of the type in P.
        /// </summary>
        public string OriginalRepresentation { get; set; }
    }
}