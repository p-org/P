namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IHasScope : IPDecl
    {
        Scope Table { get; set; }
    }
}
