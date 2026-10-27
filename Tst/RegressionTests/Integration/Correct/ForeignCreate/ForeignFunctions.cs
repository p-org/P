using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static PMachineValue ForeignCreate(PInt v, StateMachine machine)
        {
            return machine.CreateInterface<I_Worker>(machine, v);
        }
    }
}
