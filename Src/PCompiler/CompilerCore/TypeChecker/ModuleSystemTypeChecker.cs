using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.ModuleExprs;

namespace Plang.Compiler.TypeChecker
{
    public class ModuleSystemTypeChecker
    {
        private readonly Scope globalScope;
        private readonly ITranslationErrorHandler handler;

        public ModuleSystemTypeChecker(ITranslationErrorHandler handler, Scope globalScope)
        {
            this.handler = handler;
            this.globalScope = globalScope;
        }

        private IEnumerable<PEvent> GetPermissions(IEnumerable<PEvent> allowed)
        {
            if (allowed == null)
            {
                return globalScope.UniversalEventSet.Events;
            }

            return allowed;
        }

        public void CheckWellFormedness(IPModuleExpr moduleExpr)
        {
            switch (moduleExpr)
            {
                case AssertModuleExpr assertExpr:
                    CheckWellFormedness(assertExpr);
                    break;

                case BindModuleExpr bindExpr:
                    CheckWellFormedness(bindExpr);
                    break;

                case RenameModuleExpr renameExpr:
                    CheckWellFormedness(renameExpr);
                    break;

                case UnionOrComposeModuleExpr uOrCExpr:
                    CheckWellFormedness(uOrCExpr);
                    break;

                case HideEventModuleExpr hideEExpr:
                    CheckWellFormedness(hideEExpr);
                    break;

                case HideInterfaceModuleExpr hideIExpr:
                    CheckWellFormedness(hideIExpr);
                    break;

                default:
                    throw handler.InternalError(moduleExpr.SourceLocation,
                        new ArgumentOutOfRangeException(nameof(moduleExpr)));
            }
        }

        private void CheckWellFormedness(AssertModuleExpr assertExpr)
        {
            if (assertExpr.ModuleInfo != null)
            {
                return;
            }

            //check if the current module is wellformed
            CheckWellFormedness(assertExpr.ComponentModule);

            var componentModuleInfo = assertExpr.ComponentModule.ModuleInfo;

            // check that the observed events of the monitor is a subset of the sends set.
            foreach (var monitor in assertExpr.SpecMonitors)
            {
                if (!monitor.Observes.IsSubsetEqOf(componentModuleInfo.Sends))
                {
                    var @event = monitor.Observes.Events.First(e => !componentModuleInfo.Sends.Contains(e));
                    throw handler.InvalidAssertExpr(assertExpr.SourceLocation, monitor, @event);
                }
            }

            // check if the same monitor has already been attached
            foreach (var conflictMonitor in componentModuleInfo.MonitorMap.Keys.Where(
                         x => assertExpr.SpecMonitors.Contains(x)))
            {
                throw handler.InvalidAssertExpr(assertExpr.SourceLocation, conflictMonitor);
            }

            assertExpr.ModuleInfo = new ModuleInfo();
            var currentModule = assertExpr.ModuleInfo;

            //populate the attributes of the module

            // initialize the monitor map
            foreach (var mMapItem in componentModuleInfo.MonitorMap)
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

            foreach (var linkMapItem in componentModuleInfo
                         .LinkMap)
            {
                currentModule.LinkMap[linkMapItem.Key] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    currentModule.LinkMap[linkMapItem.Key].Add(localLinkMap.Key, localLinkMap.Value);
                }
            }

            foreach (var ipItem in componentModuleInfo.InterfaceDef)
            {
                currentModule.InterfaceDef.Add(ipItem.Key, ipItem.Value);
            }
        }

