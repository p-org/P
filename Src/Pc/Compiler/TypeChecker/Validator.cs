using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    public class Validator
    {
        private readonly ParseTreeProperty<IPDecl> nodesToDeclarations;

        private Validator(ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            this.nodesToDeclarations = nodesToDeclarations;
        }

        private bool IsValid(EnumElem enumElem)
        {
            // every enum element should be found among its parent's elements
            // and the map should point to the correct declaration
            return enumElem.ParentEnum.Values.Contains(enumElem) &&
                   nodesToDeclarations.Get(enumElem.SourceLocation) == enumElem;
        }

        private bool IsValid(EventSet eventSet) { return nodesToDeclarations.Get(eventSet.SourceLocation) == eventSet; }

        private bool IsValid(Function function)
        {
            return function.Owner?.Methods.Contains(function) != false && // function properly registered with machine
                   function.Signature.ReturnType != null && // function signature has return type
                   function.Signature.Parameters
                           .All(param => param.Type != null) && // function signature parameters have types
                   nodesToDeclarations.Get(function.SourceLocation) == function; // map is bi-directional
        }

        private bool IsValid(Interface pInterface)
        {
            return pInterface.PayloadType != null && // interface has known payload type
                   nodesToDeclarations.Get(pInterface.SourceLocation) == pInterface;
        }

        private static IEnumerable<State> Flatten(IEnumerable<StateGroup> groups)
        {
            foreach (StateGroup group in groups)
            {
                foreach (State groupState in group.States)
                {
                    yield return groupState;
                }

                foreach (State subState in Flatten(group.Groups))
                {
                    yield return subState;
                }
            }
        }

        private bool IsValid(Machine machine)
        {
            var allStates = machine.States.Concat(Flatten(machine.Groups)).ToList();
            bool success = machine.Methods.All(fun => fun.Owner == machine);
            success &= machine.PayloadType != null;
            success &= machine.StartState != null;
            success &= allStates.Contains(machine.StartState);
            success &= allStates.All(st => !st.IsStart || st.IsStart && st == machine.StartState);
            success &= machine.Fields.All(v => v.Role.Equals(VariableRole.Param));
            success &= nodesToDeclarations.Get(machine.SourceLocation) == machine;
            return success;
        }

        private bool IsValid(PEnum pEnum)
        {
            // All of its values have the correct parent registered
            // There is a zero element
            // All elements are distinct
            return pEnum.Values.All(val => val.ParentEnum == pEnum) &&
                   pEnum.Values.Any(val => val.Value == 0) &&
                   pEnum.Values.Select(val => val.Value).Distinct().Count() == pEnum.Values.Count() &&
                   nodesToDeclarations.Get(pEnum.SourceLocation) == pEnum;
        }

        private bool IsValid(PEvent pEvent)
        {
            // special handling for special events
            if (pEvent.SourceLocation == null)
            {
                return pEvent.Name.Equals("halt") || pEvent.Name.Equals("null");
            }

            // check that reverse trips works
            return nodesToDeclarations.Get(pEvent.SourceLocation) == pEvent;
        }

        private bool IsValid(State state)
        {
            return state.Container.States.Contains(state) &&
                   state.AllEventHandlers.All(kv => kv.Value.Trigger == kv.Key) &&
                   nodesToDeclarations.Get(state.SourceLocation) == state;
        }

        private bool IsValid(StateGroup stateGroup)
        {
            return stateGroup.ParentStateContainer.Groups.Contains(stateGroup) &&
                   stateGroup.States.All(state => state.Container == stateGroup) &&
                   stateGroup.States.All(state => state.OwningMachine == stateGroup.OwningMachine) &&
                   stateGroup.Groups.All(group => group.OwningMachine == stateGroup.OwningMachine) &&
                   stateGroup.Groups.All(group => group.ParentStateContainer == stateGroup) &&
                   nodesToDeclarations.Get(stateGroup.SourceLocation) == stateGroup;
        }

        private bool IsValid(TypeDef typeDef)
        {
            return typeDef.Type != null &&
                   nodesToDeclarations.Get(typeDef.SourceLocation) == typeDef;
        }

        private bool IsValid(Variable variable)
        {
            return variable.Type != null &&
                   nodesToDeclarations.Get(variable.SourceLocation) == variable;
        }

        [Conditional("DEBUG")]
        public static void ValidateDeclarations(
            ParseTreeProperty<IPDecl> nodesToDeclarations,
            Scope topLevelTable,
            ITranslationErrorHandler handler)
        {
            var validator = new Validator(nodesToDeclarations);
            foreach (IPDecl decl in AllDeclarations(topLevelTable))
            {
                if (!validator.IsValid((dynamic) decl))
                {
                    throw handler.InternalError(decl.SourceLocation,
                                                $"malformed declaration {decl.Name} of type {decl.GetType().FullName}");
                }
            }
        }

        private static IEnumerable<IPDecl> AllDeclarations(Scope root)
        {
            foreach (IPDecl decl in root.AllDecls)
            {
                yield return decl;
            }
            foreach (Scope child in root.Children)
            {
                foreach (IPDecl subdecl in AllDeclarations(child))
                {
                    yield return subdecl;
                }
            }
        }

        public static void ValidateMachine(ITranslationErrorHandler handler, Machine machine)
        {
            State startState = FindStartState(machine, handler);
            PLanguageType startStatePayloadType = GetStatePayload(startState, handler);
            if (!startStatePayloadType.IsSameTypeAs(machine.PayloadType))
            {
                throw handler.InternalError(machine.SourceLocation,
                                            "machine payload type is not the same as start state's entry payload type");
            }

            foreach (Interface machineInterface in machine.Interfaces)
            {
                if (!machine.PayloadType.IsAssignableFrom(machineInterface.PayloadType))
                {
                    // TODO: add special "invalid machine interface" error
                    throw handler.TypeMismatch(machine.StartState.Entry?.SourceLocation ?? machine.SourceLocation,
                                               machine.PayloadType,
                                               machineInterface.PayloadType);
                }
            }
        }

        private static PLanguageType GetStatePayload(State startState, ITranslationErrorHandler handler)
        {
            if (!(startState.Entry?.Signature.Parameters.Count > 0))
            {
                return PrimitiveType.Null;
            }
            if (startState.Entry.Signature.Parameters.Count != 1)
            {
                throw handler.InternalError(startState.OwningMachine.SourceLocation,
                                            "Allowed start state entry with multiple parameters");
            }
            return startState.Entry.Signature.Parameters[0].Type;
        }

        private static State FindStartState(Machine machine, ITranslationErrorHandler handler)
        {
            bool foundStartState = false;
            foreach (State state in machine.AllStates())
            {
                if (state == machine.StartState || state.IsStart)
                {
                    if (!foundStartState)
                    {
                        foundStartState = true;
                    }
                    else
                    {
                        // TODO add source location to states
                        throw handler.InternalError(machine.SourceLocation,
                                                    $"Two start states {state.Name} occurs twice in all states list");
                    }
                }
            }

            if (foundStartState && machine.StartState == null)
            {
                throw handler.InternalError(machine.SourceLocation, "machine has unregistered start state");
            }

            if (!foundStartState || machine.StartState == null)
            {
                // Allow machines with no start state so long as there are no other states.
                throw handler.MissingStartState(machine);
            }

            return machine.StartState;
        }
    }
}
