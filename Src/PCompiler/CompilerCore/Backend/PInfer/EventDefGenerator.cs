using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.Backend.Java;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.Backend.PInfer
{
    class EventDefGenerator : EventGenerator
    {
        private IEnumerable<Event> PEvents;
        private Event ConfigEvent;
        internal EventDefGenerator(ICompilerConfiguration job, string filename, IEnumerable<Event> events, Event configEvent) : base(job, filename)
        {
            PEvents = events;
            ConfigEvent = configEvent;
        }

        protected override void GenerateCodeImpl()
        {
            WriteLine($"public class {Constants.EventNamespaceName} {{");
            if (ConfigEvent != null && !PEvents.Contains(ConfigEvent))
            {
                WriteEventDecl(ConfigEvent, true);
                WriteLine();
            }
            foreach (var e in PEvents.DistinctBy(x => x.SourceLocation))
            {
               WriteEventDecl(e, true);
               WriteLine();
            }
            WriteLine("}");
        }
    }
}