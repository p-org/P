/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */

using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.Rvm
{
    internal class CompilationContext : CompilationContextBase
    {
        public CompilationContext(ICompilerConfiguration job)
            : base(job)
        {
            Names = new RvmNameManager("PGEN_");

            ProjectDependencies = job.ProjectDependencies.Count == 0? new List<string>() { ProjectName } : job.ProjectDependencies;
        }

        public RvmNameManager Names { get; }

        public string GetAjFileName()
        {
            // AspectJ file name should match aspect class name
            var aspectClassName = Names.GetAspectClassName();
            return $"{aspectClassName}.aj";
        }

        public string GetRvmFileName(Machine machine)
        {
            var rvmSpecName = Names.GetRvmSpecName(machine);
            return $"{rvmSpecName}.rvm";
        }

        public IList<string> ProjectDependencies { get; }
    }
}
