using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static PBool NewEventsHaveNoPayload(StateMachine machine)
        {
            var withPayload = new eInt((PInt)5);
            return new eInt().Payload == null &&
                   new eTuple().Payload == null &&
                   new eDone().Payload == null &&
                   ((eInt)withPayload.Clone()).Payload == null &&
                   ((PInt)withPayload.Payload) == 5;
        }
    }
}
