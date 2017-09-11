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
            CreateSpecMachine("ReqIdsAreMonotonicallyIncreasing");
            CreateSpecMachine("RespIdsAreMonotonicallyIncreasing");
            CreateMainMachine("TestDriver0");
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        static Application()
        {
            Types.Types_refinement();
            Events.Events_refinement();
            (isSafeMap).Add("ClientInterface", false);
            (isSafeMap).Add("ServerClientInterface", false);
            (isSafeMap).Add("TestDriver0", false);
            (isSafeMap).Add("RespIdsAreMonotonicallyIncreasing", false);
            (isSafeMap).Add("ReqIdsAreMonotonicallyIncreasing", false);
            (machineDefMap).Add("ServerClientInterface", "AbstractServerMachine");
            (machineDefMap).Add("ClientInterface", "ClientMachine");
            (machineDefMap).Add("TestDriver0", "TestDriver0");
            (machineDefMap).Add("RespIdsAreMonotonicallyIncreasing", "RespIdsAreMonotonicallyIncreasing");
            (machineDefMap).Add("ReqIdsAreMonotonicallyIncreasing", "ReqIdsAreMonotonicallyIncreasing");
            (createSpecMap).Add("RespIdsAreMonotonicallyIncreasing", CreateSpecMachine_RespIdsAreMonotonicallyIncreasing);
            (createSpecMap).Add("ReqIdsAreMonotonicallyIncreasing", CreateSpecMachine_ReqIdsAreMonotonicallyIncreasing);
            (createMachineMap).Add("AbstractServerMachine", CreateMachine_AbstractServerMachine);
            (createMachineMap).Add("ClientMachine", CreateMachine_ClientMachine);
            (createMachineMap).Add("TestDriver0", CreateMachine_TestDriver0);
            interfaceMap["ServerHelperInterface"] = new List<PrtEventValue>()
            {Events.event_eReqSuccessful, Events.event_eReqFailed};
            interfaceMap["ServerClientInterface"] = new List<PrtEventValue>()
            {Events.event_eRequest};
            interfaceMap["ClientInterface"] = new List<PrtEventValue>()
            {Events.event_eResponse};
            interfaceMap["HelperInterface"] = new List<PrtEventValue>()
            {Events.event_eProcessReq};
            visibleEvents = new List<string>()
            {};
            visibleInterfaces = new List<string>()
            {};
            ((PrtInterfaceType)(Types.type_ServerHelperInterface)).permissions = interfaceMap["ServerHelperInterface"];
            ((PrtInterfaceType)(Types.type_ServerClientInterface)).permissions = interfaceMap["ServerClientInterface"];
            ((PrtInterfaceType)(Types.type_ClientInterface)).permissions = interfaceMap["ClientInterface"];
            ((PrtInterfaceType)(Types.type_HelperInterface)).permissions = interfaceMap["HelperInterface"];
            (specMachineMap).Add("ReqIdsAreMonotonicallyIncreasing", new List<string>()
            {"ClientInterface", "ServerClientInterface"});
            (specMachineMap).Add("RespIdsAreMonotonicallyIncreasing", new List<string>()
            {"ServerClientInterface", "ClientInterface"});
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("ClientInterface", "ClientInterface");
            (_temp).Add("ServerClientInterface", "ServerClientInterface");
            (linkMap).Add("TestDriver0", _temp);
        }
    }
}
