using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public class DeclarationListener : PParserBaseListener
    {
        private readonly ParseTreeProperty<DeclarationTable> programDeclarations;
        private readonly ParseTreeProperty<IPDecl> nodesToDeclarations;
        private DeclarationTable table;

        public DeclarationListener(ParseTreeProperty<DeclarationTable> programDeclarations, ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            this.programDeclarations = programDeclarations;
            this.nodesToDeclarations = nodesToDeclarations;
        }

        public override void EnterPTypeDef(PParser.PTypeDefContext context)
        {
            var typedef = (TypeDef) nodesToDeclarations.Get(context);
            typedef.Type = TypeResolver.ResolveType(context.type(), table);
        }

        public override void EnterEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            var pEnum = (PEnum) nodesToDeclarations.Get(context);
            if (pEnum.Values.All(elem => elem.Value != 0))
            {
                throw new EnumMissingDefaultException(pEnum);
            }
        }

        public override void EnterEventDecl(PParser.EventDeclContext context)
        {
            var pEvent = (PEvent)nodesToDeclarations.Get(context);

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

        public override void EnterEventSetDecl(PParser.EventSetDeclContext context)
        {
            var eventSet = (EventSet) nodesToDeclarations.Get(context);
            IEnumerable<string> eventNames = context.nonDefaultEventList()._events.Select(t => t.Text);
            AddEventsToEventSet(eventNames, eventSet);
        }

        public override void EnterInterfaceDecl(PParser.InterfaceDeclContext context)
        {
            var mInterface = (Interface) nodesToDeclarations.Get(context);
            mInterface.PayloadType = TypeResolver.ResolveType(context.type(), table);
            EventSet eventSet;
            if (context.eventSet != null)
            {
                if (!table.Lookup(context.eventSet.Text, out eventSet))
                {
                    throw new MissingDeclarationException(eventSet);
                }
            }
            else
            {
                eventSet = new EventSet($"{mInterface.Name}$eventset", null);
                IEnumerable<string> eventNames = context.nonDefaultEventList()._events.Select(t => t.Text);
                AddEventsToEventSet(eventNames, eventSet);
            }
            mInterface.ReceivableEvents = eventSet;
        }

        public override void EnterImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            throw new NotImplementedException("Still have to implement concrete machines");
        }

        public override void EnterSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            throw new NotImplementedException("Still have to implement spec machines");
        }

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
                throw new NotImplementedException("Body-less functions not implemented");
            }
        }

        public override void EnterVarDecl(PParser.VarDeclContext context)
        {
            var variable = (Variable) nodesToDeclarations.Get(context);
            variable.Type = TypeResolver.ResolveType(context.type(), table);
        }

        public override void EnterPayloadVarDecl(PParser.PayloadVarDeclContext context)
        {
            var variable = (Variable)nodesToDeclarations.Get(context);
            variable.Type = TypeResolver.ResolveType(context.funParam().type(), table);
        }

        private void AddEventsToEventSet(IEnumerable<string> eventNames, EventSet eventSet)
        {
            foreach (string eventName in eventNames)
            {
                if (!table.Lookup(eventName, out PEvent evt))
                {
                    throw new MissingEventException(eventSet, eventName);
                }

                eventSet.Events.Add(evt);
            }
        }

        public override void EnterImplMachineProtoDecl(PParser.ImplMachineProtoDeclContext context)
        {
            var proto = (MachineProto) nodesToDeclarations.Get(context);
            proto.PayloadType = TypeResolver.ResolveType(context.type(), table);
        }

        public override void EnterFunProtoDecl(PParser.FunProtoDeclContext context)
        {
            var proto = (FunctionProto) nodesToDeclarations.Get(context);
            proto.Signature.ReturnType = TypeResolver.ResolveType(context.type(), table);
            PParser.FunParamContext[] paramList = context.funParamList()?.funParam() ?? new PParser.FunParamContext[0];
            foreach (PParser.FunParamContext paramContext in paramList)
            {
                proto.Signature.Parameters.Add(
                    new FormalParameter
                    {
                        Name = paramContext.name.Text,
                        Type = TypeResolver.ResolveType(paramContext.type(), table)
                    });
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

    public class MissingDeclarationException : Exception
    {
        public IPDecl Declaration { get; }

        public MissingDeclarationException(IPDecl declaration)
        {
            Declaration = declaration;
        }
    }
}