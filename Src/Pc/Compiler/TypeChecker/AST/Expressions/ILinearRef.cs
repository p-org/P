namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public interface ILinearRef : IVarRef
    {
        LinearType LinearType { get; }
    }
}
