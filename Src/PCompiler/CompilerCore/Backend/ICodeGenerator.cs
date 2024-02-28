﻿using System;
using System.Collections.Generic;
using Plang.Compiler.TypeChecker;

namespace Plang.Compiler.Backend
{
    public interface ICodeGenerator
    {
        /// <summary>
        /// Generate target language source files from a P project.
        /// </summary>
        /// <returns>All the source files geenrated</returns>
        IEnumerable<CompiledFile> GenerateCode(ICompilerConfiguration job, Scope globalScope);


        /// <summary>
        /// After emitting the source files generated by GenerateCode, and perhaps associated files such
        /// as build scripts or project files, compile and assemble those files into a final target.
        /// </summary>
        /// <param name="job"></param>
        public void Compile(ICompilerConfiguration job)
        {
            if (HasCompilationStage)
            {
                throw new Exception("HasCompilationState overridden but no associated Compile() method");
            }
            throw new NotImplementedException();
        }

        /// <returns>Whether this compiler has a compilation stage.  By default, they do not.  If this
        /// produces true, then `Compile` should be implemented as well.</returns>
        public virtual bool HasCompilationStage => false;
    }
}