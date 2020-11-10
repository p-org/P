using Antlr4.Runtime.Tree;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plang.Compiler.TypeChecker
{
    public static class Analyzer
    {
        public static Scope AnalyzeCompilationUnit(ITranslationErrorHandler handler,
            params PParser.ProgramContext[] programUnits)
        {
            // Step 1: Build the global scope of declarations
            Scope globalScope = BuildGlobalScope(handler, programUnits);

            // Step 2: Validate machine specifications
            foreach (Machine machine in globalScope.Machines)
            {
                MachineChecker.Validate(handler, machine);
            }

            // Step 3: Fill function bodies
            List<Function> allFunctions = globalScope.GetAllMethods().ToList();
            foreach (Function machineFunction in allFunctions)
            {
                FunctionBodyVisitor.PopulateMethod(handler, machineFunction);
                FunctionValidator.CheckAllPathsReturn(handler, machineFunction);
            }

            // Step 2: Validate no static handlers
            foreach (Machine machine in globalScope.Machines)
            {
                MachineChecker.ValidateNoStaticHandlers(handler, machine);
            }

            // Step 4: Propagate purity properties
            ApplyPropagations(allFunctions,
                CreatePropagation(fn => fn.CanRaiseEvent, (fn, value) => fn.CanRaiseEvent = value,
                    true),
                CreatePropagation(fn => fn.CanReceive, (fn, value) => fn.CanReceive = value,
                    true),
                CreatePropagation(fn => fn.CanChangeState, (fn, value) => fn.CanChangeState = value,
                    true));

            // Step 5: Verify capability restrictions
            foreach (Function machineFunction in allFunctions)
            {
                // TODO: is this checked earlier?
                if (machineFunction.Owner?.IsSpec == true && machineFunction.IsNondeterministic == true)
                {
                    throw handler.NonDeterministicFunctionInSpecMachine(machineFunction);
                }

                if ((machineFunction.CanChangeState == true || machineFunction.CanRaiseEvent == true) &&
                    (machineFunction.Role.HasFlag(FunctionRole.TransitionFunction) ||
                     machineFunction.Role.HasFlag(FunctionRole.ExitHandler)))
                {
                    throw handler.ChangedStateMidTransition(machineFunction.SourceLocation, machineFunction);
                }
            }

            // Step 6: Check linear type ownership
            // LinearTypeChecker.AnalyzeMethods(handler, allFunctions);

            // Step 7: Check control flow well-formedness
            ControlFlowChecker.AnalyzeMethods(handler, allFunctions);

            // Step 8: Infer the creates set for each machine.
            foreach (Machine machine in globalScope.Machines)
            {
                InferMachineCreates.Populate(machine, handler);
            }

            // Step 9: Fill the module expressions
            ModuleSystemDeclarations.PopulateAllModuleExprs(handler, globalScope);

            ModuleSystemTypeChecker moduleTypeChecker = new ModuleSystemTypeChecker(handler, globalScope);
            // Step 10: Check that all module expressions are well-formed
            foreach (IPModuleExpr moduleExpr in AllModuleExprs(globalScope))
            {
                moduleTypeChecker.CheckWellFormedness(moduleExpr);
            }

            // Step 11: Check the test and implementation declarations
            foreach (Implementation impl in globalScope.Implementations)
            {
                moduleTypeChecker.CheckImplementationDecl(impl);
            }

            foreach (SafetyTest test in globalScope.SafetyTests)
            {
                moduleTypeChecker.CheckSafetyTest(test);
            }

            foreach (RefinementTest test in globalScope.RefinementTests)
            {
                moduleTypeChecker.CheckRefinementTest(test);
            }

            return globalScope;
        }

        private static Propagation<T> CreatePropagation<T>(Func<Function, T> getter, Action<Function, T> setter,
            T value)
        {
            return new Propagation<T>
            {
                Getter = getter,
                Setter = setter,
                ActiveValue = value
            };
        }

        private static void ApplyPropagations<T>(IEnumerable<Function> functions, params Propagation<T>[] propagations)
        {
            foreach (Function function in functions)
            {
                foreach (Propagation<T> propagation in propagations)
                {
                    if (propagation.Getter(function).Equals(propagation.ActiveValue))
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
                        if (!propagation.Getter(caller).Equals(propagation.ActiveValue))
                        {
                            propagation.Setter(caller, propagation.ActiveValue);
                            propagation.PropertyStack.Push(caller);
                        }
                    }
                }
            }
        }

        private static Scope BuildGlobalScope(ITranslationErrorHandler handler, PParser.ProgramContext[] programUnits)
        {
            Scope globalScope = Scope.CreateGlobalScope(handler);
            ParseTreeProperty<IPDecl> nodesToDeclarations = new ParseTreeProperty<IPDecl>();

            // Add built-in events to the table.
            globalScope.Put("null", (PParser.EventDeclContext)null);
            globalScope.Put("halt", (PParser.EventDeclContext)null);

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

        private static IEnumerable<IPModuleExpr> AllModuleExprs(Scope globalScope)
        {
            // first do all the named modules
            foreach (NamedModule mod in globalScope.NamedModules)
            {
                yield return mod.ModExpr;
            }

            // all the test declarations
            foreach (SafetyTest test in globalScope.SafetyTests)
            {
                yield return test.ModExpr;
            }

            foreach (RefinementTest test in globalScope.RefinementTests)
            {
                yield return test.LeftModExpr;
            }

            foreach (RefinementTest test in globalScope.RefinementTests)
            {
                yield return test.RightModExpr;
            }

            // all the implementations
            foreach (Implementation impl in globalScope.Implementations)
            {
                yield return impl.ModExpr;
            }
        }

        private class Propagation<T>
        {
            public Stack<Function> PropertyStack { get; } = new Stack<Function>();
            public Func<Function, T> Getter { get; set; }
            public Action<Function, T> Setter { get; set; }
            public T ActiveValue { get; set; }
        }
    }
}