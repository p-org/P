using System.Collections.Generic;
using System;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class BindModuleExpr : IPModuleExpr
    {
        private IEnumerable<PEvent> privateEvents = new List<PEvent>();
        private IEnumerable<Interface> privateInterfaces = new List<Interface>();
        private IEnumerable<PEvent> sends = new List<PEvent>();
        private IEnumerable<PEvent> receives = new List<PEvent>();
        private IEnumerable<Interface> creates = new List<Interface>();

        private IDictionary<Interface, IDictionary<Interface, Machine>> linkMap = new Dictionary<Interface, IDictionary<Interface, Machine>>();
        private IDictionary<Interface, Machine> interfaceDef = new Dictionary<Interface, Machine>();
        private IDictionary<Interface, IEnumerable<Machine>> monitorMap = new Dictionary<Interface, IEnumerable<Machine>>();

        private bool isWellFormed = false;
        private IEnumerable<Tuple<Interface, Machine>> bindings;

        public BindModuleExpr(ParserRuleContext sourceNode, List<Tuple<Interface, Machine>> bindings)
        {
            SourceLocation = sourceNode;
            this.bindings = bindings;
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

        /*
            private static void ValidateInterfaces(ITranslationErrorHandler handler, Machine machine)
        {
            foreach (Interface machineInterface in machine.Interfaces)
            {
                if (!machine.PayloadType.IsAssignableFrom(machineInterface.PayloadType))
                {
                    // TODO: add special "invalid machine interface" error
                    throw handler.TypeMismatch(machine.StartState.Entry?.SourceLocation ?? machine.SourceLocation,
                                               machine.PayloadType,
                                               machineInterface.PayloadType);
                }
            }
        }
         */ 
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
 
 