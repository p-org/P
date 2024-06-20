using System.Collections.Generic;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.PInfer
{
    class EventDefGenerator : EventGenerator
    {
        private IEnumerable<PEvent> PEvents;
        internal EventDefGenerator(ICompilerConfiguration job, string filename, IEnumerable<PEvent> events) : base(job, filename)
        {
            PEvents = events;
        }

        protected override void GenerateCodeImpl()
        {
            WriteLine($"public class {Constants.EventNamespaceName} {{");
            WriteLine("public static class EventBase {}");
            foreach (var e in PEvents)
            {
               WriteEventDecl(e, true);
               WriteLine();
            }
            WriteLine("}");
        }
    }
}