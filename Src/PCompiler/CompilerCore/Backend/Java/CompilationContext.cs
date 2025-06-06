namespace Plang.Compiler.Backend.Java
{
    internal class CompilationContext : CompilationContextBase
    {
        public CompilationContext(ICompilerConfiguration job)
            : base(job)
        {
            Names = new NameManager(job.ProjectName, "PGEN_");
            Types = new TypeManager(Names);

            FileName = $"{ProjectName}.java";
        }

        public NameManager Names { get; }
        public TypeManager Types { get; }

        //public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;

        public string FileName { get; }

    }
}