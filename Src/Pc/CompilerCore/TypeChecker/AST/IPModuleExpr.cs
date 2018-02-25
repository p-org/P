using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IPModuleExpr : IPAST
    {
        ModuleInfo ModuleInfo { get; }
    }

    public class ModuleInfo
    {
        //Attributes of module expression
        public readonly IEventSet PrivateEvents = new EventSet();
        public readonly IInterfaceSet PrivateInterfaces = new InterfaceSet();
        public readonly IEventSet Sends = new EventSet();
        public readonly IEventSet Receives = new EventSet();
        public readonly IInterfaceSet Creates = new InterfaceSet();

        //used for code generation and runtime
        public readonly IDictionary<Interface, IDictionary<Interface, Interface>> LinkMap = new Dictionary<Interface, IDictionary<Interface, Interface>>();
        public readonly IDictionary<Interface, Machine> InterfaceDef = new Dictionary<Interface, Machine>();
        public readonly IDictionary<Machine, IEnumerable<Interface>> MonitorMap = new Dictionary<Machine, IEnumerable<Interface>>();
    }
}

