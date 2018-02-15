using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST
{
    public interface IPModuleExpr : IPAST
    {
        //Attributes of module expression
        IEnumerable<PEvent> PrivateEvents { get; }
        IEnumerable<Interface> PrivateInterfaces { get; }
        IEnumerable<PEvent> Sends { get; }
        IEnumerable<PEvent> Receives { get; }
        IEnumerable<Interface> Creates { get; }

        //used for code generation and runtime
        IDictionary<Interface, IDictionary<Interface, Machine>> LinkMap { get; }
        IDictionary<Interface, Machine> InterfaceDef { get; }
        IDictionary<Interface, IEnumerable<Machine>> MonitorMap{ get; }

        bool IsWellFormed { get; }

        bool CheckAndPopulateAttributes(ITranslationErrorHandler handler);
    }
}

