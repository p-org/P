using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.PInfer
{
    public class CompilationContext : CompilationContextBase
    {
        public CompilationContext(ICompilerConfiguration job) : base(job)
        {
            FileName = $"{job.ProjectName}.java";
            PredicateMap = [];
            FunctionMap = [];
        }

        public string FileName { get; }
        public Dictionary<IPredicate, string> PredicateMap { get; }
        public Dictionary<Function, string> FunctionMap { get; }
    }
}