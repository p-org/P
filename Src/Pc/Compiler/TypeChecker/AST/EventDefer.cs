namespace Microsoft.Pc.TypeChecker.AST
{
    public class EventDefer : IStateAction
    {
        public EventDefer(PEvent trigger) { Trigger = trigger; }

        public PEvent Trigger { get; }
    }
}
