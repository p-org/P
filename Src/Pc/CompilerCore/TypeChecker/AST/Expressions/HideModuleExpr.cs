using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class HideEventModuleExpr : ModuleExpr
    {
        
        private IPModuleExpr module;
        private IEventSet hideEvents;
        

        public HideEventModuleExpr(ParserRuleContext sourceNode, IEnumerable<PEvent> events, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            hideEvents = new EventSet();
            hideEvents.AddEvents(events);
            this.module = module;
        }

        public override bool CheckAndPopulateAttributes(ITranslationErrorHandler handler)
        {
            if (IsWellFormed)
                return true;

            //check that component module is wellformed
            module.CheckAndPopulateAttributes(handler);

            //check if the current module is wellformed

            // 1) e \subseteq ER \intersect ES
            var receiveAndsends = module.Sends.Events.Where(ev => module.Receives.Contains(ev));
            if(!hideEvents.IsSubsetEqOf(receiveAndsends))
            {
                var @event = hideEvents.Events.Where(h => !receiveAndsends.Contains(h)).First();
                throw handler.InvalidHideEventExpr(SourceLocation, $"event {@event.Name} cannot be made private, it must belong to both receive and send set of the module");
            }

            // 2) only events in interfaces that are both created and implemented by the module can be hidden
            var interfaceImplAndNotCreated = module.Creates.Interfaces.Except(module.InterfaceDef.Keys);
            var interfaceCreatedAndNotImpl = module.InterfaceDef.Keys.Except(module.Creates.Interfaces);

            foreach(var @interface in interfaceCreatedAndNotImpl.Union(interfaceImplAndNotCreated).Where(i => hideEvents.Intersects(i.ReceivableEvents.Events)))
            {
                var @event = hideEvents.Events.Where(ev => @interface.ReceivableEvents.Contains(ev)).First();
                throw handler.InvalidHideEventExpr(SourceLocation, $"event {@event.Name} cannot be made private as interface {@interface.Name} contains this event. " +
                    $"Only events in interfaces that are both created and bound in the module can be hidden");
            }

            // 3) events received and sent by the module must not include private permissions
            var eventsReceivedAndSent = module.Sends.Events.Union(module.Receives.Events);
            foreach(var @event in eventsReceivedAndSent.Except(hideEvents.Events))
            {
                var permissionsEmbedded = @event.PayloadType.AllowedPermissions();
                foreach(var privatePermission in hideEvents.Events.Where(ev => permissionsEmbedded.Contains(ev)))
                {
                    throw handler.InvalidHideEventExpr(SourceLocation, $"event {privatePermission} cannot be made private as it belongs to allowed permission of {@event.Name} which is received or sent by the module");
                }
            }

            //module is wellformed
            isWellFormed = true;

            //populate the attributes of the module
            privateEvents.AddEvents(module.PrivateEvents.Events.Union(hideEvents.Events));
            privateInterfaces.AddInterfaces(module.PrivateInterfaces.Interfaces);
            sends.AddEvents(module.Sends.Events.Except(hideEvents.Events));
            receives.AddEvents(module.Receives.Events.Except(hideEvents.Events));
            creates.AddInterfaces(module.Creates.Interfaces);

            foreach(var monMap in module.MonitorMap)
            {
                monitorMap[monMap.Key] = monMap.Value.ToList();
            }

            foreach (var linkMapItem in module.LinkMap)
            {
                linkMap[linkMapItem.Key] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    linkMap[linkMapItem.Key].Add(localLinkMap.Key, localLinkMap.Value);
                }
            }

            foreach (var IpItem in module.InterfaceDef)
            {
                interfaceDef.Add(IpItem.Key, IpItem.Value);
            }

            return IsWellFormed;
        }
    }

    public class HideInterfaceModuleExpr : ModuleExpr
    {
        private IPModuleExpr module;
        private IEnumerable<Interface> hideInterfaces;

        public HideInterfaceModuleExpr(ParserRuleContext sourceNode, IEnumerable<Interface> interfaces, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            hideInterfaces = interfaces;
            this.module = module;
        }

        public override bool CheckAndPopulateAttributes(ITranslationErrorHandler handler)
        {
            if (IsWellFormed)
                return true;

            //check that component module is wellformed
            module.CheckAndPopulateAttributes(handler);

            //check if the current module is wellformed

            // 1) interfaces to be hidden must be both implemented and created by the module
            var interfacesImplementedAndCreated = module.Creates.Interfaces.Intersect(module.InterfaceDef.Keys);
            foreach (var @interface in hideInterfaces.Where(it => !interfacesImplementedAndCreated.Contains(it)))
            {
                throw handler.InvalidHideInterfaceExpr(SourceLocation, $"interface {@interface.Name} cannot be made private. Interface {@interface.Name} must be both created and bounded in the module");
            }

            //module is wellformed
            isWellFormed = true;

            //populate the attributes of the module
            privateEvents.AddEvents(module.PrivateEvents.Events);
            privateInterfaces.AddInterfaces(module.PrivateInterfaces.Interfaces.Union(PrivateInterfaces.Interfaces));
            sends.AddEvents(module.Sends.Events);
            receives.AddEvents(module.Receives.Events);
            creates.AddInterfaces(module.Creates.Interfaces);

            foreach (var monMap in module.MonitorMap)
            {
                monitorMap[monMap.Key] = monMap.Value.ToList();
            }

            foreach (var linkMapItem in module.LinkMap)
            {
                linkMap[linkMapItem.Key] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    linkMap[linkMapItem.Key].Add(localLinkMap.Key, localLinkMap.Value);
                }
            }

            foreach (var IpItem in module.InterfaceDef)
            {
                interfaceDef.Add(IpItem.Key, IpItem.Value);
            }

            return IsWellFormed;
        }
    }
}