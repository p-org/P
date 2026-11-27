using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static void ForeignRaise(StateMachine machine)
        {
            machine.RaiseEvent(new eStep());
        }

        public static PInt ForeignRaiseInt(StateMachine machine)
        {
            machine.RaiseEvent(new eStep());
            return (PInt)42;
        }
    }

    internal partial class Main
    {
        public void ForeignGoto()
        {
            RaiseGotoStateEvent<Callback>();
        }

        public void ForeignCallback()
        {
            GotoDone();
        }
    }
}
