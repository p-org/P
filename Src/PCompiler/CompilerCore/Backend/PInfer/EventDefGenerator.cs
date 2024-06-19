using System.Collections.Generic;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.PInfer
{
    class EventDefGenerator : EventGenerator
    {
        private IEnumerable<PEvent> pEvents;
        internal EventDefGenerator(ICompilerConfiguration job, string filename, IEnumerable<PEvent> events) : base(job, filename)
        {
            pEvents = events;
        }

        protected override void GenerateCodeImpl()
        {
            WriteLine($"public class {Constants.EventNamespaceName} {{");
            WriteLine("public static class EventBase {}");
            foreach (var e in pEvents)
            {
               WriteEventDecl(e, true);
               WriteLine();
            }
            WriteLine("}");
        }
    }
}