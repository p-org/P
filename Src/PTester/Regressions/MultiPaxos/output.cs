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
            CreateSpecMachine("BasicPaxosInvariant_P2b");
            CreateSpecMachine("ValmachineityCheck");
            CreateMainMachine();
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        static Application()
        {
            Types.Types_Multi_Paxos_4();
            Events.Events_Multi_Paxos_4();
            (isSafeMap).Add("PaxosNode", false);
            (isSafeMap).Add("Timer", false);
            (isSafeMap).Add("LeaderElection", false);
            (isSafeMap).Add("Main", false);
            (isSafeMap).Add("ValmachineityCheck", false);
            (isSafeMap).Add("BasicPaxosInvariant_P2b", false);
            (isSafeMap).Add("Client", false);
            (renameMap).Add("PaxosNode", "PaxosNode");
            (renameMap).Add("Timer", "Timer");
            (renameMap).Add("LeaderElection", "LeaderElection");
            (renameMap).Add("Main", "Main");
            (renameMap).Add("ValmachineityCheck", "ValmachineityCheck");
            (renameMap).Add("BasicPaxosInvariant_P2b", "BasicPaxosInvariant_P2b");
            (renameMap).Add("Client", "Client");
            (createMachineMap).Add("PaxosNode", CreateMachine_PaxosNode);
            (createMachineMap).Add("Timer", CreateMachine_Timer);
            (createMachineMap).Add("LeaderElection", CreateMachine_LeaderElection);
            (createMachineMap).Add("Main", CreateMachine_Main);
            (createSpecMap).Add("ValmachineityCheck", CreateSpecMachine_ValmachineityCheck);
            (createSpecMap).Add("BasicPaxosInvariant_P2b", CreateSpecMachine_BasicPaxosInvariant_P2b);
            (createMachineMap).Add("Client", CreateMachine_Client);
            (specMachineMap).Add("BasicPaxosInvariant_P2b", new List<string>()
            {"Client", "PaxosNode", "Timer", "LeaderElection", "Main", "ValmachineityCheck", "BasicPaxosInvariant_P2b"});
            (specMachineMap).Add("ValmachineityCheck", new List<string>()
            {"Client", "PaxosNode", "Timer", "LeaderElection", "Main", "ValmachineityCheck", "BasicPaxosInvariant_P2b"});
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("Client", "Client");
            (_temp).Add("PaxosNode", "PaxosNode");
            (_temp).Add("Timer", "Timer");
            (_temp).Add("LeaderElection", "LeaderElection");
            (_temp).Add("ValmachineityCheck", "ValmachineityCheck");
            (_temp).Add("BasicPaxosInvariant_P2b", "BasicPaxosInvariant_P2b");
            (_temp).Add("Main", "Main");
            (linkMap).Add("Main", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("LeaderElection", "LeaderElection");
            (_temp).Add("BasicPaxosInvariant_P2b", "BasicPaxosInvariant_P2b");
            (_temp).Add("ValmachineityCheck", "ValmachineityCheck");
            (_temp).Add("Main", "Main");
            (_temp).Add("Timer", "Timer");
            (_temp).Add("PaxosNode", "PaxosNode");
            (_temp).Add("Client", "Client");
            (linkMap).Add("Client", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("LeaderElection", "LeaderElection");
            (_temp).Add("BasicPaxosInvariant_P2b", "BasicPaxosInvariant_P2b");
            (_temp).Add("ValmachineityCheck", "ValmachineityCheck");
            (_temp).Add("Main", "Main");
            (_temp).Add("Timer", "Timer");
            (_temp).Add("PaxosNode", "PaxosNode");
            (_temp).Add("Client", "Client");
            (linkMap).Add("PaxosNode", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("LeaderElection", "LeaderElection");
            (_temp).Add("BasicPaxosInvariant_P2b", "BasicPaxosInvariant_P2b");
            (_temp).Add("ValmachineityCheck", "ValmachineityCheck");
            (_temp).Add("Main", "Main");
            (_temp).Add("Timer", "Timer");
            (_temp).Add("Client", "Client");
            (_temp).Add("PaxosNode", "PaxosNode");
            (linkMap).Add("Timer", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("LeaderElection", "LeaderElection");
            (_temp).Add("ValmachineityCheck", "ValmachineityCheck");
            (_temp).Add("BasicPaxosInvariant_P2b", "BasicPaxosInvariant_P2b");
            (_temp).Add("Main", "Main");
            (_temp).Add("Client", "Client");
            (_temp).Add("PaxosNode", "PaxosNode");
            (_temp).Add("Timer", "Timer");
            (linkMap).Add("LeaderElection", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("ValmachineityCheck", "ValmachineityCheck");
            (_temp).Add("Client", "Client");
            (_temp).Add("PaxosNode", "PaxosNode");
            (_temp).Add("Timer", "Timer");
            (_temp).Add("BasicPaxosInvariant_P2b", "BasicPaxosInvariant_P2b");
            (_temp).Add("LeaderElection", "LeaderElection");
            (_temp).Add("Main", "Main");
            (linkMap).Add("ValmachineityCheck", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("BasicPaxosInvariant_P2b", "BasicPaxosInvariant_P2b");
            (_temp).Add("Client", "Client");
            (_temp).Add("PaxosNode", "PaxosNode");
            (_temp).Add("Timer", "Timer");
            (_temp).Add("LeaderElection", "LeaderElection");
            (_temp).Add("Main", "Main");
            (_temp).Add("ValmachineityCheck", "ValmachineityCheck");
            (linkMap).Add("BasicPaxosInvariant_P2b", _temp);
        }
    }
}
