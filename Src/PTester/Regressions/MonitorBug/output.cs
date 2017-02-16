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
            CreateSpecMachine("Liveness");
            CreateSpecMachine("Safety");
            CreateMainMachine();
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        static Application()
        {
            Types.Types_PingPongBugRepro();
            Types.Types_Timer();
            Events.Events_PingPongBugRepro();
            Events.Events_Timer();
            (isSafeMap).Add("Node", false);
            (isSafeMap).Add("Main", false);
            (isSafeMap).Add("Timer", false);
            (isSafeMap).Add("FailureDetector", false);
            (isSafeMap).Add("Liveness", false);
            (isSafeMap).Add("Safety", false);
            (renameMap).Add("FailureDetector", "FailureDetector");
            (renameMap).Add("Node", "Node");
            (renameMap).Add("Main", "Main");
            (renameMap).Add("Timer", "Timer");
            (renameMap).Add("Liveness", "Liveness");
            (renameMap).Add("Safety", "Safety");
            (createMachineMap).Add("FailureDetector", CreateMachine_FailureDetector);
            (createMachineMap).Add("Node", CreateMachine_Node);
            (createMachineMap).Add("Main", CreateMachine_Main);
            (createMachineMap).Add("Timer", CreateMachine_Timer);
            (createSpecMap).Add("Liveness", CreateSpecMachine_Liveness);
            (createSpecMap).Add("Safety", CreateSpecMachine_Safety);
            (specMachineMap).Add("Liveness", new List<string>()
            {"Main", "Timer", "FailureDetector", "Liveness", "Safety", "Node"});
            (specMachineMap).Add("Safety", new List<string>()
            {"Timer", "FailureDetector", "Liveness", "Safety", "Node", "Main"});
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("Main", "Main");
            (_temp).Add("Timer", "Timer");
            (_temp).Add("FailureDetector", "FailureDetector");
            (_temp).Add("Liveness", "Liveness");
            (_temp).Add("Safety", "Safety");
            (_temp).Add("Node", "Node");
            (linkMap).Add("Timer", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("FailureDetector", "FailureDetector");
            (_temp).Add("Timer", "Timer");
            (_temp).Add("Node", "Node");
            (_temp).Add("Safety", "Safety");
            (_temp).Add("Liveness", "Liveness");
            (_temp).Add("Main", "Main");
            (linkMap).Add("Liveness", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("FailureDetector", "FailureDetector");
            (_temp).Add("Timer", "Timer");
            (_temp).Add("Safety", "Safety");
            (_temp).Add("Liveness", "Liveness");
            (_temp).Add("Node", "Node");
            (_temp).Add("Main", "Main");
            (linkMap).Add("FailureDetector", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Timer", "Timer");
            (_temp).Add("Node", "Node");
            (_temp).Add("Safety", "Safety");
            (_temp).Add("FailureDetector", "FailureDetector");
            (_temp).Add("Liveness", "Liveness");
            (_temp).Add("Main", "Main");
            (linkMap).Add("Safety", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Timer", "Timer");
            (_temp).Add("Node", "Node");
            (_temp).Add("FailureDetector", "FailureDetector");
            (_temp).Add("Liveness", "Liveness");
            (_temp).Add("Safety", "Safety");
            (_temp).Add("Main", "Main");
            (linkMap).Add("Node", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("Timer", "Timer");
            (_temp).Add("Main", "Main");
            (_temp).Add("FailureDetector", "FailureDetector");
            (_temp).Add("Liveness", "Liveness");
            (_temp).Add("Safety", "Safety");
            (_temp).Add("Node", "Node");
            (linkMap).Add("Main", _temp);
        }
    }
}
