using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
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
                FunctionBodyVisitor.PopulateMethod(handler, machineFunction);
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

        private static IEnumerable<Function> AllFunctions(Scope globalScope)
        {
            foreach (Function fun in globalScope.Functions)
            {
                yield return fun;
            }

            foreach (Machine machine in globalScope.Machines)
            {
                foreach (Function method in machine.Methods)
                {
                    yield return method;
                }
                foreach (State state in machine.AllStates())
                {
                    if (state.Entry != null)
                    {
                        yield return state.Entry;
                    }
                    foreach (IStateAction stateAction in state.AllEventHandlers.Select(kv => kv.Value))
                    {
                        switch (stateAction)
                        {
                            case EventDoAction action:
                                if (action.Target != null)
                                {
                                    yield return action.Target;
                                }
                                break;
                            case EventGotoState action:
                                if (action.TransitionFunction != null)
                                {
                                    yield return action.TransitionFunction;
                                }
                                break;
                        }
                    }
                    if (state.Exit != null)
                    {
                        yield return state.Exit;
                    }
                }
            }
        }
    }
}
