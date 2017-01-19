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
            (isSafeMap).Add("LEDMachine", false);
            (isSafeMap).Add("TimerMachine", false);
            (isSafeMap).Add("SwitchMachine", false);
            (isSafeMap).Add("OSRDriverMachine", false);
            (isSafeMap).Add("UserMachine", false);
            (renameMap).Add("LEDMachine", "LEDMachine");
            (renameMap).Add("TimerMachine", "TimerMachine");
            (renameMap).Add("SwitchMachine", "SwitchMachine");
            (renameMap).Add("OSRDriverMachine", "OSRDriverMachine");
            (renameMap).Add("UserMachine", "UserMachine");
            (createMachineMap).Add("LEDMachine", CreateMachine_LEDMachine);
            (createMachineMap).Add("TimerMachine", CreateMachine_TimerMachine);
            (createMachineMap).Add("SwitchMachine", CreateMachine_SwitchMachine);
            (createMachineMap).Add("OSRDriverMachine", CreateMachine_OSRDriverMachine);
            (createMachineMap).Add("UserMachine", CreateMachine_UserMachine);
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("TimerMachine", "TimerMachine");
            (_temp).Add("LEDMachine", "LEDMachine");
            (_temp).Add("SwitchMachine", "SwitchMachine");
            (_temp).Add("UserMachine", "UserMachine");
            (_temp).Add("OSRDriverMachine", "OSRDriverMachine");
            (linkMap).Add("TimerMachine", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("LEDMachine", "LEDMachine");
            (_temp).Add("SwitchMachine", "SwitchMachine");
            (_temp).Add("TimerMachine", "TimerMachine");
            (_temp).Add("UserMachine", "UserMachine");
            (_temp).Add("OSRDriverMachine", "OSRDriverMachine");
            (linkMap).Add("LEDMachine", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("OSRDriverMachine", "OSRDriverMachine");
            (_temp).Add("LEDMachine", "LEDMachine");
            (_temp).Add("TimerMachine", "TimerMachine");
            (_temp).Add("SwitchMachine", "SwitchMachine");
            (_temp).Add("SwitchInterface", "SwitchMachine");
            (_temp).Add("LEDInterface", "LEDMachine");
            (_temp).Add("TimerInterface", "TimerMachine");
            (_temp).Add("UserMachine", "UserMachine");
            (linkMap).Add("OSRDriverMachine", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("SwitchMachine", "SwitchMachine");
            (_temp).Add("LEDMachine", "LEDMachine");
            (_temp).Add("TimerMachine", "TimerMachine");
            (_temp).Add("UserMachine", "UserMachine");
            (_temp).Add("OSRDriverMachine", "OSRDriverMachine");
            (linkMap).Add("SwitchMachine", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("UserMachine", "UserMachine");
            (_temp).Add("LEDMachine", "LEDMachine");
            (_temp).Add("TimerMachine", "TimerMachine");
            (_temp).Add("SwitchMachine", "SwitchMachine");
            (_temp).Add("OSRDriverMachine", "OSRDriverMachine");
            (linkMap).Add("UserMachine", _temp);
        }
    }
}
