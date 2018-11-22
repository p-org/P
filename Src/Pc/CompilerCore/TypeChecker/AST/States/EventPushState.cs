using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.States
{
    public class EventPushState : IStateAction
    {
        public EventPushState(ParserRuleContext sourceLocation, PEvent trigger, State target)
        {
            SourceLocation = sourceLocation;
            Trigger = trigger;
            Target = target;
        }

        public State Target { get; }

        public ParserRuleContext SourceLocation { get; }
        public PEvent Trigger { get; }
    }
}