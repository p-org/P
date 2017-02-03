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
        private partial class Events
        {
            public static PrtEventValue halt = new PrtEventValue(new PrtEvent("halt", new PrtNullType(), 1, false));
            public static PrtEventValue @null = new PrtEventValue(new PrtEvent("@null", new PrtNullType(), 1, false));
        }

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
            (isSafeMap).Add("Main", false);
            (isSafeMap).Add("Elevator", false);
            (isSafeMap).Add("Timer", false);
            (isSafeMap).Add("Door", false);
            (renameMap).Add("Main", "Main");
            (renameMap).Add("Elevator", "Elevator");
            (renameMap).Add("Timer", "Timer");
            (renameMap).Add("Door", "Door");
            (createMachineMap).Add("Main", CreateMachine_Main);
            (createMachineMap).Add("Elevator", CreateMachine_Elevator);
            (createMachineMap).Add("Timer", CreateMachine_Timer);
            (createMachineMap).Add("Door", CreateMachine_Door);
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("Main", "Main");
            (_temp).Add("Elevator", "Elevator");
            (_temp).Add("Door", "Door");
            (_temp).Add("Timer", "Timer");
            (linkMap).Add("Door", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Timer", "Timer");
            (_temp).Add("Elevator", "Elevator");
            (_temp).Add("Main", "Main");
            (_temp).Add("Door", "Door");
            (linkMap).Add("Timer", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Timer", "Timer");
            (_temp).Add("Elevator", "Elevator");
            (_temp).Add("Door", "Door");
            (_temp).Add("Main", "Main");
            (linkMap).Add("Elevator", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Main", "Main");
            (_temp).Add("Timer", "Timer");
            (_temp).Add("Door", "Door");
            (_temp).Add("Elevator", "Elevator");
            (linkMap).Add("Main", _temp);
            
        }
    }
}
