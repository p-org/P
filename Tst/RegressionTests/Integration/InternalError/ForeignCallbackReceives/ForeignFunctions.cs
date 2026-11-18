using PChecker.Runtime.StateMachines;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static void ForeignCallsReceiver(StateMachine machine)
        {
            // Wrong: P functions called from foreign code must not receive.
            _ = WaitForPing(machine);
        }
    }
}
