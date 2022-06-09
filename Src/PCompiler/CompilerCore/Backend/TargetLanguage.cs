using Plang.Compiler.Backend.C;
using Plang.Compiler.Backend.CSharp;
using Plang.Compiler.Backend.Rvm;
using System.Collections.Generic;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.Backend.Symbolic;

namespace Plang.Compiler.Backend
{
    public static class TargetLanguage
    {
        private static readonly IDictionary<CompilerOutput, ICodeGenerator> BackendMap =
            new Dictionary<CompilerOutput, ICodeGenerator>();

        static TargetLanguage()
        {
            RegisterCodeGenerator(CompilerOutput.CSharp, new CSharpCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.C, new CCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.Java, new JavaCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.Rvm, new RvmCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.Symbolic, new SymbolicCodeGenerator());
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
