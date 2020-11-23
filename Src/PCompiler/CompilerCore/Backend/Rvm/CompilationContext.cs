using Plang.Compiler.TypeChecker.AST.Declarations;
using System.Collections.Generic;
using System.IO;

namespace Plang.Compiler.Backend.Rvm
{
    internal class CompilationContext : CompilationContextBase
    {
        public CompilationContext(ICompilationJob job)
            : base(job)
        {
            Names = new RvmNameManager("PGEN_");

            ProjectDependencies = job.ProjectDependencies.Count == 0? new List<string>() { ProjectName } : job.ProjectDependencies;
        }

        public RvmNameManager Names { get; }

        public string GetAjFileName(Machine machine)
        {
            // AspectJ file name should match aspect class name
            string aspectClassName = Names.GetAspectClassName(machine);
            return $"{aspectClassName}.aj";
        }

        public string GetRvmFileName(Machine machine)
        {
            string rvmSpecName = Names.GetRvmSpecName(machine);
            return $"{rvmSpecName}.rvm";
        }

        public IReadOnlyList<string> ProjectDependencies { get; }
    }
}
