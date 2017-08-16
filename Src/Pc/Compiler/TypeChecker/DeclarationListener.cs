using System;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker {
    public class TypeVisitor : PParserBaseVisitor<PLanguageType>
    {
        private DeclarationTable declarations;
        private readonly PTypeUniverse universe;

        public TypeVisitor(PTypeUniverse universe)
        {
            this.universe = universe;
        }

        public PLanguageType ResolveType(ParserRuleContext context, DeclarationTable table)
        {
            declarations = table;
            return context == null ? PrimitiveType.Null : Visit(context);
        }

        public override PLanguageType VisitBoundedType(PParser.BoundedTypeContext context)
        {
            return base.VisitBoundedType(context);
        }

        public override PLanguageType VisitSeqType(PParser.SeqTypeContext context)
        {
            PLanguageType elemType = Visit(context.type());
            return universe.GetOrCreateSeqType(elemType);
        }

        public override PLanguageType VisitNamedType(PParser.NamedTypeContext context)
        {
            return base.VisitNamedType(context);
        }

        public override PLanguageType VisitTupleType(PParser.TupleTypeContext context)
        {
            return base.VisitTupleType(context);
        }

        public override PLanguageType VisitNamedTupleType(PParser.NamedTupleTypeContext context)
        {
            return base.VisitNamedTupleType(context);
        }

        public override PLanguageType VisitPrimitiveType(PParser.PrimitiveTypeContext context)
        {
            string name = context.GetText();
            return universe.GetPrimitiveType(name);
        }

        public override PLanguageType VisitMapType(PParser.MapTypeContext context)
        {
            PLanguageType keyType = Visit(context.keyType);
            PLanguageType valueType = Visit(context.valueType);
            return universe.GetOrCreateMapType(keyType, valueType);
        }
    }

    public class DeclarationListener : PParserBaseListener
    {
        private readonly ParseTreeProperty<DeclarationTable> programDeclarations;
        private readonly TypeVisitor typeVisitor;
        private DeclarationTable table;

        public DeclarationListener(ParseTreeProperty<DeclarationTable> programDeclarations, TypeVisitor typeVisitor)
        {
            this.programDeclarations = programDeclarations;
            this.typeVisitor = typeVisitor;
        }

        public override void EnterEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            base.EnterEnumTypeDefDecl(context);
        }

        public override void EnterEventDecl(PParser.EventDeclContext context)
        {
            if (!table.Get(context.name.Text, out PEvent pEvent))
            {
                Debug.Assert(false, $"INTERNAL ERROR: somehow, the event {context.name.Text} was lost.");
            }

            bool hasAssume = context.cardinality()?.ASSUME() != null;
            bool hasAssert = context.cardinality()?.ASSERT() != null;
            int cardinality = int.Parse(context.cardinality()?.IntLiteral().GetText() ?? "-1");
            pEvent.Assume = hasAssume ? cardinality : -1;
            pEvent.Assert = hasAssert ? cardinality : -1;

            pEvent.PayloadType = typeVisitor.ResolveType(context.type(), table);

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