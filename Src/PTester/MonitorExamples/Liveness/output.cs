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
        public partial class Events
        {
            public static PrtEventValue halt = PrtValue.halt;
            public static PrtEventValue @null = PrtValue.@null;
        }

        public Application(): base ()
        {
        }

        public Application(bool initialize): base ()
        {
            CreateSpecMachine("M");
            CreateMainMachine();
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        static Application()
        {
            Types.Types_PingPongMonitor();
            Events.Events_PingPongMonitor();
            (isSafeMap).Add("Main", false);
            (isSafeMap).Add("M", false);
            (isSafeMap).Add("PONG", false);
            (renameMap).Add("Main", "Main");
            (renameMap).Add("M", "M");
            (renameMap).Add("PONG", "PONG");
            (createMachineMap).Add("Main", CreateMachine_Main);
            (createSpecMap).Add("M", CreateSpecMachine_M);
            (createMachineMap).Add("PONG", CreateMachine_PONG);
            (specMachineMap).Add("M", new List<string>()
            {"Main", "PONG", "M"});
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("M", "M");
            (_temp).Add("PONG", "PONG");
            (_temp).Add("Main", "Main");
            (linkMap).Add("PONG", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("PONG", "PONG");
            (_temp).Add("Main", "Main");
            (_temp).Add("M", "M");
            (linkMap).Add("M", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("PONG", "PONG");
            (_temp).Add("M", "M");
            (_temp).Add("Main", "Main");
            (linkMap).Add("Main", _temp);
        }
    }
}
