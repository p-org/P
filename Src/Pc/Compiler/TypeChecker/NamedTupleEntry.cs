namespace Microsoft.Pc.TypeChecker {
    public class NamedTupleEntry : ITypedName
    {
        public string Name { get; set; }
        public PLanguageType Type { get; set; }
    }
}