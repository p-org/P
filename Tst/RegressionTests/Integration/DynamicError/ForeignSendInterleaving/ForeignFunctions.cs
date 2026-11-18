using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static void ForeignSend(PMachineValue target, PInt v, StateMachine machine)
        {
            machine.SendEvent(target, new eA(v));
        }
    }
}
