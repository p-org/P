using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Expressions;
using Microsoft.Pc.TypeChecker.AST.Statements;

namespace Microsoft.Pc.TypeChecker
{
    public class ModuleSystemTypeChecker
    {
        private readonly ITranslationErrorHandler handler;

        private ModuleSystemTypeChecker(ITranslationErrorHandler handler)
        {
            this.handler = handler;
        }

        public static void CheckWellFormedness(ITranslationErrorHandler handler, IPModuleExpr moduleExpr)
        {
            switch(moduleExpr)
            {
                case AssertModuleExpr assertExpr:
                    CheckWellFormedness(handler, assertExpr);
                    break;
                case BindModuleExpr bindExpr:
                    CheckWellFormedness(handler, bindExpr);
                    break;
                case RenameModuleExpr renameExpr:
                    CheckWellFormedness(handler, renameExpr);
                    break;
                case UnionOrComposeModuleExpr UorCExpr:
                    CheckWellFormedness(handler, UorCExpr);
                    break;
                case HideEventModuleExpr hideEExpr:
                    CheckWellFormedness(handler, hideEExpr);
                    break;
                case HideInterfaceModuleExpr hideIExpr:
                    CheckWellFormedness(handler, hideIExpr);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown module expression");
            }
        }

        public static void CheckWellFormedness(ITranslationErrorHandler handler, AssertModuleExpr assertExpr)
        {
            if (assertExpr.ModuleInfo != null)
                return;

            //check if the current module is wellformed
            CheckWellFormedness(handler, assertExpr.ComponentModule);

            var componentModuleInfo = assertExpr.ComponentModule.ModuleInfo;

            // check that the observed events of the monitor is a subset of the sends set.
            foreach (var monitor in assertExpr.SpecMonitors)
            {
                if (!monitor.Observes.IsSubsetEqOf(componentModuleInfo.Sends))
                {
                    PEvent @event = monitor.Observes.Events.Where(e => !componentModuleInfo.Sends.Contains(e)).First();
                    throw handler.InvalidAssertExpr(assertExpr.SourceLocation, monitor, @event);
                }
            }

            // check if the same monitor has already been attached
            foreach (var conflictMonitor in componentModuleInfo.MonitorMap.Keys.Where(x => assertExpr.SpecMonitors.Contains(x)))
            {
                throw handler.InvalidAssertExpr(assertExpr.SourceLocation, conflictMonitor);
            }

            assertExpr.ModuleInfo = new ModuleInfo();
            var currentModule = assertExpr.ModuleInfo;
            
            //populate the attributes of the module

            // initialize the monitor map
            foreach(var mMapItem in componentModuleInfo.MonitorMap)
            {
                currentModule.MonitorMap.Add(mMapItem.Key, mMapItem.Value.ToList());
            }
            foreach (var monitor in assertExpr.SpecMonitors)
            {
                currentModule.MonitorMap.Add(monitor, componentModuleInfo.InterfaceDef.Select(id => id.Key).ToList());
            }

            // rest of the attributes remain same
            currentModule.PrivateEvents.AddEvents(componentModuleInfo.PrivateEvents.Events);
            currentModule.PrivateInterfaces.AddInterfaces(componentModuleInfo.PrivateInterfaces.Interfaces);
            currentModule.Sends.AddEvents(componentModuleInfo.Sends.Events);
            currentModule.Receives.AddEvents(componentModuleInfo.Receives.Events);
            currentModule.Creates.AddInterfaces(componentModuleInfo.Creates.Interfaces);

            foreach (var linkMapItem in componentModuleInfo.LinkMap)
            {
                currentModule.LinkMap[linkMapItem.Key] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    currentModule.LinkMap[linkMapItem.Key].Add(localLinkMap.Key, localLinkMap.Value);
                }
            }

            foreach (var IpItem in componentModuleInfo.InterfaceDef)
            {
                currentModule.InterfaceDef.Add(IpItem.Key, IpItem.Value);
            }
        }

