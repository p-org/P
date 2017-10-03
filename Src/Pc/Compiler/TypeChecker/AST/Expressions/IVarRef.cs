namespace Microsoft.Pc.TypeChecker.AST.Expressions
{
    public interface IVarRef : IPExpr
    {
        Variable Variable { get; }
    }
}
