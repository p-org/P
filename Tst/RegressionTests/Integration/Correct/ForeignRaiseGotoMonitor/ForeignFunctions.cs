using PChecker.Runtime.Specifications;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static void MonitorForeignRaise(Monitor monitor)
        {
            monitor.RaiseEvent(new eMonStep());
        }

        public static PInt MonitorForeignRaiseInt(Monitor monitor)
        {
            monitor.RaiseEvent(new eMonStep());
            return (PInt)42;
        }
    }
}