        public static void CheckWellFormedness(ITranslationErrorHandler handler, BindModuleExpr bindExpr)
        {
            if (bindExpr.ModuleInfo != null)
                return;

            // checked already that the bindings is a function

            // check that receive set of interface is a subset of the receive set of machine
            foreach (var binding in bindExpr.Bindings)
            {
                if (!binding.Item1.ReceivableEvents.IsSubsetEqOf(binding.Item2.Receives))
                {
                    throw handler.InvalidBindExpr(bindExpr.SourceLocation, $"receive set of {binding.Item1.Name} is not a subset of receive set of {binding.Item2.Name}");
                }

                if (!binding.Item2.PayloadType.IsAssignableFrom(binding.Item1.PayloadType))
                {
                    throw handler.InvalidBindExpr(bindExpr.SourceLocation, $"payload type of {binding.Item1.Name} is not a subtype of payload type of {binding.Item2.Name}");
                }
            }

            //populate the attributes of the module
            bindExpr.ModuleInfo = new ModuleInfo();
            var currentModuleInfo = bindExpr.ModuleInfo;
            // 1) Private events and private interfaces are empty

            // 2) Initialize Ip
            foreach (var binding in bindExpr.Bindings)
            {
                currentModuleInfo.InterfaceDef.Add(binding.Item1, binding.Item2);
            }

            // 3) Initialize Lp
            foreach (var binding in bindExpr.Bindings)
            {
                currentModuleInfo.LinkMap[binding.Item1] = new Dictionary<Interface, Interface>();
                foreach (var interfaceCreated in binding.Item2.Creates.Interfaces)
                {
                    currentModuleInfo.LinkMap[binding.Item1][interfaceCreated] = interfaceCreated;
                }
            }

            var boundMachines = bindExpr.Bindings.Select(b => b.Item2);
            // 4) compute the sends
            currentModuleInfo.Sends.AddEvents(boundMachines.SelectMany(m => m.Sends.Events));

            // 5) compute the receives
            currentModuleInfo.Receives.AddEvents(boundMachines.SelectMany(m => m.Receives.Events));

            // 6) compute the creates
            foreach (var binding in bindExpr.Bindings)
            {
                foreach (var createdInterface in binding.Item2.Creates.Interfaces)
                {
                    currentModuleInfo.Creates.AddInterface(currentModuleInfo.LinkMap[binding.Item1][createdInterface]);
                }
            }
        }

        public static void CheckWellFormedness(ITranslationErrorHandler handler, RenameModuleExpr renameExpr)
        {
            if (renameExpr.ModuleInfo != null)
                return;

            //check that component module is wellformed
            CheckWellFormedness(handler, renameExpr.ComponentModule);

            //check that the module is wellformed
            var componentModuleInfo = renameExpr.ComponentModule.ModuleInfo;

            // 1) receives set of both new and old interface must be same
            if (!renameExpr.NewInterface.ReceivableEvents.IsSame(renameExpr.OldInterface.ReceivableEvents))
            {
                throw handler.InvalidRenameExpr(renameExpr.SourceLocation, $"{renameExpr.NewInterface.Name} and {renameExpr.OldInterface.Name} must have the same receive set");
            }

            // 2) oldInterface must belong to implemented or created interface
            if (!(componentModuleInfo.Creates.Interfaces.Union(componentModuleInfo.InterfaceDef.Keys)).Contains(renameExpr.OldInterface))
            {
                throw handler.InvalidRenameExpr(renameExpr.SourceLocation, $"{renameExpr.OldInterface.Name} must belong to either created interfaces or bounded interfaces of the module");
            }

            // 3) newInterface must not belong to created and implemented interfaces.
            if ((componentModuleInfo.Creates.Interfaces.Union(componentModuleInfo.InterfaceDef.Keys)).Contains(renameExpr.NewInterface))
            {
                throw handler.InvalidRenameExpr(renameExpr.SourceLocation, $"{renameExpr.NewInterface.Name} must not belong to created interfaces or bounded interfaces of the module");
            }

            //populate the attributes of the module
            renameExpr.ModuleInfo = new ModuleInfo();
            var currentModuleInfo = renameExpr.ModuleInfo;

            // compute the new monitor map
            foreach (var monMap in componentModuleInfo.MonitorMap)
            {
                var interfaceList = new List<Interface>();
                foreach (var @interface in monMap.Value)
                {
                    if (@interface.Equals(renameExpr.OldInterface))
                    {
                        interfaceList.Add(renameExpr.NewInterface);
                    }
                    else
                    {
                        interfaceList.Add(@interface);
                    }
                }
                currentModuleInfo.MonitorMap[monMap.Key] = interfaceList;
            }

            // compute the new private interfaces
            foreach (var @interface in componentModuleInfo.PrivateInterfaces.Interfaces)
            {
                if (@interface.Equals(renameExpr.OldInterface))
                    currentModuleInfo.PrivateInterfaces.AddInterface(renameExpr.NewInterface);
                else
                    currentModuleInfo.PrivateInterfaces.AddInterface(@interface);
            }

            // compute the new interface definition map
            foreach (var interfaceDefItem in componentModuleInfo.InterfaceDef)
            {
                if (interfaceDefItem.Key.Equals(renameExpr.OldInterface))
                    currentModuleInfo.InterfaceDef.Add(renameExpr.NewInterface, interfaceDefItem.Value);
                else
                    currentModuleInfo.InterfaceDef.Add(interfaceDefItem.Key, interfaceDefItem.Value);
            }

            // compute the new link map
            foreach (var linkMapItem in componentModuleInfo.LinkMap)
            {
                Interface keyInterface;
                if (linkMapItem.Key.Equals(renameExpr.OldInterface)) keyInterface = renameExpr.NewInterface;
                else keyInterface = linkMapItem.Key;

                currentModuleInfo.LinkMap[keyInterface] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    if (localLinkMap.Value.Equals(renameExpr.OldInterface))
                        currentModuleInfo.LinkMap[keyInterface].Add(localLinkMap.Key, renameExpr.NewInterface);
                    else
                        currentModuleInfo.LinkMap[keyInterface].Add(localLinkMap.Key, localLinkMap.Value);
                }
            }

