namespace Microsoft.Pc.TypeChecker {
    public interface ITypedName
    {
        string Name { get; set; }
        PLanguageType Type { get; set; }
    }
}