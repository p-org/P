namespace Plang.Compiler.TypeChecker.AST
{
    public interface IHasScope : IPAST
    {
        Scope Scope { get; set; }
    }
}