        internal void CheckRefinementTest(RefinementTest test)
        {
            //check that the test module is closed with respect to creates
            var notImplementedInterface =
                test.LeftModExpr.ModuleInfo.Creates.Interfaces.Where(i =>
                    !test.LeftModExpr.ModuleInfo.InterfaceDef.Keys.Contains(i));
            var @interface = notImplementedInterface as Interface[] ?? notImplementedInterface.ToArray();
            if (@interface.Any())
            {
                throw handler.NotClosed(test.SourceLocation,
                    $"LHS test module is not closed with respect to created interfaces; interface {@interface.First().Name} is created but not implemented inside the module");
            }

            //check that the test module main machine exists
            var hasMainMachine =
                test.LeftModExpr.ModuleInfo.InterfaceDef.Values.Any(m => m.Name == test.Main && !m.IsSpec);
            if (!hasMainMachine)
            {
                throw handler.NoMain(test.SourceLocation,
                    $"machine {test.Main} does not exist in the LHS test module");
            }

            //check that the test module is closed with respect to creates
            notImplementedInterface =
                test.RightModExpr.ModuleInfo.Creates.Interfaces.Where(i =>
                    !test.RightModExpr.ModuleInfo.InterfaceDef.Keys.Contains(i));
            @interface = notImplementedInterface as Interface[] ?? notImplementedInterface.ToArray();
            if (@interface.Any())
            {
                throw handler.NotClosed(test.SourceLocation,
                    $"RHS test module is not closed with respect to created interfaces; interface {@interface.First().Name} is created but not implemented inside the module");
            }

            //check that the test module main machine exists
            hasMainMachine =
                test.RightModExpr.ModuleInfo.InterfaceDef.Values.Any(m => m.Name == test.Main && !m.IsSpec);
            if (!hasMainMachine)
            {
                throw handler.NoMain(test.SourceLocation,
                    $"machine {test.Main} does not exist in the RHS test module");
            }

            //todo: Implement the checks with respect to refinement relation
            throw new NotImplementedException();
        }

        internal void CheckSafetyTest(SafetyTest test)
        {
            //check that the test module is closed with respect to creates
            var notImplementedInterface =
                test.ModExpr.ModuleInfo.Creates.Interfaces.Where(i =>
                    !test.ModExpr.ModuleInfo.InterfaceDef.Keys.Contains(i));
            var @interface = notImplementedInterface as Interface[] ?? notImplementedInterface.ToArray();
            if (@interface.Any())
            {
                throw handler.NotClosed(test.SourceLocation,
                    $"test module is not closed with respect to created interfaces; interface {@interface.First().Name} is created but not implemented inside the module");
            }

            //check that the test module main machine exists
            var hasMainMachine = test.ModExpr.ModuleInfo.InterfaceDef.Values.Any(m => m.Name == test.Main && !m.IsSpec);
            if (!hasMainMachine)
            {
                throw handler.NoMain(test.SourceLocation,
                    $"machine {test.Main} does not exist in the test module");
            }
        }

        internal void CheckImplementationDecl(Implementation impl)
        {
            //check that the implementation module is closed with respect to creates
            var notImplementedInterface =
                impl.ModExpr.ModuleInfo.Creates.Interfaces.Where(i =>
                    !impl.ModExpr.ModuleInfo.InterfaceDef.Keys.Contains(i)).ToList();
            if (notImplementedInterface.Any())
            {
                throw handler.NotClosed(notImplementedInterface.First().SourceLocation,
                    $"implementation module is not closed with respect to created interfaces; interface {notImplementedInterface.First().Name} is created but not implemented inside the module");
            }
        }

