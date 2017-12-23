using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.States
{
    public class EventGotoState : IStateAction
    {
        public EventGotoState(ParserRuleContext sourceLocation, PEvent trigger, State target,
                              Function transitionFunction)
        {
            SourceLocation = sourceLocation;
            Trigger = trigger;
            Target = target;
            TransitionFunction = transitionFunction;
        }

        public State Target { get; }
        public Function TransitionFunction { get; }

        public ParserRuleContext SourceLocation { get; }
        public PEvent Trigger { get; }
    }
}
