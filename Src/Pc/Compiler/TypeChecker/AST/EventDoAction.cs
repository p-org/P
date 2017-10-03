namespace Microsoft.Pc.TypeChecker.AST
{
    public class EventDoAction : IStateAction
    {
        public EventDoAction(PEvent trigger, Function target)
        {
            Trigger = trigger;
            Target = target;
        }

        public Function Target { get; }
        public PEvent Trigger { get; }
    }
}
