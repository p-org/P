using Microsoft.Pc.TypeChecker.AST.Declarations;

namespace Microsoft.Pc.TypeChecker.AST.States
{
    public class EventDefer : IStateAction
    {
        public EventDefer(PEvent trigger) { Trigger = trigger; }

        public PEvent Trigger { get; }
    }
}
