using System.Collections.Generic;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST
{
    public interface IPModuleExpr : IPAST
    {
        ModuleInfo ModuleInfo { get; }
    }

    public class ModuleInfo
    {
        public readonly IInterfaceSet Creates = new InterfaceSet();
        public readonly IDictionary<Interface, Machine> InterfaceDef = new Dictionary<Interface, Machine>();

        //used for code generation and runtime
        public readonly IDictionary<Interface, IDictionary<Interface, Interface>> LinkMap =
            new Dictionary<Interface, IDictionary<Interface, Interface>>();

        public readonly IDictionary<Machine, IEnumerable<Interface>> MonitorMap =
            new Dictionary<Machine, IEnumerable<Interface>>();

        //Attributes of module expression
        public readonly IEventSet PrivateEvents = new EventSet();

        public readonly IInterfaceSet PrivateInterfaces = new InterfaceSet();
        public readonly IEventSet Receives = new EventSet();
        public readonly IEventSet Sends = new EventSet();
    }
}