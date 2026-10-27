using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static void ForeignSendUnawaited(PMachineValue target, StateMachine machine)
        {
            // Wrong: SendEventAsync is a scheduling point and must be awaited.
            _ = machine.SendEventAsync(target, new ePing());
        }
    }
}
