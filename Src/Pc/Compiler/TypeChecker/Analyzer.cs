using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.Statements;
using Microsoft.Pc.TypeChecker.AST.States;

namespace Microsoft.Pc.TypeChecker
{
    public static class Analyzer
    {
        public static PProgramModel AnalyzeCompilationUnit(
            ITranslationErrorHandler handler,
            params PParser.ProgramContext[] programUnits)
        {
            // Step 1: Build the global scope of declarations
            Scope globalScope = BuildGlobalScope(handler, programUnits);

            // Step 2: Validate machine specifications
            foreach (Machine machine in globalScope.Machines)
            {
                Validator.ValidateMachine(handler, machine);
            }
            
            // Step 3: Fill function bodies
            foreach (var machineFunction in AllFunctions(globalScope))
            {
                FunctionBodyVisitor.PopulateMethod(handler, machineFunction.Item1, machineFunction.Item2);
            }

            // NOW: AST Complete, pass to StringTemplate
            return new PProgramModel
            {
                GlobalScope = globalScope
            };
        }

        private static Scope BuildGlobalScope(ITranslationErrorHandler handler, PParser.ProgramContext[] programUnits)
        {
            var globalScope = new Scope(handler);
            var nodesToDeclarations = new ParseTreeProperty<IPDecl>();

            // Add built-in events to the table.
            globalScope.Put("halt", (PParser.EventDeclContext) null);
            globalScope.Put("null", (PParser.EventDeclContext) null);

            // Step 1: Create mapping of names to declaration stubs
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                DeclarationStubVisitor.PopulateStubs(globalScope, programUnit, nodesToDeclarations);
            }

            // Step 2: Validate declarations and fill with types
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                DeclarationVisitor.PopulateDeclarations(handler, globalScope, programUnit, nodesToDeclarations);
            }
            return globalScope;
        }

        private static IEnumerable<Tuple<Machine, Function>> AllFunctions(Scope globalScope)
        {
            foreach (Function fun in globalScope.Functions)
            {
                yield return Tuple.Create((Machine) null, fun);
            }

            foreach (Machine machine in globalScope.Machines)
            {
                foreach (Function method in machine.Methods)
                {
                    yield return Tuple.Create(machine, method);
                }
                foreach (State state in machine.AllStates())
                {
                    foreach (IStateAction stateAction in state.AllEventHandlers.Select(kv => kv.Value))
                    {
                        switch (stateAction)
                        {
                            case EventDoAction action:
                                if (action.Target != null)
                                {
                                    yield return Tuple.Create(machine, action.Target);
                                }
                                break;
                            case EventGotoState action:
                                if (action.TransitionFunction != null)
                                {
                                    yield return Tuple.Create(machine, action.TransitionFunction);
                                }
                                break;
                        }
                    }
                }
            }
        }
    }

    public class FunctionBodyVisitor : PParserBaseVisitor<object>
    {
        private readonly ITranslationErrorHandler handler;
        private readonly Machine machine;
        private readonly Function method;

        private FunctionBodyVisitor(ITranslationErrorHandler handler, Machine machine, Function method)
        {
            this.handler = handler;
            this.machine = machine;
            this.method = method;
        }

        public static void PopulateMethod(ITranslationErrorHandler handler, Function fun)
        {
            PopulateMethod(handler, null, fun);
        }

        public static void PopulateMethod(ITranslationErrorHandler handler, Machine machine, Function method)
        {
            Debug.Assert(method.Owner == machine);
            if (method.Body != null)
            {
                return;
            }

            var visitor = new FunctionBodyVisitor(handler, machine, method);
            visitor.Visit(method.SourceLocation);
        }

        public override object VisitAnonEventHandler(PParser.AnonEventHandlerContext context)
        {
            return Visit(context.functionBody());
        }

        public override object VisitNoParamAnonEventHandler(PParser.NoParamAnonEventHandlerContext context)
        {
            return Visit(context.functionBody());
        }

        public override object VisitPFunDecl(PParser.PFunDeclContext context) { return Visit(context.functionBody()); }

        public override object VisitForeignFunDecl(PParser.ForeignFunDeclContext context) { return null; }

        public override object VisitFunctionBody(PParser.FunctionBodyContext context)
        {
            // TODO: check that parameters have been added to internal scope?

            // Add all local variables to scope.
            foreach (PParser.VarDeclContext varDeclContext in context.varDecl())
            {
                Visit(varDeclContext);
            }

            // Build the statement trees
            var statementVisitor = new StatementVisitor(handler, machine, method);
            method.Body = new CompoundStmt(context.statement().Select(statementVisitor.Visit).ToList());
            return null;
        }

        public override object VisitVarDecl(PParser.VarDeclContext context)
        {
            foreach (PParser.IdenContext varName in context.idenList()._names)
            {
                Variable variable = method.Scope.Put(varName.GetText(), context, varName);
                variable.Type = TypeResolver.ResolveType(context.type(), method.Scope, handler);
            }
            return null;
        }
    }
}
