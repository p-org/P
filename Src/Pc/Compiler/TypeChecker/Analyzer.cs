using System;
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

            // NOW: no declarations have ambiguous names.
            // NOW: there is exactly one declaration object for each declaration.
            // NOW: every declaration object is associated in both directions with its corresponding parse tree node.
            // NOW: enums and their elements are related to one another

            // Step 2: Validate declarations and fill with types
            foreach (PParser.ProgramContext programUnit in programUnits)
            {
                DeclarationVisitor.PopulateDeclarations(handler, globalScope, programUnit, nodesToDeclarations);
            }

            // NOW: all declarations are valid, with appropriate links and types resolved.

            // Step 3: Validate machine specifications
            foreach (Machine machine in globalScope.Machines)
            {
                Validator.ValidateMachine(machine, handler);
            }

            // NOW: everything except for function bodies have been filled and validated.

            // Step 4: Fill function bodies
            PopulateFunctionBodies(handler, globalScope);

            // NOW: AST Complete, pass to StringTemplate
            return new PProgramModel
            {
                GlobalScope = globalScope
            };
        }

        private static void PopulateFunctionBodies(ITranslationErrorHandler handler, Scope globalScope)
        {
            foreach (Function fun in globalScope.Functions)
            {
                FunctionBodyVisitor.PopulateMethod(handler, fun);
            }

            foreach (Machine machine in globalScope.Machines)
            {
                foreach (Function method in machine.Methods)
                {
                    FunctionBodyVisitor.PopulateMethod(handler, machine, method);
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
                                    FunctionBodyVisitor.PopulateMethod(handler, machine, action.Target);
                                }
                                break;
                            case EventGotoState action:
                                if (action.TransitionFunction != null)
                                {
                                    FunctionBodyVisitor.PopulateMethod(handler, machine, action.TransitionFunction);
                                }
                                break;
                        }
                    }
                }
            }
        }
    }

    public class FunctionBodyVisitor
    {
        public static void PopulateMethod(ITranslationErrorHandler handler, Function fun)
        {
            PopulateMethod(handler, null, fun);
        }

        public static void PopulateMethod(ITranslationErrorHandler handler, Machine machine, Function method)
        {
            if (method.Body != null)
            {
                return;
            }

            Debug.Assert(method.Owner == machine);
            AddLocalVariables(handler, method);
            var statementVisitor = new StatementVisitor(method.Scope, machine, handler);
            method.Body = statementVisitor.Visit(method.SourceLocation);
        }

        private static void AddLocalVariables(ITranslationErrorHandler handler, Function method)
        {
            if (GetFunctionBody(handler, method)?.varDecl() is PParser.VarDeclContext[] varDecls)
            {
                foreach (var varDecl in varDecls)
                {
                    foreach (var varName in varDecl.idenList()._names)
                    {
                        var variable = method.Scope.Put(varName.GetText(), varDecl, varName);
                        variable.Type = TypeResolver.ResolveType(varDecl.type(), method.Scope, handler);
                    }
                }
            }
        }

        private static PParser.FunctionBodyContext GetFunctionBody(ITranslationErrorHandler handler, Function fun)
        {
            switch (fun.SourceLocation)
            {
                case PParser.AnonEventHandlerContext ctx:
                    return ctx.functionBody();
                case PParser.NoParamAnonEventHandlerContext ctx:
                    return ctx.functionBody();
                case PParser.PFunDeclContext ctx:
                    return ctx.functionBody();
                case PParser.ForeignFunDeclContext ctx:
                    return null;
                default:
                    throw handler.InternalError(fun.SourceLocation, "function is of unexpected parse tree node type");
            }
        }
    }
}
