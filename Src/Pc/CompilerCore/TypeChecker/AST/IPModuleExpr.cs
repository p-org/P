using System;
using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IPModuleExpr : IPAST
    {
        //Attributes of module expression
        IEventSet PrivateEvents { get; }
        IInterfaceSet PrivateInterfaces { get; }
        IEventSet Sends { get; }
        IEventSet Receives { get; }
        IInterfaceSet Creates { get; }

        //used for code generation and runtime
        IDictionary<Interface, IDictionary<Interface, Interface>> LinkMap { get; }
        IDictionary<Interface, Machine> InterfaceDef { get; }
        IDictionary<Machine, IEnumerable<Interface>> MonitorMap{ get; }

        bool IsWellFormed { get; }

        bool CheckAndPopulateAttributes(ITranslationErrorHandler handler);
    }

    public abstract class ModuleExpr : IPModuleExpr
    {
        protected IEventSet privateEvents = new EventSet();
        protected IInterfaceSet privateInterfaces = new InterfaceSet();
        protected IEventSet sends = new EventSet();
        protected IEventSet receives = new EventSet();
        protected IInterfaceSet creates = new InterfaceSet();

        protected IDictionary<Interface, IDictionary<Interface, Interface>> linkMap = new Dictionary<Interface, IDictionary<Interface, Interface>>();
        protected IDictionary<Interface, Machine> interfaceDef = new Dictionary<Interface, Machine>();
        protected IDictionary<Machine, IEnumerable<Interface>> monitorMap = new Dictionary<Machine, IEnumerable<Interface>>();

        protected bool isWellFormed = false;

        public bool IsWellFormed => isWellFormed;

        public IEventSet PrivateEvents => privateEvents;
        public IInterfaceSet PrivateInterfaces => privateInterfaces;
        public IEventSet Sends => sends;
        public IEventSet Receives => receives;
        public IInterfaceSet Creates => creates;

        public IDictionary<Interface, IDictionary<Interface, Interface>> LinkMap => linkMap;
        public IDictionary<Interface, Machine> InterfaceDef => interfaceDef;
        public IDictionary<Machine, IEnumerable<Interface>> MonitorMap => monitorMap;

        public ParserRuleContext SourceLocation { get; set;  }

        public abstract bool CheckAndPopulateAttributes(ITranslationErrorHandler handler);
    }
}

