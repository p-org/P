namespace Microsoft.Pc.TypeChecker.AST
{
    public class EventIgnore : IStateAction
    {
        public EventIgnore(PEvent trigger) { Trigger = trigger; }

        public PEvent Trigger { get; }
    }
}
