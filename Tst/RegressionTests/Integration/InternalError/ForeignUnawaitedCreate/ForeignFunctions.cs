using PChecker.Runtime.Values;

namespace PImplementation
{
    internal partial class Main
    {
        public void CreateWithoutAwaiting(PMachineValue r)
        {
            // Wrong: CreateStateMachineAsync is a scheduling point, so it must be awaited.
            _ = CreateStateMachineAsync(typeof(Worker), "Worker");
            SendEvent(r, new ePing());
        }
    }
}
