using System.Collections.Generic;
using Plang.Compiler.Backend.CSharp;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.Backend.PEx;
using Plang.Compiler.Backend.Stately;
using Plang.Compiler.Backend.PVerifier;

namespace Plang.Compiler.Backend
{
    public static class TargetLanguage
    {
        private static readonly IDictionary<CompilerOutput, ICodeGenerator> BackendMap =
            new Dictionary<CompilerOutput, ICodeGenerator>();

        static TargetLanguage()
        {
            RegisterCodeGenerator(CompilerOutput.PChecker, new PCheckerCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.PObserve, new PObserveCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.Stately, new StatelyCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.PVerifier, new PVerifierCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.PEx, new PExCodeGenerator());
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