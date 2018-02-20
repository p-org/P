using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class AssertModuleExpr : ModuleExpr
    {
       
        private IPModuleExpr module;
        private List<Machine> specMonitors;

        public AssertModuleExpr(ParserRuleContext sourceNode, List<Machine> specs, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            specMonitors = specs;
            this.module = module;
        }

        public override bool CheckAndPopulateAttributes(ITranslationErrorHandler handler)
        {
            if (IsWellFormed)
                return true;

            //check if the current module is wellformed
            module.CheckAndPopulateAttributes(handler);

            // check that the observed events of the monitor is a subset of the sends set.
            foreach(var monitor in specMonitors)
            {
                if (!monitor.Observes.IsSubsetEqOf(module.Sends))
                {
                    PEvent @event = monitor.Observes.Events.Where(e => !module.Sends.Contains(e)).First();
                    throw handler.InvalidAssertExpr(SourceLocation, monitor, @event);
                }
            }

            // check if the same monitor has already been attached
            foreach(var conflictMonitor in module.MonitorMap.Keys.Where(x => specMonitors.Contains(x)))
            {
                throw handler.InvalidAssertExpr(SourceLocation, conflictMonitor);
            }

            //module is wellformed
            isWellFormed = true;

            //populate the attributes of the module

            // initialize the monitor map
            foreach (var monitor in specMonitors)
            {
                monitorMap.Add(monitor, interfaceDef.Select(id => id.Key).ToList());
            }

            // rest of the attributes remain same
            privateEvents.AddEvents(module.PrivateEvents.Events);
            privateInterfaces.AddInterfaces(module.PrivateInterfaces.Interfaces);
            sends.AddEvents(module.Sends.Events);
            receives.AddEvents(module.Receives.Events);
            creates.AddInterfaces(module.Creates.Interfaces);

            foreach(var linkMapItem in module.LinkMap)
            {
                linkMap[linkMapItem.Key] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    linkMap[linkMapItem.Key].Add(localLinkMap.Key, localLinkMap.Value);
                }
            }

            foreach(var IpItem in module.InterfaceDef)
            {
                interfaceDef.Add(IpItem.Key, IpItem.Value);
            }

            return IsWellFormed;
        }
    }
    
}