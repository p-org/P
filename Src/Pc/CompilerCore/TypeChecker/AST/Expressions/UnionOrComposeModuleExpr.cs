using System.Collections.Generic;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using System.Linq;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class UnionOrComposeModuleExpr : ModuleExpr
    {

        private IEnumerable<IPModuleExpr> modules;
        private bool isComposition = false;

        public UnionOrComposeModuleExpr(ParserRuleContext sourceNode, IEnumerable<IPModuleExpr> modules, bool isComposition)
        {
            SourceLocation = sourceNode;
            this.modules = modules;
            this.isComposition = isComposition;
        }

        public override bool CheckAndPopulateAttributes(ITranslationErrorHandler handler)
        {
            if (IsWellFormed)
                return true;

            //check that all component modules are wellformed
            foreach(var module in modules)
            {
                module.CheckAndPopulateAttributes(handler);
            }

            //check if the current module is wellformed

            // 1) domain of interface def map is disjoint
            foreach(var module1 in modules)
            {
                foreach(var module2 in modules)
                {
                    if (module1 == module2)
                    {
                        continue;
                    }
                    else
                    {
                        var allPrivateEvents = module1.PrivateEvents.Events.Union(module2.PrivateEvents.Events);
                        var allSendAndReceiveEvents = module1.Sends.Events.Union(module1.Receives.Events.Union(module2.Receives.Events.Union(module2.Sends.Events)));

                        foreach (var @interface in module1.InterfaceDef.Keys.Intersect(module2.InterfaceDef.Keys))
                        {
                            throw handler.InvalidCompositionExpr(module1.SourceLocation, $"bound interfaces after composition are not disjoint, e.g., " +
                                    $"interface {@interface.Name} is bound in both the modules being composed");
                        }
                        
                        foreach (var @event in allSendAndReceiveEvents.Intersect(allPrivateEvents))
                        {
                            throw handler.InvalidCompositionExpr(module1.SourceLocation, $"private events after composition are not disjoint from send and receives set, e.g., " +
                                    $"after composition private event {@event.Name} belongs to both private and public (sends or receives) events");
                        }

                        foreach (var @event in allSendAndReceiveEvents)
                        {
                            var permissionsEmbedded = @event.PayloadType.AllowedPermissions();
                            foreach (var privatePermission in allPrivateEvents.Where(ev => permissionsEmbedded.Contains(ev)))
                            {
                                throw handler.InvalidCompositionExpr(module1.SourceLocation, $"private events after composition are not disjoint from permissions in events sent or received, e.g., " +
                                    $"after composition private event {privatePermission.Name} is in the permissions set of {@event.Name}");
                            }
                        }

                        var interfaceImplAndNotCreated_1 = module1.Creates.Interfaces.Except(module1.InterfaceDef.Keys);
                        var interfaceCreatedAndNotImpl_1 = module1.InterfaceDef.Keys.Except(module1.Creates.Interfaces);
                        var interfaceImplAndNotCreated_2 = module2.Creates.Interfaces.Except(module2.InterfaceDef.Keys);
                        var interfaceCreatedAndNotImpl_2 = module2.InterfaceDef.Keys.Except(module2.Creates.Interfaces);

                        foreach(var @interface in interfaceImplAndNotCreated_1.Union(interfaceCreatedAndNotImpl_1.Union(interfaceImplAndNotCreated_2.Union(interfaceCreatedAndNotImpl_2))))
                        {
                            foreach(var @event in allPrivateEvents.Where(ev => @interface.ReceivableEvents.Contains(ev)))
                            {
                                throw handler.InvalidCompositionExpr(module1.SourceLocation, 
                                    $"After composition, private event {@event.Name} is in the received events of interface {@interface.Name} which is created or bound in the module");
                            }
                        }

                        // ensure also that the monitor maps are disjoint
                        foreach (var monitor in module1.MonitorMap.Keys.Intersect(module2.MonitorMap.Keys))
                        {
                            throw handler.InvalidCompositionExpr(module1.SourceLocation, $"monitor {monitor.Name} is attached in more than one modules being composed");
                        }

                        // if composition then output actions must be disjoint
                        if (isComposition)
                        {
                            foreach(var @event in module1.Sends.Events.Intersect(module2.Sends.Events))
                            {
                                throw handler.InvalidCompositionExpr(module1.SourceLocation, $"output sends are not disjoint, {@event.Name} belongs to the sends of the composed module");
                            }

                            foreach (var @interface in module1.Creates.Interfaces.Intersect(module2.Creates.Interfaces))
                            {
                                throw handler.InvalidCompositionExpr(module1.SourceLocation, $"output creates are not disjoint, {@interface.Name} belongs to the creates of the composed module");
                            }
                        }
                    }
                }
            }

            //module is wellformed
            isWellFormed = true;

            //populate the attributes of the module

            foreach (var module in modules)
            {
                privateEvents.AddEvents(module.PrivateEvents.Events);
                privateInterfaces.AddInterfaces(module.PrivateInterfaces.Interfaces);

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
            }

            // compute all the derived attributes
            sends.AddEvents(modules.SelectMany(m => m.Sends.Events));
            receives.AddEvents(modules.SelectMany(m => m.Receives.Events));
            creates.AddInterfaces(modules.SelectMany(m => m.Creates.Interfaces));


            return IsWellFormed;
        }
    }
    
}