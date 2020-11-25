﻿using Plang.Compiler.Backend.Prt;
using Plang.Compiler.Backend.Coyote;
using Plang.Compiler.Backend.Rvm;
using System.Collections.Generic;

namespace Plang.Compiler.Backend
{
    public static class TargetLanguage
    {
        private static readonly IDictionary<CompilerOutput, ICodeGenerator> BackendMap =
            new Dictionary<CompilerOutput, ICodeGenerator>();

        static TargetLanguage()
        {
            RegisterCodeGenerator(CompilerOutput.Coyote, new CoyoteCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.C, new PrtCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.Rvm, new RvmCodeGenerator());
        }

        private static void RegisterCodeGenerator(CompilerOutput name, ICodeGenerator generator)
        {
            BackendMap[name] = generator;
        }

        public static ICodeGenerator GetCodeGenerator(CompilerOutput languageName)
        {
            return BackendMap[languageName];
        }
    }
}