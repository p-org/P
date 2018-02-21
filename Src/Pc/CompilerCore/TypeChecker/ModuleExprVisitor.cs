using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Misc;

namespace Microsoft.Pc.TypeChecker.AST
{
    class ModuleSystemDeclarations
    {

        public static void PopulateAllModuleExprs(
            ITranslationErrorHandler handler,
            Scope globalScope)
        {
            var modExprVisitor = new ModuleExprVisitor(handler, globalScope);
            // first do all the named modules
            foreach (NamedModule mod in globalScope.NamedModules)
            {
                var context = (PParser.NamedModuleDeclContext) mod.SourceLocation;
                mod.ModExpr = modExprVisitor.Visit(context.modExpr());
            }

            // all the test declarations
            foreach (SafetyTest test in globalScope.SafetyTests)
            {
                var context = (PParser.SafetyTestDeclContext)test.SourceLocation;
                test.ModExpr = modExprVisitor.Visit(context.modExpr());
            }

            foreach (RefinementTest test in globalScope.RefinementTests)
            {
                var context = (PParser.RefinementTestDeclContext)test.SourceLocation;
                test.LeftModExpr = modExprVisitor.Visit(context.modExpr()[0]);
                test.RightModExpr = modExprVisitor.Visit(context.modExpr()[1]);
            }

            // all the implementations
            foreach (Implementation impl in globalScope.Implementations)
            {
                var context = (PParser.ImplementationDeclContext)impl.SourceLocation;
                impl.ModExpr = modExprVisitor.Visit(context.modExpr());
            }
            
        }
    }

    class ModuleExprVisitor : PParserBaseVisitor<IPModuleExpr>
    {
        private ITranslationErrorHandler handler;
        private Scope globalScope;

        public ModuleExprVisitor(
            ITranslationErrorHandler handler,
            Scope globalScope)
        {
            this.handler = handler;
            this.globalScope = globalScope;
        }

        public override IPModuleExpr VisitNamedModule([NotNull] PParser.NamedModuleContext context)
        {
            // check if the named module is declared
            if (globalScope.Get(context.GetText(), out NamedModule mod))
            {
                var declContext = (PParser.NamedModuleDeclContext)mod.SourceLocation;
                return Visit(declContext.modExpr());
            }
            else
            {
                throw handler.MissingDeclaration(context, "module", context.GetText());
            }

        }

        public override IPModuleExpr VisitPrimitiveModuleExpr([NotNull] PParser.PrimitiveModuleExprContext context)
        {
            var bindings = new List<Tuple<Interface, Machine>>();
            foreach(var bindExpr in context._bindslist)
            {
                bindings.Add(VisitBindExpr(bindExpr));
            }
            return new BindModuleExpr(context, bindings);
        }

        public override IPModuleExpr VisitComposeModuleExpr([NotNull] PParser.ComposeModuleExprContext context)
        {
            List<IPModuleExpr> moduleList = new List<IPModuleExpr>();
            foreach (var modExpr in context._mexprs)
            {
                moduleList.Add(Visit(modExpr));
            }
            return new UnionOrComposeModuleExpr(context, moduleList, true);
        }

        public override IPModuleExpr VisitHideEventsModuleExpr([NotNull] PParser.HideEventsModuleExprContext context)
        {
            List<PEvent> eventList = new List<PEvent>();
            foreach (PParser.NonDefaultEventContext eventName in context.nonDefaultEventList()._events)
            {
                if (!globalScope.Get(eventName.GetText(), out PEvent @event))
                {
                    throw handler.MissingDeclaration(eventName, "event", eventName.GetText());
                }
                else
                {
                    eventList.Add(@event);
                }
            }
            return new HideEventModuleExpr(context, eventList, Visit(context.modExpr()));
        }

        public override IPModuleExpr VisitHideInterfacesModuleExpr([NotNull] PParser.HideInterfacesModuleExprContext context)
        {
            List<Interface> interfaceList = new List<Interface>();
            foreach (PParser.IdenContext interfaceName in context.idenList()._names)
            {
                if (!globalScope.Get(interfaceName.GetText(), out Interface @interface))
                {
                    throw handler.MissingDeclaration(interfaceName, "interface", interfaceName.GetText());
                }
                else
                {
                    interfaceList.Add(@interface);
                }
            }
            return new HideInterfaceModuleExpr(context, interfaceList, Visit(context.modExpr()));
        }

        public override IPModuleExpr VisitRenameModuleExpr([NotNull] PParser.RenameModuleExprContext context)
        {
            if(!globalScope.Get(context.newName.GetText(), out Interface newInterface))
            {
                throw handler.MissingDeclaration(context.newName, "interface", context.newName.GetText());
            }
            if (!globalScope.Get(context.oldName.GetText(), out Interface oldInterface))
            {
                throw handler.MissingDeclaration(context.oldName, "interface", context.oldName.GetText());
            }

            return new RenameModuleExpr(context, newInterface, oldInterface, Visit(context.modExpr()));
        }

        public override IPModuleExpr VisitUnionModuleExpr([NotNull] PParser.UnionModuleExprContext context)
        {
            List<IPModuleExpr> moduleList = new List<IPModuleExpr>();
            foreach(var modExpr in context._mexprs)
            {
                moduleList.Add(Visit(modExpr));
            }
            return new UnionOrComposeModuleExpr(context, moduleList, false);
        }

        public override IPModuleExpr VisitAssertModuleExpr([NotNull] PParser.AssertModuleExprContext context)
        {
            List<Machine> monList = new List<Machine>();
            foreach(PParser.IdenContext monName in context.idenList()._names)
            {
                if(!globalScope.Get(monName.GetText(), out Machine mon))
                {
                    throw handler.MissingDeclaration(monName, "spec machine", monName.GetText());
                }
                else
                {
                    if(!mon.IsSpec)
                    {
                        handler.IssueError(monName, $"expected a specification machine instead of {mon.Name}");
                    }
                    else
                    {
                        monList.Add(mon);
                    }
                }
            }
            return new AssertModuleExpr(context, monList, Visit(context.modExpr()));
        }

        private new Tuple<Interface, Machine> VisitBindExpr([NotNull] PParser.BindExprContext context)
        {
            string machine = context.mName.GetText();
            string @interface = context.iName == null ? machine : context.iName.GetText();

            if(!globalScope.Get(@interface, out Interface i))
            {
                throw handler.MissingDeclaration(context.iName, "interface", @interface);
            }
            if (!globalScope.Get(machine, out Machine m))
            {
                throw handler.MissingDeclaration(context.mName, "machine", machine);
            }

            return new Tuple<Interface, Machine>(i, m);
        }
    }
}
