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
            public static PrtEventValue @null = new PrtEventValue(new PrtEvent("null", new PrtNullType(), 1, false));
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
            (isSafeMap).Add("LEDMachine", false);
            (isSafeMap).Add("TimerMachine", false);
            (isSafeMap).Add("UserMachine", false);
            (isSafeMap).Add("SwitchMachine", false);
            (isSafeMap).Add("OSRDriverMachine", false);
            (renameMap).Add("LEDMachine", "LEDMachine");
            (renameMap).Add("TimerMachine", "TimerMachine");
            (renameMap).Add("UserMachine", "UserMachine");
            (renameMap).Add("SwitchMachine", "SwitchMachine");
            (renameMap).Add("OSRDriverMachine", "OSRDriverMachine");
            (createMachineMap).Add("LEDMachine", CreateMachine_LEDMachine);
            (createMachineMap).Add("TimerMachine", CreateMachine_TimerMachine);
            (createMachineMap).Add("UserMachine", CreateMachine_UserMachine);
            (createMachineMap).Add("SwitchMachine", CreateMachine_SwitchMachine);
            (createMachineMap).Add("OSRDriverMachine", CreateMachine_OSRDriverMachine);
            interfaceMap["TimerInterface"] = new List<PrtEventValue>()
            {(Events).eStartDebounceTimer, (Events).eStopTimer};
            interfaceMap["LEDInterface"] = new List<PrtEventValue>()
            {(Events).eSetLedStateToStableUsingControlTransfer, (Events).eSetLedStateToUnstableUsingControlTransfer, (Events).eUpdateBarGraphStateUsingControlTransfer};
            interfaceMap["OSRDriverInterface"] = new List<PrtEventValue>()
            {(Events).eTimerStopped, (Events).eTimerFired, (Events).eSwitchStatusChange, (Events).eStoppingSuccess, (Events).eStoppingFailure, (Events).eOperationSuccess, (Events).eD0Exit, (Events).eD0Entry, (Events).eTransferSuccess, (Events).eTransferFailure};
            interfaceMap["SwitchInterface"] = new List<PrtEventValue>()
            {(Events).eYes};
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("OSRDriverMachine", "OSRDriverMachine");
            (_temp).Add("SwitchMachine", "SwitchMachine");
            (_temp).Add("UserMachine", "UserMachine");
            (_temp).Add("TimerMachine", "TimerMachine");
            (_temp).Add("LEDMachine", "LEDMachine");
            (linkMap).Add("UserMachine", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("OSRDriverMachine", "OSRDriverMachine");
            (_temp).Add("TimerMachine", "TimerMachine");
            (_temp).Add("SwitchMachine", "SwitchMachine");
            (_temp).Add("UserMachine", "UserMachine");
            (_temp).Add("LEDMachine", "LEDMachine");
            (linkMap).Add("TimerMachine", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("OSRDriverMachine", "OSRDriverMachine");
            (_temp).Add("TimerMachine", "TimerMachine");
            (_temp).Add("SwitchInterface", "SwitchMachine");
            (_temp).Add("LEDInterface", "LEDMachine");
            (_temp).Add("TimerInterface", "TimerMachine");
            (_temp).Add("SwitchMachine", "SwitchMachine");
            (_temp).Add("UserMachine", "UserMachine");
            (_temp).Add("LEDMachine", "LEDMachine");
            (linkMap).Add("OSRDriverMachine", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("OSRDriverMachine", "OSRDriverMachine");
            (_temp).Add("UserMachine", "UserMachine");
            (_temp).Add("LEDMachine", "LEDMachine");
            (_temp).Add("SwitchMachine", "SwitchMachine");
            (_temp).Add("TimerMachine", "TimerMachine");
            (linkMap).Add("LEDMachine", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("SwitchMachine", "SwitchMachine");
            (_temp).Add("TimerMachine", "TimerMachine");
            (_temp).Add("OSRDriverMachine", "OSRDriverMachine");
            (_temp).Add("UserMachine", "UserMachine");
            (_temp).Add("LEDMachine", "LEDMachine");
            (linkMap).Add("SwitchMachine", _temp);
        }
    }
}
