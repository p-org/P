using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static void ForeignSteps(PMachineValue r, StateMachine machine)
        {
            machine.SendEvent(r, new ePing(1));
            AnnounceStep(2, machine);
            machine.Announce(new eStep(), (PInt)3);
            machine.SendEvent(r, new ePing(4));
        }
    }
}
