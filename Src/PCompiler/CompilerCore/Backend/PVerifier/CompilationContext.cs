namespace Plang.Compiler.Backend.PVerifier;

internal class CompilationContext : CompilationContextBase
{
    public CompilationContext(ICompilerConfiguration job) : base(job)
    {
        FileName = $"{ProjectName}.ucl";
    }

    public string FileName { get; set; }
}