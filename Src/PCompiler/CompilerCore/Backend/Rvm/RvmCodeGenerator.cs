/* Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved. */

using System.Collections.Generic;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler.Backend.Rvm
{
    public class RvmCodeGenerator : ICodeGenerator
    {
        public IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope)
        {
            var context = new CompilationContext(job);
            var sources = new List<CompiledFile>();
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
