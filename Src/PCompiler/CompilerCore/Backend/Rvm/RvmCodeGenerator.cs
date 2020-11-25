using Plang.Compiler.TypeChecker;
using System.Collections.Generic;

namespace Plang.Compiler.Backend.Rvm
{
    public class RvmCodeGenerator : ICodeGenerator
    {
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope globalScope)
        {
            CompilationContext context = new CompilationContext(job);
            List<CompiledFile> sources = new List<CompiledFile>();
            sources.AddRange(
                new AjFileGenerator(context).GenerateSources(globalScope));
            sources.AddRange(
                new RvmFileGenerator(context).GenerateSources(globalScope));
            sources.AddRange(
                new EventFileGenerator(context).GenerateSources(globalScope));
            sources.Add(
                new StateBaseFileGenerator(context).GenerateSource(globalScope));
            return sources;
        }
    }
}
