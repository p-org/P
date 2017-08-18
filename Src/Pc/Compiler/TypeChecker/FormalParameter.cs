namespace Microsoft.Pc.TypeChecker {
    public class FormalParameter : ITypedName
    {
        public string Name { get; set; }
        public PLanguageType Type { get; set; }
    }
}