            // compute the sends
            currentModuleInfo.Sends.AddEvents(componentModuleInfo.Sends.Events);

            // compute the receives
            currentModuleInfo.Receives.AddEvents(componentModuleInfo.Receives.Events);

            // compute the creates
            foreach (var binding in currentModuleInfo.InterfaceDef)
            {
                foreach (var createdInterface in binding.Value.Creates.Interfaces)
                {
                    currentModuleInfo.Creates.AddInterface(currentModuleInfo.LinkMap[binding.Key][createdInterface]);
                }
            }
        }

        public static void CheckWellFormedness(ITranslationErrorHandler handler, UnionOrComposeModuleExpr composeExpr)
        {
            if (composeExpr.ModuleInfo != null)
                return;

            //check that all component modules are wellformed
            foreach (var module in composeExpr.ComponentModules)
            {
                CheckWellFormedness(handler, module);
            }

            //check if the current module is wellformed

            // 1) domain of interface def map is disjoint
            foreach (var module1 in composeExpr.ComponentModules)
            {
                foreach (var module2 in composeExpr.ComponentModules)
                {
                    if (module1 == module2)
                    {
                        continue;
                    }
                    else
                    {
                        var module1Info = module1.ModuleInfo;
                        var module2Info = module2.ModuleInfo;
                        var allPrivateEvents = module1Info.PrivateEvents.Events.Union(module2Info.PrivateEvents.Events);
                        var allSendAndReceiveEvents = module1Info.Sends.Events.Union(module1Info.Receives.Events.Union(module2Info.Receives.Events.Union(module2Info.Sends.Events)));

                        foreach (var @interface in module1Info.InterfaceDef.Keys.Intersect(module2Info.InterfaceDef.Keys))
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
                            var permissionsEmbedded = @event.PayloadType.AllowedPermissions;
                            foreach (var privatePermission in allPrivateEvents.Where(ev => permissionsEmbedded.Contains(ev)))
                            {
                                throw handler.InvalidCompositionExpr(module1.SourceLocation, $"private events after composition are not disjoint from permissions in events sent or received, e.g., " +
                                    $"after composition private event {privatePermission.Name} is in the permissions set of {@event.Name}");
                            }
                        }

                        var interfaceImplAndNotCreated_1 = module1Info.Creates.Interfaces.Except(module1Info.InterfaceDef.Keys);
                        var interfaceCreatedAndNotImpl_1 = module1Info.InterfaceDef.Keys.Except(module1Info.Creates.Interfaces);
                        var interfaceImplAndNotCreated_2 = module2Info.Creates.Interfaces.Except(module2Info.InterfaceDef.Keys);
                        var interfaceCreatedAndNotImpl_2 = module2Info.InterfaceDef.Keys.Except(module2Info.Creates.Interfaces);

                        foreach (var @interface in interfaceImplAndNotCreated_1.Union(interfaceCreatedAndNotImpl_1.Union(interfaceImplAndNotCreated_2.Union(interfaceCreatedAndNotImpl_2))))
                        {
                            foreach (var @event in allPrivateEvents.Where(ev => @interface.ReceivableEvents.Contains(ev)))
                            {
                                throw handler.InvalidCompositionExpr(module1.SourceLocation,
                                    $"After composition, private event {@event.Name} is in the received events of interface {@interface.Name} which is created or bound in the module");
                            }
                        }

                        // ensure also that the monitor maps are disjoint
                        foreach (var monitor in module1Info.MonitorMap.Keys.Intersect(module2Info.MonitorMap.Keys))
                        {
                            throw handler.InvalidCompositionExpr(module1.SourceLocation, $"monitor {monitor.Name} is attached in more than one modules being composed");
                        }

                        // if composition then output actions must be disjoint
                        if (composeExpr.IsComposition)
                        {
                            foreach (var @event in module1Info.Sends.Events.Intersect(module2Info.Sends.Events))
                            {
                                throw handler.InvalidCompositionExpr(module1.SourceLocation, $"output sends are not disjoint, {@event.Name} belongs to the sends of the composed module");
                            }

                            foreach (var @interface in module1Info.Creates.Interfaces.Intersect(module2Info.Creates.Interfaces))
                            {
                                throw handler.InvalidCompositionExpr(module1.SourceLocation, $"output creates are not disjoint, {@interface.Name} belongs to the creates of the composed module");
                            }
                        }
                    }
                }
            }

            composeExpr.ModuleInfo = new ModuleInfo();
            var currentModuleInfo = composeExpr.ModuleInfo;
            //populate the attributes of the module

            foreach (var module in composeExpr.ComponentModules.Select(cm => cm.ModuleInfo))
            {
                currentModuleInfo.PrivateEvents.AddEvents(module.PrivateEvents.Events);
                currentModuleInfo.PrivateInterfaces.AddInterfaces(module.PrivateInterfaces.Interfaces);

                foreach (var monMap in module.MonitorMap)
                {
                    currentModuleInfo.MonitorMap[monMap.Key] = monMap.Value.ToList();
                }

                foreach (var linkMapItem in module.LinkMap)
                {
                    currentModuleInfo.LinkMap[linkMapItem.Key] = new Dictionary<Interface, Interface>();
                    foreach (var localLinkMap in linkMapItem.Value)
                    {
                        currentModuleInfo.LinkMap[linkMapItem.Key].Add(localLinkMap.Key, localLinkMap.Value);
                    }
                }

                foreach (var IpItem in module.InterfaceDef)
                {
                    currentModuleInfo.InterfaceDef.Add(IpItem.Key, IpItem.Value);
                }
            }

            // compute all the derived attributes
            currentModuleInfo.Sends.AddEvents(composeExpr.ComponentModules.SelectMany(m => m.ModuleInfo.Sends.Events));
            currentModuleInfo.Receives.AddEvents(composeExpr.ComponentModules.SelectMany(m => m.ModuleInfo.Receives.Events));
            currentModuleInfo.Creates.AddInterfaces(composeExpr.ComponentModules.SelectMany(m => m.ModuleInfo.Creates.Interfaces));

        }

        public static void CheckWellFormedness(ITranslationErrorHandler handler, HideEventModuleExpr hideEExpr)
        {
            if (hideEExpr.ModuleInfo != null)
                return;

            //check that component module is wellformed
            CheckWellFormedness(handler, hideEExpr.ComponentModule);

            //check if the current module is wellformed
            var componentModuleInfo = hideEExpr.ComponentModule.ModuleInfo;

            // 1) e \subseteq ER \intersect ES
            var receiveAndsends = componentModuleInfo.Sends.Events.Where(ev => componentModuleInfo.Receives.Contains(ev));
            if (!hideEExpr.HideEvents.IsSubsetEqOf(receiveAndsends))
            {
                var @event = hideEExpr.HideEvents.Events.Where(h => !receiveAndsends.Contains(h)).First();
                throw handler.InvalidHideEventExpr(hideEExpr.SourceLocation, $"event {@event.Name} cannot be made private, it must belong to both receive and send set of the module");
            }

            // 2) only events in interfaces that are both created and implemented by the module can be hidden
            var interfaceImplAndNotCreated = componentModuleInfo.Creates.Interfaces.Except(componentModuleInfo.InterfaceDef.Keys);
            var interfaceCreatedAndNotImpl = componentModuleInfo.InterfaceDef.Keys.Except(componentModuleInfo.Creates.Interfaces);

            foreach (var @interface in interfaceCreatedAndNotImpl.Union(interfaceImplAndNotCreated).Where(i => hideEExpr.HideEvents.Intersects(i.ReceivableEvents.Events)))
            {
                var @event = hideEExpr.HideEvents.Events.Where(ev => @interface.ReceivableEvents.Contains(ev)).First();
                throw handler.InvalidHideEventExpr(hideEExpr.SourceLocation, $"event {@event.Name} cannot be made private as interface {@interface.Name} contains this event. " +
                    $"Only events in interfaces that are both created and bound in the module can be hidden");
            }

            // 3) events received and sent by the module must not include private permissions
            var eventsReceivedAndSent = componentModuleInfo.Sends.Events.Union(componentModuleInfo.Receives.Events);
            foreach (var @event in eventsReceivedAndSent.Except(hideEExpr.HideEvents.Events))
            {
                var permissionsEmbedded = @event.PayloadType.AllowedPermissions;
                foreach (var privatePermission in hideEExpr.HideEvents.Events.Where(ev => permissionsEmbedded.Contains(ev)))
                {
                    throw handler.InvalidHideEventExpr(hideEExpr.SourceLocation, $"event {privatePermission} cannot be made private as it belongs to allowed permission of {@event.Name} which is received or sent by the module");
                }
            }

            hideEExpr.ModuleInfo = new ModuleInfo();
            var currentModuleInfo = hideEExpr.ModuleInfo;

            //populate the attributes of the module
            currentModuleInfo.PrivateEvents.AddEvents(componentModuleInfo.PrivateEvents.Events.Union(hideEExpr.HideEvents.Events));
            currentModuleInfo.PrivateInterfaces.AddInterfaces(componentModuleInfo.PrivateInterfaces.Interfaces);
            currentModuleInfo.Sends.AddEvents(componentModuleInfo.Sends.Events.Except(hideEExpr.HideEvents.Events));
            currentModuleInfo.Receives.AddEvents(componentModuleInfo.Receives.Events.Except(hideEExpr.HideEvents.Events));
            currentModuleInfo.Creates.AddInterfaces(componentModuleInfo.Creates.Interfaces);

            foreach (var monMap in componentModuleInfo.MonitorMap)
            {
                currentModuleInfo.MonitorMap[monMap.Key] = monMap.Value.ToList();
            }

            foreach (var linkMapItem in componentModuleInfo.LinkMap)
            {
                currentModuleInfo.LinkMap[linkMapItem.Key] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    currentModuleInfo.LinkMap[linkMapItem.Key].Add(localLinkMap.Key, localLinkMap.Value);
                }
            }

            foreach (var IpItem in componentModuleInfo.InterfaceDef)
            {
                currentModuleInfo.InterfaceDef.Add(IpItem.Key, IpItem.Value);
            }
        }

        public static void CheckWellFormedness(ITranslationErrorHandler handler, HideInterfaceModuleExpr hideIExpr)
        {
            if (hideIExpr.ModuleInfo != null)
                return;

            //check that component module is wellformed
            CheckWellFormedness(handler, hideIExpr.ComponentModule);

            //check if the current module is wellformed
            var componentModuleInfo = hideIExpr.ComponentModule.ModuleInfo;

            // 1) interfaces to be hidden must be both implemented and created by the module
            var interfacesImplementedAndCreated = componentModuleInfo.Creates.Interfaces.Intersect(componentModuleInfo.InterfaceDef.Keys);
            foreach (var @interface in hideIExpr.HideInterfaces.Where(it => !interfacesImplementedAndCreated.Contains(it)))
            {
                throw handler.InvalidHideInterfaceExpr(hideIExpr.SourceLocation, $"interface {@interface.Name} cannot be made private. Interface {@interface.Name} must be both created and bounded in the module");
            }

            hideIExpr.ModuleInfo = new ModuleInfo();
            var currentModuleInfo = hideIExpr.ModuleInfo;

            //populate the attributes of the module
            currentModuleInfo.PrivateEvents.AddEvents(componentModuleInfo.PrivateEvents.Events);
            currentModuleInfo.PrivateInterfaces.AddInterfaces(componentModuleInfo.PrivateInterfaces.Interfaces.Union(hideIExpr.HideInterfaces));
            currentModuleInfo.Sends.AddEvents(componentModuleInfo.Sends.Events);
            currentModuleInfo.Receives.AddEvents(componentModuleInfo.Receives.Events);
            currentModuleInfo.Creates.AddInterfaces(componentModuleInfo.Creates.Interfaces);

            foreach (var monMap in componentModuleInfo.MonitorMap)
            {
                currentModuleInfo.MonitorMap[monMap.Key] = monMap.Value.ToList();
            }

            foreach (var linkMapItem in componentModuleInfo.LinkMap)
            {
                currentModuleInfo.LinkMap[linkMapItem.Key] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    currentModuleInfo.LinkMap[linkMapItem.Key].Add(localLinkMap.Key, localLinkMap.Value);
                }
            }

            foreach (var IpItem in componentModuleInfo.InterfaceDef)
            {
                currentModuleInfo.InterfaceDef.Add(IpItem.Key, IpItem.Value);
            }
        }
    }
}