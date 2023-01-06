namespace Plang.Compiler
{
    public interface ICompiler
    {
        int Compile(ICompilationJob job);
    }
}