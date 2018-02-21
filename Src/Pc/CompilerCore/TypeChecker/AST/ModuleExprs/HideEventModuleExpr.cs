using System.Collections.Generic;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.ModuleExprs
{
    public class HideEventModuleExpr : IPModuleExpr
    {
        public HideEventModuleExpr(ParserRuleContext sourceNode, IEnumerable<PEvent> events, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            HideEvents = new EventSet();
            HideEvents.AddEvents(events);
            ComponentModule = module;
            ModuleInfo = null;
        }

        public IEventSet HideEvents { get; }

        public IPModuleExpr ComponentModule { get; }

        public ParserRuleContext SourceLocation { get; }

        public ModuleInfo ModuleInfo { get; set; }
    }
}