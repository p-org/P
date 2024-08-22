using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.States
{
    public class EventDefer : IStateAction
    {
        public EventDefer(ParserRuleContext sourceLocation, Event trigger)
        {
            SourceLocation = sourceLocation;
            Trigger = trigger;
        }

        public ParserRuleContext SourceLocation { get; }
        public Event Trigger { get; }
    }
}