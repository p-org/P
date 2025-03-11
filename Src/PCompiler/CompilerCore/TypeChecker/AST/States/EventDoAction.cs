using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.States
{
    public class EventDoAction : IStateAction
    {
        public EventDoAction(ParserRuleContext sourceLocation, Event trigger, Function target)
        {
            SourceLocation = sourceLocation;
            Trigger = trigger;
            Target = target;
        }

        public Function Target { get; }
        public ParserRuleContext SourceLocation { get; }
        public Event Trigger { get; }
    }
}