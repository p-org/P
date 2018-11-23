namespace Plang.Compiler.TypeChecker.AST
{
    public interface IPDecl : IPAST
    {
        string Name { get; }
    }
}