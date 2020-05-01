namespace Plang.Compiler
{
    public interface ICompiler
    {
        void Compile(ICompilationJob job);
    }
}