using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;

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
}

