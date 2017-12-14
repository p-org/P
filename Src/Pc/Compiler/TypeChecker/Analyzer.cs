using System;
using System.Collections.Generic;
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
        public static Scope AnalyzeCompilationUnit(ITranslationErrorHandler handler, params PParser.ProgramContext[] programUnits)
        {
            // Step 1: Build the global scope of declarations
            Scope globalScope = BuildGlobalScope(handler, programUnits);

            // Step 2: Validate machine specifications
            foreach (Machine machine in globalScope.Machines)
            {
                Validator.ValidateMachine(handler, machine);
            }

            // Step 3: Fill function bodies
            List<Function> allFunctions = AllFunctions(globalScope).ToList();
            foreach (Function machineFunction in allFunctions)
            {
                FunctionBodyVisitor.PopulateMethod(handler, machineFunction);
            }

            // Step 4: Propagate purity properties
            ApplyPropagations(allFunctions,
                              CreatePropagation(fn => fn.CanCommunicate, (fn, value) => fn.CanCommunicate = value,
                                                true),
                              CreatePropagation(fn => fn.CanChangeState, (fn, value) => fn.CanChangeState = value,
                                                true));

            // Step 5: Verify purity invariants
            foreach (Function machineFunction in allFunctions)
            {
                if (machineFunction.Owner?.IsSpec == true && machineFunction.IsNondeterministic == true)
                {
                    throw handler.NonDeterministicFunctionInSpecMachine(machineFunction);
                }
                if (machineFunction.CanChangeState == true &&
                    (machineFunction.Role.HasFlag(FunctionRole.TransitionFunction) ||
                     machineFunction.Role.HasFlag(FunctionRole.ExitHandler)))
                {
                    throw handler.ChangedStateMidTransition(machineFunction.SourceLocation, machineFunction);
                }
            }

            // Step 6: Check linear type ownership
            LinearTypeChecker.AnalyzeMethods(handler, allFunctions);

            return globalScope;
        }

        private static Propagation<T> CreatePropagation<T>(Func<Function, T> getter, Action<Function, T> setter,
                                                           T value)
        {
            return new Propagation<T>
            {
                PropertyGetter = getter,
                PropertySetter = setter,
                ActiveValue = value
            };
        }

        private static void ApplyPropagations<T>(IEnumerable<Function> functions, params Propagation<T>[] propagations)
        {
            foreach (Function function in functions)
            {
                foreach (Propagation<T> propagation in propagations)
                {
                    if (propagation.PropertyGetter(function).Equals(propagation.ActiveValue))
                    {
                        propagation.PropertyStack.Push(function);
                    }
                }
            }
            foreach (Propagation<T> propagation in propagations)
            {
                while (propagation.PropertyStack.Any())
                {
                    foreach (Function caller in propagation.PropertyStack.Pop().Callers)
                    {
                        if (!propagation.PropertyGetter(caller).Equals(propagation.ActiveValue))
                        {
                            propagation.PropertySetter(caller, propagation.ActiveValue);
                            propagation.PropertyStack.Push(caller);
                        }
                    }
                }
            }
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
                // TODO: save these to field containing anonymous methods
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

        private class Propagation<T>
        {
            public Stack<Function> PropertyStack { get; } = new Stack<Function>();
            public Func<Function, T> PropertyGetter { get; set; }
            public Action<Function, T> PropertySetter { get; set; }
            public T ActiveValue { get; set; }
        }
    }
}