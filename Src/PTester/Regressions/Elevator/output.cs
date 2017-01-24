#pragma warning disable CS0162, CS0164, CS0168
using P.Runtime;
using System;
using System.Collections.Generic;

namespace P.Program
{
    using P.Runtime;
    using System;
    using System.Collections.Generic;

    public partial class Application : StateImpl
    {
        public Application(): base ()
        {
        }

        public Application(bool initialize): base ()
        {
            CreateMainMachine();
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        static Application()
        {
            (isSafeMap).Add("Door", false);
            (isSafeMap).Add("Timer", false);
            (isSafeMap).Add("Main", false);
            (isSafeMap).Add("Elevator", false);
            (renameMap).Add("Door", "Door");
            (renameMap).Add("Timer", "Timer");
            (renameMap).Add("Main", "Main");
            (renameMap).Add("Elevator", "Elevator");
            (createMachineMap).Add("Door", CreateMachine_Door);
            (createMachineMap).Add("Timer", CreateMachine_Timer);
            (createMachineMap).Add("Main", CreateMachine_Main);
            (createMachineMap).Add("Elevator", CreateMachine_Elevator);
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("Elevator", "Elevator");
            (_temp).Add("Main", "Main");
            (_temp).Add("Door", "Door");
            (_temp).Add("Timer", "Timer");
            (linkMap).Add("Elevator", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Main", "Main");
            (_temp).Add("Door", "Door");
            (_temp).Add("Elevator", "Elevator");
            (_temp).Add("Timer", "Timer");
            (linkMap).Add("Main", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Elevator", "Elevator");
            (_temp).Add("Timer", "Timer");
            (_temp).Add("Door", "Door");
            (_temp).Add("Main", "Main");
            (linkMap).Add("Timer", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Door", "Door");
            (_temp).Add("Main", "Main");
            (_temp).Add("Elevator", "Elevator");
            (_temp).Add("Timer", "Timer");
            (linkMap).Add("Door", _temp);
        }
    }
}
