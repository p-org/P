using System;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker
{
    public class DeclarationVisitor : PParserBaseVisitor<object>
    {
        private readonly StackProperty<Scope> currentScope = new StackProperty<Scope>();
        private readonly ITranslationErrorHandler handler;
        private readonly ParseTreeProperty<IPDecl> nodesToDeclarations;
        private readonly ParseTreeProperty<Scope> nodesToScopes;

        private DeclarationVisitor(
            ITranslationErrorHandler handler,
            ParseTreeProperty<Scope> nodesToScopes,
            ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            this.handler = handler;
            this.nodesToScopes = nodesToScopes;
            this.nodesToDeclarations = nodesToDeclarations;
        }

        public static void PopulateDeclarations(
            ITranslationErrorHandler handler,
            ParseTreeProperty<Scope> nodesToScopes,
            ParseTreeProperty<IPDecl> nodesToDeclarations,
            PParser.ProgramContext context)
        {
            var visitor = new DeclarationVisitor(handler, nodesToScopes, nodesToDeclarations);
            visitor.Visit(context);
        }

        public override object VisitEventSetDecl(PParser.EventSetDeclContext context)
        {
            var eventSet = (EventSet) nodesToDeclarations.Get(context);
            eventSet.Events.UnionWith((PEvent[]) Visit(context.eventSetLiteral()));
            return eventSet;
        }

        public override object VisitEventSetLiteral(PParser.EventSetLiteralContext context)
        {
            var events = new PEvent[context._events.Count];
            for (var i = 0; i < context._events.Count; i++)
            {
                PParser.NonDefaultEventContext token = context._events[i];
                string eventName = token.GetText();
                if (!currentScope.Value.Lookup(eventName, out PEvent evt))
                {
                    throw handler.MissingDeclaration(token, "event", eventName);
                }
                events[i] = evt;
            }
            return events;
        }

        public override object VisitEventDecl(PParser.EventDeclContext context)
        {
            // EVENT name = Iden
            var pEvent = (PEvent) nodesToDeclarations.Get(context);

            // cardinality?
            bool hasAssume = context.cardinality()?.ASSUME() != null;
            bool hasAssert = context.cardinality()?.ASSERT() != null;
            int cardinality = int.Parse(context.cardinality()?.IntLiteral().GetText() ?? "-1");
            pEvent.Assume = hasAssume ? cardinality : -1;
            pEvent.Assert = hasAssert ? cardinality : -1;

            // (COLON type)?
            pEvent.PayloadType = TypeResolver.ResolveType(context.type(), currentScope.Value, handler);

            // annotationSet?
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("event annotations");
            }

            // SEMI ;
            return pEvent;
        }

        public override object VisitInterfaceDecl(PParser.InterfaceDeclContext context)
        {
            // TYPE name=Iden
            var mInterface = (Interface) nodesToDeclarations.Get(context);

            // LPAREN type? RPAREN
            mInterface.PayloadType = TypeResolver.ResolveType(context.type(), currentScope.Value, handler);

            if (context.eventSet == null)
            {
                // ASSIGN LBRACE eventSetLiteral RBRACE
                // Let the eventSetLiteral handler fill in a newly created event set...
                PParser.EventSetLiteralContext eventSetLiteral = context.eventSetLiteral();
                Debug.Assert(eventSetLiteral != null);
                var eventSet = new EventSet($"{mInterface.Name}$eventset", eventSetLiteral);
                eventSet.Events.UnionWith((PEvent[]) Visit(eventSetLiteral));
                mInterface.ReceivableEvents = eventSet;
            }
            else
            {
                // ASSIGN eventSet=Iden
                // ...or look up the event set and establish the link by name.
                string eventSetName = context.eventSet.GetText();
                if (!currentScope.Value.Lookup(eventSetName, out EventSet eventSet))
                {
                    throw handler.MissingDeclaration(context.eventSet, "event set", eventSetName);
                }

                mInterface.ReceivableEvents = eventSet;
            }

            return mInterface;
        }

        public override object VisitPFunDecl(PParser.PFunDeclContext context)
        {
            // FUN name=Iden
            var fun = (Function) nodesToDeclarations.Get(context);

            // LPAREN funParamList? RPAREN
            if (context.funParamList() is PParser.FunParamListContext paramList)
            {
                fun.Signature.Parameters.AddRange((Variable[]) Visit(paramList));
            }

            // (COLON type)?
            fun.Signature.ReturnType = TypeResolver.ResolveType(context.type(), currentScope.Value, handler);

            // annotationSet?
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("function annotations");
            }

            // functionBody (handled later)
            return fun;
        }

        public override object VisitForeignFunDecl(PParser.ForeignFunDeclContext context)
        {
            // FUN name=Iden
            var fun = (Function)nodesToDeclarations.Get(context);

            // LPAREN funParamList? RPAREN
            if (context.funParamList() is PParser.FunParamListContext paramList)
            {
                fun.Signature.Parameters.AddRange((Variable[])Visit(paramList));
            }

            // (COLON type)?
            fun.Signature.ReturnType = TypeResolver.ResolveType(context.type(), currentScope.Value, handler);

            // annotationSet?
            if (context.annotationSet() != null)
            {
                throw new NotImplementedException("function annotations");
            }

            // NULL
            return fun;
        }

        public override object VisitFunParamList(PParser.FunParamListContext context)
        {
            // funParam (COMMA funParam)*
            return context.funParam().Select(Visit).ToArray();
        }

        public override object VisitFunParam(PParser.FunParamContext context)
        {
            // name=Iden
            bool success = currentScope.Value.Get(context.name.GetText(), out Variable variable);
            Debug.Assert(success);

            // COLON type
            variable.Type = TypeResolver.ResolveType(context.type(), currentScope.Value, handler);
            return variable;
        }
    }
}
