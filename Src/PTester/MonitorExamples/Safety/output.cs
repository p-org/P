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
            Types.Types_PingPong();
            Types.Types_PrtDistHelp();
            Events.Events_PingPong();
            Events.Events_PrtDistHelp();
            (isSafeMap).Add("PONG", false);
            (isSafeMap).Add("Container", false);
            (isSafeMap).Add("PING", false);
            (isSafeMap).Add("M", false);
            (isSafeMap).Add("Main", false);
            (renameMap).Add("PONG", "PONG");
            (renameMap).Add("Container", "Container");
            (renameMap).Add("PING", "PING");
            (renameMap).Add("Main", "Main");
            (renameMap).Add("M", "M");
            (createMachineMap).Add("PONG", CreateMachine_PONG);
            (createMachineMap).Add("Container", CreateMachine_Container);
            (createMachineMap).Add("PING", CreateMachine_PING);
            (createMachineMap).Add("Main", CreateMachine_Main);
            (createSpecMap).Add("M", CreateSpecMachine_M);
            (specMachineMap).Add("M", new List<string>()
            {"M", "PONG", "Container", "PING", "Main"});
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("M", "M");
            (_temp).Add("Container", "Container");
            (_temp).Add("PONG", "PONG");
            (_temp).Add("PING", "PING");
            (_temp).Add("Main", "Main");
            (linkMap).Add("M", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("PONG", "PONG");
            (_temp).Add("PING", "PING");
            (_temp).Add("Main", "Main");
            (_temp).Add("M", "M");
            (_temp).Add("Container", "Container");
            (linkMap).Add("PONG", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("M", "M");
            (_temp).Add("PONG", "PONG");
            (_temp).Add("PING", "PING");
            (_temp).Add("Main", "Main");
            (_temp).Add("Container", "Container");
            (linkMap).Add("PING", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("PING", "PING");
            (_temp).Add("Main", "Main");
            (_temp).Add("M", "M");
            (_temp).Add("PONG", "PONG");
            (_temp).Add("Container", "Container");
            (linkMap).Add("Container", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("PING", "PING");
            (_temp).Add("Container", "Container");
            (_temp).Add("Main", "Main");
            (_temp).Add("PONG", "PONG");
            (_temp).Add("M", "M");
            (linkMap).Add("Main", _temp);
        }
    }
}
