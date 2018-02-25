using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker
{
    public static class MachineChecker
    {
        public static void Validate(ITranslationErrorHandler handler, Machine machine)
        {
            State startState = FindStartState(machine, handler);
            PLanguageType startStatePayloadType = GetStatePayload(startState, handler);
            if (!startStatePayloadType.IsSameTypeAs(machine.PayloadType))
            {
                throw handler.InternalError(machine.SourceLocation,
                                            "machine payload type is not the same as start state's entry payload type");
            }

            ValidateTransitions(handler, machine);
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
                                ValidateEventPayloadToTransitionTarget(handler, eventDoAction.SourceLocation,
                                                                       handledEvent.PayloadType, eventDoAction.Target);
                            }

                            break;
                        case EventGotoState eventGotoState:
                            if (eventGotoState.Target.Entry != null)
                            {
                                ValidateEventPayloadToTransitionTarget(handler, eventGotoState.SourceLocation,
                                                                       handledEvent.PayloadType,
                                                                       eventGotoState.Target.Entry);
                            }

                            if (eventGotoState.TransitionFunction != null)
                            {
                                ValidateEventPayloadToTransitionTarget(handler, eventGotoState.SourceLocation,
                                                                       handledEvent.PayloadType,
                                                                       eventGotoState.TransitionFunction);
                            }

                            break;
                        case EventPushState eventPushState:
                            if (eventPushState.Target.Entry != null)
                            {
                                ValidateEventPayloadToTransitionTarget(handler, eventPushState.SourceLocation,
                                                                       handledEvent.PayloadType,
                                                                       eventPushState.Target.Entry);
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

        private static void ValidateEventPayloadToTransitionTarget(ITranslationErrorHandler handler,
                                                                   ParserRuleContext sourceLocation,
                                                                   PLanguageType eventPayloadType,
                                                                   Function targetFunction)
        {
            IReadOnlyList<PLanguageType> entrySignature = targetFunction.Signature.ParameterTypes.ToList();
            if (entrySignature.Count == 0)
            {
                // We ignore payloads that go to states with no entry parameter
                return;
            }

            if (entrySignature.Count == 1 && entrySignature[0].IsAssignableFrom(eventPayloadType))
            {
                // If the single argument matches the payload type, then that's it.
                // This has higher precedence, too, since `any` arguments should receive
                // the entire payload.
                return;
            }

            if (entrySignature.Count == 1 && eventPayloadType.Canonicalize() is TupleType tuple &&
                tuple.Types.Count == 1 && entrySignature[0].IsAssignableFrom(tuple.Types[0]))
            {
                // handles the case where we have something like:
                // event E : (int);
                // on E do (x : int) { ... }
                // this is silly, but should be allowable since tuples are unpacked in:
                // event E2 : (int, bool);
                // on E2 do (x : int, b : bool) { ... }
                return;
            }

            if (entrySignature.Count == 1)
            {
                throw handler.TypeMismatch(sourceLocation, eventPayloadType, entrySignature[0]);
            }

            PLanguageType entrySignatureType = new TupleType(entrySignature.ToArray());
            if (!entrySignatureType.IsAssignableFrom(eventPayloadType))
            {
                throw handler.TypeMismatch(sourceLocation, eventPayloadType, entrySignatureType);
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
