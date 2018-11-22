namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IHasScope : IPAST
    {
        Scope Scope { get; set; }
    }
}