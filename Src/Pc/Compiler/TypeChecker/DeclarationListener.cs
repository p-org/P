using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public class DeclarationListener : PParserBaseListener
    {
        private readonly ParseTreeProperty<IPDecl> nodesToDeclarations;
        private readonly ParseTreeProperty<DeclarationTable> programDeclarations;
        private DeclarationTable table;

        public DeclarationListener(ParseTreeProperty<DeclarationTable> programDeclarations, ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            this.programDeclarations = programDeclarations;
            this.nodesToDeclarations = nodesToDeclarations;
        }

        #region Typedefs
        public override void EnterPTypeDef(PParser.PTypeDefContext context)
        {
            var typedef = (TypeDef)nodesToDeclarations.Get(context);
            typedef.Type = TypeResolver.ResolveType(context.type(), table);
        }

        public override void EnterForeignTypeDef(PParser.ForeignTypeDefContext context)
        {
            throw new NotImplementedException("TODO: foreign types");
        }
        #endregion

        #region Enums
        /// <summary>
        /// Enum declarations can't be nested, so we simply store the most recently encountered
        /// one in a variable for the listener actions for the elements to access.
        /// </summary>
        private PEnum currentEnum;

        public override void EnterEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            currentEnum = (PEnum)nodesToDeclarations.Get(context);
        }

        public override void ExitEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            // Check that there is a default element in the enum.
            if (currentEnum.Values.All(elem => elem.Value != 0))
            {
                throw new EnumMissingDefaultException(currentEnum);
            }
        }

        public override void EnterEnumElem(PParser.EnumElemContext context)
        {
            var elem = (EnumElem)nodesToDeclarations.Get(context);
            elem.Value = currentEnum.Count; // listener visits from left-to-right, so this will count upwards correctly.
            bool success = currentEnum.AddElement(elem);
            Debug.Assert(success);
        }

        public override void EnterNumberedEnumElem(PParser.NumberedEnumElemContext context)
        {
            var elem = (EnumElem)nodesToDeclarations.Get(context);
            elem.Value = int.Parse(context.value.Text);
            bool success = currentEnum.AddElement(elem);
            Debug.Assert(success);
        }
        #endregion

        #region Events
        public override void EnterEventDecl(PParser.EventDeclContext context)
        {
            var pEvent = (PEvent) nodesToDeclarations.Get(context);

            bool hasAssume = context.cardinality()?.ASSUME() != null;
            bool hasAssert = context.cardinality()?.ASSERT() != null;
            int cardinality = int.Parse(context.cardinality()?.IntLiteral().GetText() ?? "-1");
            pEvent.Assume = hasAssume ? cardinality : -1;
            pEvent.Assert = hasAssert ? cardinality : -1;

            pEvent.PayloadType = TypeResolver.ResolveType(context.type(), table);

            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("Have not implemented event annotations");
            }
        }
        #endregion

        /// <summary>
        /// Event sets cannot be nested, so we keep track only of the most recent one.
        /// </summary>
        private EventSet currentEventSet;

        public override void EnterNonDefaultEventList(PParser.NonDefaultEventListContext context)
        {
            // TODO: implement handlers for other parents of these event lists.
            Debug.Assert(currentEventSet != null, $"Event set not prepared for {nameof(EnterNonDefaultEventList)}");
            foreach (IToken contextEvent in context._events)
            {
                string eventName = contextEvent.Text;
                if (!table.Lookup(eventName, out PEvent evt))
                {
                    throw new MissingEventException(currentEventSet, eventName);
                }

                currentEventSet?.Events.Add(evt);
            }
        }

        #region Event sets
        public override void EnterEventSetDecl(PParser.EventSetDeclContext context)
        {
            currentEventSet = (EventSet)nodesToDeclarations.Get(context);
        }
        #endregion

        #region Interfaces
        public override void EnterInterfaceDecl(PParser.InterfaceDeclContext context)
        {
            var mInterface = (Interface)nodesToDeclarations.Get(context);
            mInterface.PayloadType = TypeResolver.ResolveType(context.type(), table);
            if (context.eventSet != null)
            {
                // Either look up the event set and establish the link by name...
                if (!table.Lookup(context.eventSet.Text, out EventSet eventSet))
                {
                    throw new MissingDeclarationException(eventSet);
                }
                mInterface.ReceivableEvents = eventSet;
            }
            else
            {
                // ... or let the nonDefaultEventList handler fill in a newly created event set
                Debug.Assert(context.nonDefaultEventList() != null);
                currentEventSet = new EventSet($"{mInterface.Name}$eventset", null);
                mInterface.ReceivableEvents = currentEventSet;
            }
        }
        #endregion

        /// <summary>
        /// Machines cannot be nested, so we keep track of only the most recent one.
        /// </summary>
        private Machine currentMachine;

        #region Machines
        public override void EnterImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            // eventDecl : MACHINE name=Iden
            currentMachine = (Machine) nodesToDeclarations.Get(context);

            // cardinality?
            bool hasAssume = context.cardinality()?.ASSUME() != null;
            bool hasAssert = context.cardinality()?.ASSERT() != null;
            int cardinality = int.Parse(context.cardinality()?.IntLiteral().GetText() ?? "-1");
            currentMachine.Assume = hasAssume ? cardinality : -1;
            currentMachine.Assert = hasAssert ? cardinality : -1;

            // annotationSet?
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("Machine annotations not yet implemented");
            }

            // (COLON idenList)?
            if (context.idenList() != null)
            {
                IEnumerable<string> interfaces = context.idenList()._names.Select(name => name.Text);
                foreach (string pInterfaceName in interfaces)
                {
                    if (!table.Lookup(pInterfaceName, out Interface pInterface))
                    {
                        throw new MissingDeclarationException(pInterface);
                    }

                    currentMachine.Interfaces.Add(pInterface);
                }
            }

            // receivesSends*
            // handled by EnterReceivesSends

            // machineBody
            // handled by EnterVarDecl / EnterFunDecl / EnterGroup / EnterStateDecl
        }

        public override void ExitImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            currentMachine = null;
        }

        public override void EnterReceivesSends(PParser.ReceivesSendsContext context)
        {
            if (context.RECEIVES() != null)
            {
                if (currentMachine.Receives == null)
                {
                    currentMachine.Receives = new EventSet($"{currentMachine.Name}$receives", null);
                }
                currentEventSet = currentMachine.Receives;
            }
            else if (context.SENDS() != null)
            {
                if (currentMachine.Sends == null)
                {
                    currentMachine.Sends = new EventSet($"{currentMachine.Name}$sends", null);
                }
                currentEventSet = currentMachine.Sends;
            }
            else
            {
                Debug.Fail("A receives / sends spec had neither a receives nor sends.");
            }
        }
        #endregion

        public override void EnterFunDecl(PParser.FunDeclContext context)
        {
            var fun = (Function) nodesToDeclarations.Get(context);
            fun.Signature.ReturnType = TypeResolver.ResolveType(context.type(), table);

            PParser.FunParamContext[] paramList = context.funParamList()?.funParam() ?? new PParser.FunParamContext[0];
            foreach (PParser.FunParamContext paramContext in paramList)
            {
                string name = paramContext.name.Text;
                bool success = table.Get(name, out Variable variable);
                Debug.Assert(success);
                fun.Signature.Parameters.Add(variable);
            }

            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("Function annotations not implemented");
            }

            if (context.statementBlock() == null)
            {
                throw new NotImplementedException("Foreign functions not implemented");
            }
        }

        public override void EnterVarDecl(PParser.VarDeclContext context)
        {
            foreach (ITerminalNode varName in context.idenList().Iden())
            {
                var variable = (Variable)nodesToDeclarations.Get(varName);
                variable.Type = TypeResolver.ResolveType(context.type(), table);
            }
        }

        public override void EnterPayloadVarDecl(PParser.PayloadVarDeclContext context)
        {
            var variable = (Variable) nodesToDeclarations.Get(context.funParam());
            variable.Type = TypeResolver.ResolveType(context.funParam().type(), table);
        }

        public override void EnterImplMachineProtoDecl(PParser.ImplMachineProtoDeclContext context)
        {
            var proto = (MachineProto) nodesToDeclarations.Get(context);
            proto.PayloadType = TypeResolver.ResolveType(context.type(), table);
        }

        public override void EnterSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            // TODO: implement
        }

        public override void EnterFunProtoDecl(PParser.FunProtoDeclContext context)
        {
            var proto = (FunctionProto) nodesToDeclarations.Get(context);
            proto.Signature.ReturnType = TypeResolver.ResolveType(context.type(), table);
            PParser.FunParamContext[] paramList = context.funParamList()?.funParam() ?? new PParser.FunParamContext[0];
            foreach (PParser.FunParamContext paramContext in paramList)
            {
                proto.Signature.Parameters.Add(
                    new FormalParameter {Name = paramContext.name.Text, Type = TypeResolver.ResolveType(paramContext.type(), table)});
            }
        }

        public override void EnterEveryRule(ParserRuleContext ctx)
        {
            DeclarationTable thisTable = programDeclarations.Get(ctx);
            if (thisTable != null)
            {
                table = thisTable;
            }
        }

        public override void ExitEveryRule(ParserRuleContext context)
        {
            if (programDeclarations.Get(context) != null)
            {
                Debug.Assert(table != null);
                table = table.Parent;
            }
        }
    }
}