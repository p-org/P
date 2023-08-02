namespace Plang.Compiler.Backend.Stately;

internal class CompilationContext : CompilationContextBase
{
    public CompilationContext(ICompilerConfiguration job) : base(job)
    {
        FileName = $"{ProjectName}.ts";
    }

    public string FileName { get; set; }
}