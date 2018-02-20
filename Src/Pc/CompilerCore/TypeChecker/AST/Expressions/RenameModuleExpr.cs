using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime;

namespace Microsoft.Pc.TypeChecker.AST.Declarations
{
    public class RenameModuleExpr : ModuleExpr
    {

        private IPModuleExpr module;
        private Interface newInterface;
        private Interface oldInterface;

        public RenameModuleExpr(ParserRuleContext sourceNode, Interface newName, Interface oldName, IPModuleExpr module)
        {
            SourceLocation = sourceNode;
            newInterface = newName;
            oldInterface = oldName;
            this.module = module;
        }

        public override bool CheckAndPopulateAttributes(ITranslationErrorHandler handler)
        {
            if (IsWellFormed)
                return true;

            //check that component module is wellformed
            module.CheckAndPopulateAttributes(handler);

            //check that the module is wellformed

            // 1) receives set of both new and old interface must be same
            if(!newInterface.ReceivableEvents.IsSame(oldInterface.ReceivableEvents))
            {
                throw handler.InvalidRenameExpr(SourceLocation, $"{newInterface.Name} and {oldInterface.Name} must have the same receive set");
            }

            // 2) oldInterface must belong to implemented or created interface
            if(!(module.Creates.Interfaces.Union(module.InterfaceDef.Keys)).Contains(oldInterface))
            {
                throw handler.InvalidRenameExpr(SourceLocation, $"{oldInterface.Name} must belong to either created interfaces or bounded interfaces of the module");
            }

            // 3) newInterface must not belong to created and implemented interfaces.
            if ((module.Creates.Interfaces.Union(module.InterfaceDef.Keys)).Contains(newInterface))
            {
                throw handler.InvalidRenameExpr(SourceLocation, $"{newInterface.Name} must not belong to created interfaces or bounded interfaces of the module");
            }

            //module is wellformed
            isWellFormed = true;

            //populate the attributes of the module
            // compute the new monitor map
            foreach (var monMap in module.MonitorMap)
            {
                var interfaceList = new List<Interface>();
                foreach(var @interface in monMap.Value)
                {
                    if(@interface.Equals(oldInterface))
                    {
                        interfaceList.Add(newInterface);
                    }
                    else
                    {
                        interfaceList.Add(@interface);
                    }
                }
                monitorMap[monMap.Key] = interfaceList;
            }

            // compute the new private interfaces
            foreach(var @interface in module.PrivateInterfaces.Interfaces)
            {
                if (@interface.Equals(oldInterface)) privateInterfaces.AddInterface(newInterface); else privateInterfaces.AddInterface(@interface);
            }

            // compute the new interface definition map
            foreach (var interfaceDefItem in module.InterfaceDef)
            {
                if (interfaceDefItem.Key.Equals(oldInterface)) interfaceDef.Add(newInterface, interfaceDefItem.Value); else interfaceDef.Add(interfaceDefItem.Key, interfaceDefItem.Value);
            }

            // compute the new link map
            foreach (var linkMapItem in module.LinkMap)
            {
                Interface keyInterface;
                if (linkMapItem.Key.Equals(oldInterface)) keyInterface = newInterface;
                else keyInterface = linkMapItem.Key;

                linkMap[keyInterface] = new Dictionary<Interface, Interface>();
                foreach (var localLinkMap in linkMapItem.Value)
                {
                    if(localLinkMap.Value.Equals(oldInterface))
                        linkMap[keyInterface].Add(localLinkMap.Key, newInterface);
                    else
                        linkMap[keyInterface].Add(localLinkMap.Key, localLinkMap.Value);
                }
            }

            // compute the sends
            sends.AddEvents(module.Sends.Events);

            // compute the receives
            receives.AddEvents(module.Receives.Events);

            // compute the creates
            foreach (var binding in interfaceDef)
            {
                foreach (var createdInterface in binding.Value.Creates.Interfaces)
                {
                    creates.AddInterface(LinkMap[binding.Key][createdInterface]);
                }
            }

            return IsWellFormed;
        }
    }
    
}