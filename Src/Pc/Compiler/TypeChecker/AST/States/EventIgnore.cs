using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.States
{
    public class EventIgnore : IStateAction
    {
        public EventIgnore(PEvent trigger) { Trigger = trigger; }

        public PEvent Trigger { get; }
    }
}
