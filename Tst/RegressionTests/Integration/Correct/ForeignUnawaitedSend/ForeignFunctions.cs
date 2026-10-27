using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static void ForeignSendUnawaited(PMachineValue target, StateMachine machine)
        {
            // Not recommended (foreign code should call SendEvent), but harmless.
            _ = machine.SendEventAsync(target, new ePing());
        }
    }
}
