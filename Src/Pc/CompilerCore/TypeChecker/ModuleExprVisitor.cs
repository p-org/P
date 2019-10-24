using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.ModuleExprs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plang.Compiler.TypeChecker
{
    internal static class ModuleSystemDeclarations
    {
        public static void PopulateAllModuleExprs(
            ITranslationErrorHandler handler,
            Scope globalScope)
        {
            ModuleExprVisitor modExprVisitor = new ModuleExprVisitor(handler, globalScope);

            // first do all the named modules
            foreach (NamedModule mod in globalScope.NamedModules)
            {
                PParser.NamedModuleDeclContext context = (PParser.NamedModuleDeclContext)mod.SourceLocation;
                mod.ModExpr = modExprVisitor.Visit(context.modExpr());
            }

            // all the test declarations
            foreach (SafetyTest test in globalScope.SafetyTests)
            {
                PParser.SafetyTestDeclContext context = (PParser.SafetyTestDeclContext)test.SourceLocation;
                test.ModExpr = modExprVisitor.Visit(context.modExpr());
            }

            foreach (RefinementTest test in globalScope.RefinementTests)
            {
                PParser.RefinementTestDeclContext context = (PParser.RefinementTestDeclContext)test.SourceLocation;
                test.LeftModExpr = modExprVisitor.Visit(context.modExpr()[0]);
                test.RightModExpr = modExprVisitor.Visit(context.modExpr()[1]);
            }

            if (globalScope.Implementations.Any())
            {
                // all user defind implementations
                foreach (Implementation impl in globalScope.Implementations)
                {
                    PParser.ImplementationDeclContext context = (PParser.ImplementationDeclContext)impl.SourceLocation;
                    impl.ModExpr = modExprVisitor.Visit(context.modExpr());
                }
            }
            else if (!globalScope.SafetyTests.Any())
            {
                Implementation defaultImplDecl = new Implementation(ParserRuleContext.EmptyContext, "DefaultImpl")
                {
                    Main = "Main"
                };
                // create bindings from each machine to itself
                List<Tuple<Interface, Machine>> defaultBindings = new List<Tuple<Interface, Machine>>();
                foreach (Machine machine in globalScope.Machines.Where(m => !m.IsSpec))
                {
                    globalScope.Get(machine.Name, out Interface @interface);
                    defaultBindings.Add(new Tuple<Interface, Machine>(@interface, machine));
                }

                defaultImplDecl.ModExpr = new BindModuleExpr(ParserRuleContext.EmptyContext, defaultBindings);

                globalScope.AddDefaultImpl(defaultImplDecl);
            }
        }
    }

    internal class ModuleExprVisitor : PParserBaseVisitor<IPModuleExpr>
    {
        private readonly Scope globalScope;
        private readonly ITranslationErrorHandler handler;

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
            if (!globalScope.Get(context.GetText(), out NamedModule mod))
            {
                throw handler.MissingDeclaration(context, "module", context.GetText());
            }

            PParser.NamedModuleDeclContext declContext = (PParser.NamedModuleDeclContext)mod.SourceLocation;
            return Visit(declContext.modExpr());
        }

        public override IPModuleExpr VisitPrimitiveModuleExpr([NotNull] PParser.PrimitiveModuleExprContext context)
        {
            List<Tuple<Interface, Machine>> bindings = context._bindslist.Select(VisitBindExpr).ToList();
            return new BindModuleExpr(context, bindings);
        }

        public override IPModuleExpr VisitComposeModuleExpr([NotNull] PParser.ComposeModuleExprContext context)
        {
            List<IPModuleExpr> moduleList = context._mexprs.Select(Visit).ToList();
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

                eventList.Add(@event);
            }

            return new HideEventModuleExpr(context, eventList, Visit(context.modExpr()));
        }

        public override IPModuleExpr VisitHideInterfacesModuleExpr(
            [NotNull] PParser.HideInterfacesModuleExprContext context)
        {
            List<Interface> interfaceList = new List<Interface>();
            foreach (PParser.IdenContext interfaceName in context.idenList()._names)
            {
                if (!globalScope.Get(interfaceName.GetText(), out Interface @interface))
                {
                    throw handler.MissingDeclaration(interfaceName, "interface", interfaceName.GetText());
                }

                interfaceList.Add(@interface);
            }

            return new HideInterfaceModuleExpr(context, interfaceList, Visit(context.modExpr()));
        }

        public override IPModuleExpr VisitRenameModuleExpr([NotNull] PParser.RenameModuleExprContext context)
        {
            if (!globalScope.Get(context.newName.GetText(), out Interface newInterface))
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
            List<IPModuleExpr> moduleList = context._mexprs.Select(Visit).ToList();
            return new UnionOrComposeModuleExpr(context, moduleList, false);
        }

        public override IPModuleExpr VisitAssertModuleExpr([NotNull] PParser.AssertModuleExprContext context)
        {
            List<Machine> monList = new List<Machine>();
            foreach (PParser.IdenContext monName in context.idenList()._names)
            {
                if (!globalScope.Get(monName.GetText(), out Machine monitor))
                {
                    throw handler.MissingDeclaration(monName, "spec machine", monName.GetText());
                }

                if (!monitor.IsSpec)
                {
                    throw handler.ExpectedMonitor(monName, monitor);
                }

                monList.Add(monitor);
            }

            return new AssertModuleExpr(context, monList, Visit(context.modExpr()));
        }

        public override IPModuleExpr VisitParenModuleExpr(PParser.ParenModuleExprContext context)
        {
            return Visit(context.modExpr());
        }

        private new Tuple<Interface, Machine> VisitBindExpr([NotNull] PParser.BindExprContext context)
        {
            string machine = context.mName.GetText();
            string @interface = context.iName?.GetText() ?? machine;

            if (!globalScope.Get(@interface, out Interface i))
            {
                throw handler.MissingDeclaration(context.iName ?? context.mName, "interface", @interface);
            }

            if (!globalScope.Get(machine, out Machine m))
            {
                throw handler.MissingDeclaration(context.mName, "machine", machine);
            }

            return Tuple.Create(i, m);
        }
    }
}