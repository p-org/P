using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Plang.Compiler.TypeChecker
{
    public static class MachineChecker
    {
        public static void Validate(ITranslationErrorHandler handler, Machine machine)
        {
            State startState = FindStartState(machine, handler);
            PLanguageType startStatePayloadType = GetStatePayload(startState);
            Debug.Assert(startStatePayloadType.IsSameTypeAs(machine.PayloadType));
            ValidateTransitions(handler, machine);
        }

        public static void ValidateNoStaticHandlers(ITranslationErrorHandler handler, Machine machine)
        {
            foreach (State state in machine.AllStates())
            {
                bool illegalUsage = state.Entry != null && IsStaticOrForeign(state.Entry);
                if (illegalUsage)
                {
                    throw handler.StaticFunctionNotAllowedAsHandler(state.SourceLocation,
                        state.Entry.Name);
                }

                illegalUsage = state.Exit != null && IsStaticOrForeign(state.Exit);
                if (illegalUsage)
                {
                    throw handler.StaticFunctionNotAllowedAsHandler(state.SourceLocation,
                        state.Exit.Name);
                }

                foreach (KeyValuePair<PEvent, AST.IStateAction> pair in state.AllEventHandlers)
                {
                    switch (pair.Value)
                    {
                        case EventDoAction eventDoAction:
                            if (eventDoAction.Target != null && IsStaticOrForeign(eventDoAction.Target))
                            {
                                throw handler.StaticFunctionNotAllowedAsHandler(eventDoAction.SourceLocation,
                                    eventDoAction.Target.Name);
                            }

                            break;

                        case EventGotoState eventGotoState:
                            if (eventGotoState.TransitionFunction != null &&
                                IsStaticOrForeign(eventGotoState.TransitionFunction))
                            {
                                throw handler.StaticFunctionNotAllowedAsHandler(eventGotoState.SourceLocation,
                                    eventGotoState.TransitionFunction.Name);
                            }

                            break;
                    }
                }
            }
        }

        private static bool IsStaticOrForeign(Function function)
        {
            return function.Owner == null || function.IsForeign;
        }

        private static void ValidateTransitions(ITranslationErrorHandler handler, Machine machine)
        {
            foreach (State state in machine.AllStates())
            {
                foreach (KeyValuePair<PEvent, AST.IStateAction> pair in state.AllEventHandlers)
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
                return;
            }

            if (entrySignature.Count == 1 && entrySignature[0].IsAssignableFrom(eventPayloadType))
            {
                return;
            }

            if (entrySignature.Count == 1 && eventPayloadType.Canonicalize() is TupleType tuple &&
                tuple.Types.Count == 1 && entrySignature[0].IsAssignableFrom(tuple.Types[0]))
            {
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

        private static PLanguageType GetStatePayload(State startState)
        {
            if (!(startState.Entry?.Signature.Parameters.Count > 0))
            {
                return PrimitiveType.Null;
            }

            Debug.Assert(startState.Entry.Signature.Parameters.Count == 1,
                "Allowed start state entry with multiple parameters");
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
                        throw handler.TwoStartStates(machine, state);
                    }
                }
            }

            Debug.Assert(!(foundStartState && machine.StartState == null), "machine has unregistered start state");

            if (!foundStartState || machine.StartState == null)
            {
                throw handler.MissingStartState(machine);
            }

            return machine.StartState;
        }
    }
}