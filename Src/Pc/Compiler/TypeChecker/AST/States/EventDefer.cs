using Antlr4.Runtime;
using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.States
{
    public class EventDefer : IStateAction
    {
        public EventDefer(ParserRuleContext sourceLocation, PEvent trigger)
        {
            SourceLocation = sourceLocation;
            Trigger = trigger;
        }

        public ParserRuleContext SourceLocation { get; }
        public PEvent Trigger { get; }
    }
}
