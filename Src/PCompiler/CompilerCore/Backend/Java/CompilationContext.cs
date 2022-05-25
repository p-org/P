
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.Types;
using System.Collections.Generic;

namespace Plang.Compiler.Backend.Java
{
    internal class CompilationContext : CompilationContextBase
    {
        public CompilationContext(ICompilationJob job)
            : base(job)
        {
            Names = new NameManager("PGEN_");
            Types = new TypeManager();

            FileName = $"{ProjectName}.java";
            ProjectDependencies = job.ProjectDependencies.Count == 0 ? new List<string>() { ProjectName } : job.ProjectDependencies;
        }

        public NameManager Names { get; }
        public TypeManager Types { get; }

        //public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;

        public string FileName { get; }

        public IReadOnlyList<string> ProjectDependencies { get; }

    }
}