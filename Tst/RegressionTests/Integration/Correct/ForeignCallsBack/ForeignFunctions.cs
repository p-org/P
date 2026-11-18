using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static PMachineValue ForeignCallsBack(PMachineValue r, PInt v, StateMachine machine)
        {
            // Foreign code written for synchronous P functions drops the task they now return.
#pragma warning disable CS4014
            SendPing(r, v + 1, machine);
#pragma warning restore CS4014
            machine.SendEvent(r, new ePing(v + 2));
            SendPing(r, v + 3, machine).GetAwaiter().GetResult();

            // The created receiver gets one ping, requested before the receiver is actually created.
            var created = CreateReceiver(machine).Result;
            machine.SendEvent(created, new ePing(1));
            return created;
        }
    }
}
