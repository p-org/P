using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class BindModuleExpr : IPModuleExpr
    {
        private IEventSet privateEvents = new EventSet();
        private IInterfaceSet privateInterfaces = new InterfaceSet();
        private IEventSet sends = new EventSet();
        private IEventSet receives = new EventSet();
        private IInterfaceSet creates = new InterfaceSet();

        private IDictionary<Interface, IDictionary<Interface, Interface>> linkMap = new Dictionary<Interface, IDictionary<Interface, Interface>>();
        private IDictionary<Interface, Machine> interfaceDef = new Dictionary<Interface, Machine>();
        private IDictionary<Machine, IEnumerable<Interface>> monitorMap = new Dictionary<Machine, IEnumerable<Interface>>();

        private bool isWellFormed = false;
        private IEnumerable<Tuple<Interface, Machine>> bindings;

        public BindModuleExpr(ParserRuleContext sourceNode, List<Tuple<Interface, Machine>> bindings)
        {
            SourceLocation = sourceNode;
            this.bindings = bindings;
        }

        public bool IsWellFormed => isWellFormed;

        public IEventSet PrivateEvents => privateEvents;
        public IInterfaceSet PrivateInterfaces => privateInterfaces;
        public IEventSet Sends => sends;
        public IEventSet Receives => receives;
        public IInterfaceSet Creates => creates;

        public IDictionary<Interface, IDictionary<Interface, Interface>> LinkMap => linkMap;
        public IDictionary<Interface, Machine> InterfaceDef => interfaceDef;
        public IDictionary<Machine, IEnumerable<Interface>> MonitorMap => monitorMap;
        public ParserRuleContext SourceLocation { get; }
 
        public bool CheckAndPopulateAttributes(ITranslationErrorHandler handler)
        {
            if (IsWellFormed)
                return true;

            // checked already that the bindings is a function

            // check that receive set of interface is a subset of the receive set of machine
            foreach(var binding in bindings)
            {
                if(!binding.Item1.ReceivableEvents.IsSubsetEqOf(binding.Item2.Receives))
                {
                    throw handler.InvalidBindExpr(SourceLocation, $"receive set of {binding.Item1.Name} is not a subset of receive set of {binding.Item2.Name}");
                }

                if (!binding.Item2.PayloadType.IsAssignableFrom(binding.Item1.PayloadType))
                {
                    throw handler.InvalidBindExpr(SourceLocation, $"payload type of {binding.Item1.Name} is not a subtype of payload type of {binding.Item2.Name}");
                }
            }

            // Module is wellformed
            isWellFormed = true;

            //populate the attributes of the module
            
            // 1) Private events and private interfaces are empty
            
            // 2) Initialize Ip
            foreach(var binding in bindings)
            {
                InterfaceDef.Add(binding.Item1, binding.Item2);
            }

            // 3) Initialize Lp
            foreach(var binding in bindings)
            {
                LinkMap[binding.Item1] = new Dictionary<Interface, Interface>();
                foreach(var interfaceCreated in binding.Item2.Creates.Interfaces)
                {
                    LinkMap[binding.Item1][interfaceCreated] = interfaceCreated;
                }
            }

            var boundMachines = bindings.Select(b => b.Item2);
            // 4) compute the sends
            boundMachines.Select(m => m.Sends.Events.Select(sends.AddEvent));

            // 5) compute the receives
            boundMachines.Select(m => m.Receives.Events.Select(receives.AddEvent));

            // 6) compute the creates
            foreach (var binding in bindings)
            {
                foreach(var createdInterface in binding.Item2.Creates.Interfaces)
                {
                    creates.AddInterface(LinkMap[binding.Item1][createdInterface]);
                }
            }

            //module is wellformed
            return IsWellFormed;
        }
    }
    
}
 
 