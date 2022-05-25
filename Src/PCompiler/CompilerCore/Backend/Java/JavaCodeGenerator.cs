using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System;
using System.Collections.Generic;

namespace Plang.Compiler.Backend.Java {

    public class JavaCodeGenerator : ICodeGenerator
    {

        private CompilationContext context;
        private CompiledFile source;
        private Scope globalScope;
        
        /// <summary>
        /// Generates Java code for a given compilation job.
        ///
        /// Currently, we should be able to use nested classes to put everything we need in a single
        /// Java file, in a manner similar to how the C# extractor uses namesspaces.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="globalScope"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IEnumerable<CompiledFile> GenerateCode(ICompilationJob job, Scope scope)
        {
            context = new CompilationContext(job);
            source = new CompiledFile(context.FileName);
            globalScope = scope;
            
            return new List<CompiledFile> { source };
        }

        private void WriteLine(string s = "")
        {
            context.WriteLine(source.Stream, s);
        }

        private void Write(string s)
        {
            context.Write(source.Stream, s);
        }
    }
}
