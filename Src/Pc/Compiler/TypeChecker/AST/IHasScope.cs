namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IHasScope : IPDecl
    {
        DeclarationTable Table { get; set; }
    }
}
