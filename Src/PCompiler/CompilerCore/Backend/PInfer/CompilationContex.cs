namespace Plang.Compiler.Backend.PInfer
{
    public class CompilationContext : CompilationContextBase
    {
        public CompilationContext(ICompilerConfiguration job) : base(job)
        {
            FileName = $"{job.ProjectName}Predicates.java";
        }

        public string FileName { get; }
    }
}