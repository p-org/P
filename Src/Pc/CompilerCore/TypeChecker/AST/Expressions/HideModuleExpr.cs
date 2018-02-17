using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class HideEventModuleExpr : IPModuleExpr
    {
        private IEnumerable<PEvent> privateEvents = new List<PEvent>();
        private IEnumerable<Interface> privateInterfaces = new List<Interface>();
        private IEnumerable<PEvent> sends = new List<PEvent>();
        private IEnumerable<PEvent> receives = new List<PEvent>();
        private IEnumerable<Interface> creates = new List<Interface>();

        private IDictionary<Interface, IDictionary<Interface, Interface>> linkMap = new Dictionary<Interface, IDictionary<Interface, Interface>>();
        private IDictionary<Interface, Machine> interfaceDef = new Dictionary<Interface, Machine>();
        private IDictionary<Interface, IEnumerable<Machine>> monitorMap = new Dictionary<Interface, IEnumerable<Machine>>();

        private IPModuleExpr module;
        private IEnumerable<PEvent> hideEvents;
        private bool isWellFormed = false;

        public HideEventModuleExpr(ParserRuleContext sourceNode, IEnumerable<PEvent> events, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            hideEvents = events;
            this.module = module;
        }

        public bool IsWellFormed => isWellFormed;

        public IEnumerable<PEvent> PrivateEvents => privateEvents;
        public IEnumerable<Interface> PrivateInterfaces => privateInterfaces;
        public IEnumerable<PEvent> Sends => sends;
        public IEnumerable<PEvent> Receives => receives;
        public IEnumerable<Interface> Creates => creates;

        public IDictionary<Interface, IDictionary<Interface, Interface>> LinkMap => linkMap;
        public IDictionary<Interface, Machine> InterfaceDef => interfaceDef;
        public IDictionary<Interface, IEnumerable<Machine>> MonitorMap => monitorMap;
        public ParserRuleContext SourceLocation { get; }

        public bool CheckAndPopulateAttributes(ITranslationErrorHandler handler)
        {
            if (IsWellFormed)
                return true;

            //check that all component modules are wellformed

            //check if the current module is wellformed

            //populate the attributes of the module



            //module is wellformed
            isWellFormed = true;
            return IsWellFormed;
        }
    }

    public class HideInterfaceModuleExpr : IPModuleExpr
    {
        private IEnumerable<PEvent> privateEvents = new List<PEvent>();
        private IEnumerable<Interface> privateInterfaces = new List<Interface>();
        private IEnumerable<PEvent> sends = new List<PEvent>();
        private IEnumerable<PEvent> receives = new List<PEvent>();
        private IEnumerable<Interface> creates = new List<Interface>();

        private IDictionary<Interface, IDictionary<Interface, Machine>> linkMap = new Dictionary<Interface, IDictionary<Interface, Machine>>();
        private IDictionary<Interface, Machine> interfaceDef = new Dictionary<Interface, Machine>();
        private IDictionary<Interface, IEnumerable<Machine>> monitorMap = new Dictionary<Interface, IEnumerable<Machine>>();

        private IPModuleExpr module;
        private IEnumerable<Interface> hideInterfaces;
        private bool isWellFormed = false;

        public HideInterfaceModuleExpr(ParserRuleContext sourceNode, IEnumerable<Interface> interfaces, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            hideInterfaces = interfaces;
            this.module = module;
        }

        public bool IsWellFormed => isWellFormed;

        public IEnumerable<PEvent> PrivateEvents => privateEvents;
        public IEnumerable<Interface> PrivateInterfaces => privateInterfaces;
        public IEnumerable<PEvent> Sends => sends;
        public IEnumerable<PEvent> Receives => receives;
        public IEnumerable<Interface> Creates => creates;

        public IDictionary<Interface, IDictionary<Interface, Machine>> LinkMap => linkMap;
        public IDictionary<Interface, Machine> InterfaceDef => interfaceDef;
        public IDictionary<Interface, IEnumerable<Machine>> MonitorMap => monitorMap;
        public ParserRuleContext SourceLocation { get; }

        public bool CheckAndPopulateAttributes(ITranslationErrorHandler handler)
        {
            if (IsWellFormed)
                return true;

            //check that all component modules are wellformed

            //check if the current module is wellformed

            //populate the attributes of the module

            //module is wellformed
            isWellFormed = true;
            return IsWellFormed;
        }
    }
}