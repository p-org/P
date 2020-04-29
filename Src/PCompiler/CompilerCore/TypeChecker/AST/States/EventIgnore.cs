using Antlr4.Runtime;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker.AST.States
{
    public class EventIgnore : IStateAction
    {
        public EventIgnore(ParserRuleContext sourceLocation, PEvent trigger)
        {
            SourceLocation = sourceLocation;
            Trigger = trigger;
        }

        public ParserRuleContext SourceLocation { get; }
        public PEvent Trigger { get; }
    }
}