namespace Microsoft.Pc.TypeChecker.AST
{
    public class EventPushState : IStateAction
    {
        public EventPushState(PEvent trigger, State target)
        {
            Trigger = trigger;
            Target = target;
        }

        public State Target { get; }

        public PEvent Trigger { get; }
    }
}