        private void CheckWellFormedness(BindModuleExpr bindExpr)
        {
            if (bindExpr.ModuleInfo != null)
            {
                return;
            }

            // checked already that the bindings is a function

            // check that receive set of interface is a subset of the receive set of machine
            foreach (var binding in bindExpr.Bindings)
            {
                if (!binding.Item1.ReceivableEvents.IsSubsetEqOf(binding.Item2.Receives))
                {
                    throw handler.InvalidBindExpr(bindExpr.SourceLocation,
                        $"receive set of {binding.Item1.Name} is not a subset of receive set of {binding.Item2.Name}");
                }

                if (!binding.Item2.PayloadType.IsAssignableFrom(binding.Item1.PayloadType))
                {
                    throw handler.InvalidBindExpr(bindExpr.SourceLocation,
                        $"payload type of {binding.Item1.Name} is not a subtype of payload type of {binding.Item2.Name}");
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

            var boundMachines = bindExpr.Bindings.Select(b => b.Item2).ToList();
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

        private void CheckWellFormedness(RenameModuleExpr renameExpr)
        {
            if (renameExpr.ModuleInfo != null)
            {
                return;
            }

            //check that component module is wellformed
            CheckWellFormedness(renameExpr.ComponentModule);

            //check that the module is wellformed
            var componentModuleInfo = renameExpr.ComponentModule.ModuleInfo;

            // 1) receives set of both new and old interface must be same
            if (!renameExpr.NewInterface.ReceivableEvents.IsSame(renameExpr.OldInterface.ReceivableEvents))
            {
                throw handler.InvalidRenameExpr(renameExpr.SourceLocation,
                    $"{renameExpr.NewInterface.Name} and {renameExpr.OldInterface.Name} must have the same receive set");
            }

            // 2) oldInterface must belong to implemented or created interface
            if (!componentModuleInfo.Creates.Interfaces.Union(componentModuleInfo.InterfaceDef.Keys)
                    .Contains(renameExpr.OldInterface))
            {
                throw handler.InvalidRenameExpr(renameExpr.SourceLocation,
                    $"{renameExpr.OldInterface.Name} must belong to either created interfaces or bounded interfaces of the module");
            }

            // 3) newInterface must not belong to created and implemented interfaces.
            if (componentModuleInfo.Creates.Interfaces.Union(componentModuleInfo.InterfaceDef.Keys)
                .Contains(renameExpr.NewInterface))
            {
                throw handler.InvalidRenameExpr(renameExpr.SourceLocation,
                    $"{renameExpr.NewInterface.Name} must not belong to created interfaces or bounded interfaces of the module");
            }

            //populate the attributes of the module
            renameExpr.ModuleInfo = new ModuleInfo();
            var currentModuleInfo = renameExpr.ModuleInfo;

            // compute the new monitor map
            foreach (var monMap in componentModuleInfo.MonitorMap)
            {
                var interfaceList = monMap.Value.Select(@interface => @interface.Equals(renameExpr.OldInterface)
                    ? renameExpr.NewInterface
                    : @interface).ToList();
                currentModuleInfo.MonitorMap[monMap.Key] = interfaceList;
            }

            // compute the new private interfaces
            foreach (var @interface in componentModuleInfo.PrivateInterfaces.Interfaces)
            {
                currentModuleInfo.PrivateInterfaces.AddInterface(
                    @interface.Equals(renameExpr.OldInterface)
                        ? renameExpr.NewInterface
                        : @interface);
            }

            // compute the new interface definition map
            foreach (var interfaceDefItem in componentModuleInfo.InterfaceDef)
            {
                currentModuleInfo.InterfaceDef.Add(
                    interfaceDefItem.Key.Equals(renameExpr.OldInterface)
                        ? renameExpr.NewInterface
                        : interfaceDefItem.Key, interfaceDefItem.Value);
            }

            // compute the new link map
            foreach (var linkMapItem in componentModuleInfo
                         .LinkMap)
            {
                var keyInterface = linkMapItem.Key.Equals(renameExpr.OldInterface)
                    ? renameExpr.NewInterface
                    : linkMapItem.Key;

                currentModuleInfo.LinkMap[keyInterface] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    currentModuleInfo.LinkMap[keyInterface].Add(localLinkMap.Key,
                        localLinkMap.Value.Equals(renameExpr.OldInterface)
                            ? renameExpr.NewInterface
                            : localLinkMap.Value);
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

        private void CheckWellFormedness(UnionOrComposeModuleExpr composeExpr)
        {
            if (composeExpr.ModuleInfo != null)
            {
                return;
            }

            //check that all component modules are wellformed
            foreach (var module in composeExpr.ComponentModules)
            {
                CheckWellFormedness(module);
            }

            //check if the current module is wellformed

            // TODO: Woah, this is O(n^2). Can we get this down to O(n log n) at most?
            foreach (var module1 in composeExpr.ComponentModules)
            {
                foreach (var module2 in composeExpr.ComponentModules)
                {
                    if (module1 == module2)
                    {
                        continue;
                    }

                    var module1Info = module1.ModuleInfo;
                    var module2Info = module2.ModuleInfo;
                    var allPrivateEvents = module1Info
                        .PrivateEvents.Events
                        .Union(module2Info.PrivateEvents.Events).ToList();
                    var allSendAndReceiveEvents =
                        module1Info.Sends.Events.Union(
                            module1Info.Receives.Events.Union(
                                module2Info.Receives.Events.Union(
                                    module2Info.Sends.Events))).ToList();

                    // 1) domain of interface def map is disjoint
                    foreach (var @interface in module1Info.InterfaceDef.Keys.Intersect(
                                 module2Info.InterfaceDef.Keys))
                    {
                        throw handler.InvalidCompositionExpr(module1.SourceLocation,
                            "bound interfaces after composition are not disjoint, e.g., " +
                            $"interface {@interface.Name} is bound in both the modules being composed");
                    }

                    // 2) no private events in the sends or receives events
                    foreach (var @event in allSendAndReceiveEvents.Intersect(allPrivateEvents))
                    {
                        throw handler.InvalidCompositionExpr(module1.SourceLocation,
                            "private events after composition are not disjoint from send and receives set, e.g., " +
                            $"after composition private event {@event.Name} belongs to both private and public (sends or receives) events");
                    }

                    // 3) no private events in the sends or receives permissions
                    foreach (var @event in allSendAndReceiveEvents)
                    {
                        var permissionsEmbedded = GetPermissions(@event.PayloadType.AllowedPermissions?.Value);
                        foreach (var privatePermission in allPrivateEvents.Where(
                                     ev => permissionsEmbedded.Contains(ev)))
                        {
                            throw handler.InvalidCompositionExpr(module1.SourceLocation,
                                "private events after composition are not disjoint from permissions in events sent or received, e.g., " +
                                $"after composition private event {privatePermission.Name} is in the permissions set of {@event.Name}");
                        }
                    }

                    var interfaceImplAndNotCreated1 =
                        module1Info.Creates.Interfaces.Except(module1Info.InterfaceDef.Keys);
                    var interfaceCreatedAndNotImpl1 =
                        module1Info.InterfaceDef.Keys.Except(module1Info.Creates.Interfaces);
                    var interfaceImplAndNotCreated2 =
                        module2Info.Creates.Interfaces.Except(module2Info.InterfaceDef.Keys);
                    var interfaceCreatedAndNotImpl2 =
                        module2Info.InterfaceDef.Keys.Except(module2Info.Creates.Interfaces);

                    foreach (var @interface in interfaceImplAndNotCreated1.Union(
                                 interfaceCreatedAndNotImpl1.Union(
                                     interfaceImplAndNotCreated2.Union(interfaceCreatedAndNotImpl2))))
                    {
                        foreach (var @event in allPrivateEvents.Where(
                                     ev => @interface.ReceivableEvents.Contains(ev)))
                        {
                            throw handler.InvalidCompositionExpr(module1.SourceLocation,
                                $"After composition, private event {@event.Name} is in the received events of interface {@interface.Name} which is created or bound in the module");
                        }
                    }

                    // ensure also that the monitor maps are disjoint
                    foreach (var monitor in module1Info.MonitorMap.Keys.Intersect(module2Info.MonitorMap.Keys))
                    {
                        throw handler.InvalidCompositionExpr(module1.SourceLocation,
                            $"monitor {monitor.Name} is attached in more than one modules being composed");
                    }

                    // if composition then output actions must be disjoint
                    if (composeExpr.IsComposition)
                    {
                        foreach (var @event in module1Info.Sends.Events.Intersect(module2Info.Sends.Events))
                        {
                            throw handler.InvalidCompositionExpr(module1.SourceLocation,
                                $"output sends are not disjoint, {@event.Name} belongs to the sends of multiple composed module");
                        }

                        foreach (var @interface in module1Info.Creates.Interfaces.Intersect(
                                     module2Info.Creates.Interfaces))
                        {
                            throw handler.InvalidCompositionExpr(module1.SourceLocation,
                                $"output creates are not disjoint, {@interface.Name} belongs to the creates of multiple composed module");
                        }
                    }

                    foreach (var exportedOrCreatedInterface in module1.ModuleInfo.InterfaceDef.Keys.Union(module1.ModuleInfo
                                 .Creates.Interfaces))
                    {
                        foreach (var priEvent in module2.ModuleInfo.PrivateEvents.Events.Where(ev =>
                                     GetPermissions(exportedOrCreatedInterface.PayloadType.AllowedPermissions?.Value).Contains(ev)))
                        {
                            throw handler.InvalidHideEventExpr(module2.SourceLocation,
                                $"private event {priEvent.Name} belongs to the permissions of the constructor type of public interface {exportedOrCreatedInterface.Name}");
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

                foreach (var ipItem in module.InterfaceDef)
                {
                    currentModuleInfo.InterfaceDef.Add(ipItem.Key, ipItem.Value);
                }
            }

            // compute all the derived attributes
            currentModuleInfo.Sends.AddEvents(composeExpr.ComponentModules.SelectMany(m => m.ModuleInfo.Sends.Events));
            currentModuleInfo.Receives.AddEvents(
                composeExpr.ComponentModules.SelectMany(m => m.ModuleInfo.Receives.Events));
            currentModuleInfo.Creates.AddInterfaces(
                composeExpr.ComponentModules.SelectMany(m => m.ModuleInfo.Creates.Interfaces));
        }

        private void CheckWellFormedness(HideEventModuleExpr hideEExpr)
        {
            if (hideEExpr.ModuleInfo != null)
            {
                return;
            }

            //check that component module is wellformed
            CheckWellFormedness(hideEExpr.ComponentModule);

            //check if the current module is wellformed
            var componentModuleInfo = hideEExpr.ComponentModule.ModuleInfo;

            // 1) e \subseteq ER \intersect ES
            var receiveAndsends = componentModuleInfo
                .Sends.Events
                .Where(ev => componentModuleInfo.Receives.Contains(ev))
                .ToList();
            if (!hideEExpr.HideEvents.IsSubsetEqOf(receiveAndsends))
            {
                var @event = hideEExpr.HideEvents.Events.First(h => !receiveAndsends.Contains(h));
                throw handler.InvalidHideEventExpr(hideEExpr.SourceLocation,
                    $"event {@event.Name} cannot be made private, it must belong to both receive and send set of the module");
            }

            // 2) only events in interfaces that are both created and implemented by the module can be hidden
            var interfaceImplAndNotCreated =
                componentModuleInfo.Creates.Interfaces.Except(componentModuleInfo.InterfaceDef.Keys);
            var interfaceCreatedAndNotImpl =
                componentModuleInfo.InterfaceDef.Keys.Except(componentModuleInfo.Creates.Interfaces);

            foreach (var @interface in interfaceCreatedAndNotImpl
                         .Union(interfaceImplAndNotCreated)
                         .Where(i => hideEExpr.HideEvents.Intersects(i.ReceivableEvents.Events)))
            {
                var @event = hideEExpr.HideEvents.Events.First(ev => @interface.ReceivableEvents.Contains(ev));
                throw handler.InvalidHideEventExpr(hideEExpr.SourceLocation,
                    $"event {@event.Name} cannot be made private as interface {@interface.Name} contains this event. " +
                    "Only events in interfaces that are both created and bound in the module can be hidden");
            }

            // 3) events received and sent by the module must not include private permissions
            var eventsReceivedAndSent =
                componentModuleInfo.Sends.Events.Union(componentModuleInfo.Receives.Events);
            foreach (var @event in eventsReceivedAndSent.Except(hideEExpr.HideEvents.Events))
            {
                var permissionsEmbedded = GetPermissions(@event.PayloadType.AllowedPermissions.Value);
                foreach (var privatePermission in hideEExpr.HideEvents.Events.Where(
                             ev => permissionsEmbedded.Contains(ev)))
                {
                    throw handler.InvalidHideEventExpr(hideEExpr.SourceLocation,
                        $"event {privatePermission} cannot be made private as it belongs to allowed permission of {@event.Name} which is received or sent by the module");
                }
            }

            foreach (var exportedOrCreatedInterface in hideEExpr.ModuleInfo.InterfaceDef.Keys.Union(hideEExpr.ModuleInfo
                         .Creates.Interfaces))
            {
                foreach (var priEvent in hideEExpr.HideEvents.Events.Where(ev =>
                             GetPermissions(exportedOrCreatedInterface.PayloadType.AllowedPermissions?.Value).Contains(ev)))
                {
                    throw handler.InvalidHideEventExpr(hideEExpr.SourceLocation,
                        $"event {priEvent.Name} cannot be made private as it belongs to the permissions of the contructor type of interface {exportedOrCreatedInterface.Name}");
                }
            }

            hideEExpr.ModuleInfo = new ModuleInfo();
            var currentModuleInfo = hideEExpr.ModuleInfo;

            //populate the attributes of the module
            currentModuleInfo.PrivateEvents.AddEvents(
                componentModuleInfo.PrivateEvents.Events.Union(hideEExpr.HideEvents.Events));
            currentModuleInfo.PrivateInterfaces.AddInterfaces(componentModuleInfo.PrivateInterfaces.Interfaces);
            currentModuleInfo.Sends.AddEvents(componentModuleInfo.Sends.Events.Except(hideEExpr.HideEvents.Events));
            currentModuleInfo.Receives.AddEvents(
                componentModuleInfo.Receives.Events.Except(hideEExpr.HideEvents.Events));
            currentModuleInfo.Creates.AddInterfaces(componentModuleInfo.Creates.Interfaces);

            foreach (var monMap in componentModuleInfo.MonitorMap)
            {
                currentModuleInfo.MonitorMap[monMap.Key] = monMap.Value.ToList();
            }

            foreach (var linkMapItem in componentModuleInfo
                         .LinkMap)
            {
                currentModuleInfo.LinkMap[linkMapItem.Key] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    currentModuleInfo.LinkMap[linkMapItem.Key].Add(localLinkMap.Key, localLinkMap.Value);
                }
            }

            foreach (var ipItem in componentModuleInfo.InterfaceDef)
            {
                currentModuleInfo.InterfaceDef.Add(ipItem.Key, ipItem.Value);
            }
        }

        private void CheckWellFormedness(HideInterfaceModuleExpr hideIExpr)
        {
            if (hideIExpr.ModuleInfo != null)
            {
                return;
            }

            //check that component module is wellformed
            CheckWellFormedness(hideIExpr.ComponentModule);

            //check if the current module is wellformed
            var componentModuleInfo = hideIExpr.ComponentModule.ModuleInfo;

            // 1) interfaces to be hidden must be both implemented and created by the module
            var interfacesImplementedAndCreated =
                componentModuleInfo.Creates.Interfaces.Intersect(componentModuleInfo.InterfaceDef.Keys);
            foreach (var @interface in hideIExpr.HideInterfaces.Where(
                         it => !interfacesImplementedAndCreated.Contains(it)))
            {
                throw handler.InvalidHideInterfaceExpr(hideIExpr.SourceLocation,
                    $"interface {@interface.Name} cannot be made private. Interface {@interface.Name} must be both created and bounded in the module");
            }

            hideIExpr.ModuleInfo = new ModuleInfo();
            var currentModuleInfo = hideIExpr.ModuleInfo;

            //populate the attributes of the module
            currentModuleInfo.PrivateEvents.AddEvents(componentModuleInfo.PrivateEvents.Events);
            currentModuleInfo.PrivateInterfaces.AddInterfaces(
                componentModuleInfo.PrivateInterfaces.Interfaces.Union(hideIExpr.HideInterfaces));
            currentModuleInfo.Sends.AddEvents(componentModuleInfo.Sends.Events);
            currentModuleInfo.Receives.AddEvents(componentModuleInfo.Receives.Events);
            currentModuleInfo.Creates.AddInterfaces(componentModuleInfo.Creates.Interfaces);

            foreach (var monMap in componentModuleInfo.MonitorMap)
            {
                currentModuleInfo.MonitorMap[monMap.Key] = monMap.Value.ToList();
            }

            foreach (var linkMapItem in componentModuleInfo
                         .LinkMap)
            {
                currentModuleInfo.LinkMap[linkMapItem.Key] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    currentModuleInfo.LinkMap[linkMapItem.Key].Add(localLinkMap.Key, localLinkMap.Value);
                }
            }

            foreach (var ipItem in componentModuleInfo.InterfaceDef)
            {
                currentModuleInfo.InterfaceDef.Add(ipItem.Key, ipItem.Value);
            }
        }
    }
}