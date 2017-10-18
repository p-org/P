namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IHasScope : IPAST
    {
        Scope Table { get; set; }
    }
}
