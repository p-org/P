using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class HideEventModuleExpr : IPModuleExpr
    {
        
        private IPModuleExpr componentModule;
        private IEventSet hideEvents;

        public IEventSet HideEvents => hideEvents;
        public IPModuleExpr ComponentModule;

        public HideEventModuleExpr(ParserRuleContext sourceNode, IEnumerable<PEvent> events, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            hideEvents = new EventSet();
            hideEvents.AddEvents(events);
            this.componentModule = module;
            ModuleInfo = null;
        }

        public ParserRuleContext SourceLocation { get; set; }

        public ModuleInfo ModuleInfo { get; set; }
    }

    public class HideInterfaceModuleExpr : IPModuleExpr
    {
        private IPModuleExpr componentModule;
        private List<Interface> hideInterfaces;

        public IPModuleExpr ComponentModule => componentModule;
        public IReadOnlyList<Interface> HideInterfaces => hideInterfaces;
        public HideInterfaceModuleExpr(ParserRuleContext sourceNode, List<Interface> interfaces, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            hideInterfaces = interfaces;
            this.componentModule = module;
            ModuleInfo = null;
        }

        public ParserRuleContext SourceLocation { get; set; }

        public ModuleInfo ModuleInfo { get; set; }
    }
}