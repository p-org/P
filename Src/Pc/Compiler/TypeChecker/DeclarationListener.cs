using System;
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

        public override void EnterEventSetDecl(PParser.EventSetDeclContext context)
        {
            var eventSet = (EventSet) nodesToDeclarations.Get(context);
            foreach (IToken eventNameToken in context.nonDefaultEventList()._events)
            {
                string eventName = eventNameToken.Text;
                if (eventName.Equals("halt"))
                {
                    throw new NotImplementedException("Halt event not implemented");
                }

                if (!table.Lookup(eventName, out PEvent evt))
                {
                    throw new MissingEventException(eventSet, eventName);
                }

                eventSet.Events.Add(evt);
            }
        }

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
                throw new NotImplementedException();
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