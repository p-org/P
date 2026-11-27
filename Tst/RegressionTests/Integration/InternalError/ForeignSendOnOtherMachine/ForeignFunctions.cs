using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        private static StateMachine saved;

        public static void SaveMachine(StateMachine machine)
        {
            saved = machine;
        }

        public static void SendThroughSavedMachine(PMachineValue target, StateMachine machine)
        {
            // Wrong: foreign code may only send through the machine passed to it.
            saved.SendEvent(target, new ePing());
        }
    }
}
