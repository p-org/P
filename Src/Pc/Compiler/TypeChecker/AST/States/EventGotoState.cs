using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.States
{
    public class EventGotoState : IStateAction
    {
        public EventGotoState(PEvent trigger, State target, Function transitionFunction)
        {
            Trigger = trigger;
            Target = target;
            TransitionFunction = transitionFunction;
        }

        public State Target { get; }
        public Function TransitionFunction { get; }

        public PEvent Trigger { get; }
    }
}
