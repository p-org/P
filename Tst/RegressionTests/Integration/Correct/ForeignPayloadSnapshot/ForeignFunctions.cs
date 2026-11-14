using PChecker.Runtime.StateMachines;
using PChecker.Runtime.Values;

namespace PImplementation
{
    public static partial class GlobalFunctions
    {
        public static void ForeignSendAndChange(PMachineValue r, StateMachine machine)
        {
            var sent = new PSeq();
            machine.SendEvent(r, new eSeq(sent));
            sent.Add((PInt)1);

            var announced = new PSeq();
            machine.Announce(new eAnnounced(), announced);
            announced.Add((PInt)2);

            var created = new PSeq();
            machine.CreateInterface<I_Created>(machine, created);
            created.Add((PInt)3);
        }
    }
}
