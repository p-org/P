namespace Plang.Compiler
{
    public interface ICompiler
    {
        int Compile(ICompilerConfiguration job);
    }
}