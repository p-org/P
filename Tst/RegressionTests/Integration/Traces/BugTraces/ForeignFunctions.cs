using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        // Static, like ID generators and caches in foreign code: it is not reset between
        // schedules, so a replay of a schedule sees another value than the schedule did.
        public static int Counter;

        public static PInt NextCounter(StateMachine machine)
        {
            Counter++;
            return (PInt)Counter;
        }
    }
}
