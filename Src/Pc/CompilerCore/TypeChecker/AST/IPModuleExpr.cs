using System;
using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IPModuleExpr : IPAST
    {
        ModuleInfo ModuleInfo { get; }
    }

    public class ModuleInfo
    {
        //Attributes of module expression
        public IEventSet PrivateEvents = new EventSet();
        public IInterfaceSet PrivateInterfaces = new InterfaceSet();
        public IEventSet Sends = new EventSet();
        public IEventSet Receives = new EventSet();
        public IInterfaceSet Creates = new InterfaceSet();

        //used for code generation and runtime
        public IDictionary<Interface, IDictionary<Interface, Interface>> LinkMap = new Dictionary<Interface, IDictionary<Interface, Interface>>();
        public IDictionary<Interface, Machine> InterfaceDef = new Dictionary<Interface, Machine>();
        public IDictionary<Machine, IEnumerable<Interface>> MonitorMap = new Dictionary<Machine, IEnumerable<Interface>>();
    }
}

