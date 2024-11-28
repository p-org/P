using System.Collections.Generic;
using Plang.Compiler.Backend.CSharp;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.Backend.Stately;
using Plang.Compiler.Backend.Symbolic;
using Plang.Compiler.Backend.Uclid5;

namespace Plang.Compiler.Backend
{
    public static class TargetLanguage
    {
        private static readonly IDictionary<CompilerOutput, ICodeGenerator> BackendMap =
            new Dictionary<CompilerOutput, ICodeGenerator>();

        static TargetLanguage()
        {
            RegisterCodeGenerator(CompilerOutput.CSharp, new CSharpCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.Java, new JavaCompiler());
            RegisterCodeGenerator(CompilerOutput.Symbolic, new SymbolicCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.Stately, new StatelyCodeGenerator());
            RegisterCodeGenerator(CompilerOutput.Uclid5, new Uclid5CodeGenerator());
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