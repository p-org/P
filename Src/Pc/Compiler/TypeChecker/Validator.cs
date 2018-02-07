using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    public static class Validator
    {
        public static void ValidateMachine(ITranslationErrorHandler handler, Machine machine)
        {
            State startState = FindStartState(machine, handler);
            PLanguageType startStatePayloadType = GetStatePayload(startState, handler);
            if (!startStatePayloadType.IsSameTypeAs(machine.PayloadType))
            {
                throw handler.InternalError(machine.SourceLocation,
                                            "machine payload type is not the same as start state's entry payload type");
            }

            ValidateTransitions(handler, machine);
            ValidateInterfaces(handler, machine);
        }

        private static void ValidateInterfaces(ITranslationErrorHandler handler, Machine machine)
        {
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

        private static void ValidateTransitions(ITranslationErrorHandler handler, Machine machine)
        {
            foreach (State state in machine.AllStates())
            {
                foreach (KeyValuePair<PEvent, IStateAction> pair in state.AllEventHandlers)
                {
                    PEvent handledEvent = pair.Key;
                    switch (pair.Value)
                    {
                        case EventDoAction eventDoAction:
                            if (eventDoAction.Target != null)
                            {
                                TypeCheckingUtils.ValidatePayloadTypes(handler, eventDoAction.SourceLocation,
                                                                       handledEvent.PayloadType,
                                                                       eventDoAction
                                                                           .Target.Signature.ParameterTypes.ToList());
                            }

                            break;
                        case EventGotoState eventGotoState:
                            if (eventGotoState.Target.Entry != null)
                            {
                                TypeCheckingUtils.ValidatePayloadTypes(handler, eventGotoState.SourceLocation,
                                                                       handledEvent.PayloadType,
                                                                       eventGotoState
                                                                           .Target.Entry.Signature.ParameterTypes
                                                                           .ToList());
                            }

                            if (eventGotoState.TransitionFunction != null)
                            {
                                TypeCheckingUtils.ValidatePayloadTypes(handler, eventGotoState.SourceLocation,
                                                                       handledEvent.PayloadType,
                                                                       eventGotoState
                                                                           .TransitionFunction.Signature.ParameterTypes
                                                                           .ToList());
                            }

                            break;
                    }
                }

                if (state.Exit?.Signature.Parameters.Count > 0)
                {
                    throw handler.IncorrectArgumentCount(state.SourceLocation, state.Exit.Signature.Parameters.Count,
                                                         0);
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
            var foundStartState = false;
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
                        throw handler.InternalError(state.SourceLocation,
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
