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
            (isSafeMap).Add("ServerClientInterface", false);
            (isSafeMap).Add("TestDriver0", false);
            (isSafeMap).Add("ClientInterface", false);
            (machineDefMap).Add("ServerClientInterface", "AbstractServerMachine");
            (machineDefMap).Add("TestDriver0", "TestDriver0");
            (machineDefMap).Add("ClientInterface", "ClientMachine");
            (createMachineMap).Add("AbstractServerMachine", CreateMachine_AbstractServerMachine);
            (createMachineMap).Add("TestDriver0", CreateMachine_TestDriver0);
            (createMachineMap).Add("ClientMachine", CreateMachine_ClientMachine);
            interfaceMap["ServerHelperInterface"] = new List<PrtEventValue>()
            {Events.event_eReqSuccessful, Events.event_eReqFailed};
            interfaceMap["ClientInterface"] = new List<PrtEventValue>()
            {Events.event_eResponse};
            interfaceMap["ServerClientInterface"] = new List<PrtEventValue>()
            {Events.event_eRequest};
            interfaceMap["HelperInterface"] = new List<PrtEventValue>()
            {Events.event_eProcessReq};
            visibleEvents = new List<string>()
            {"eResponse", "eRequest"};
            visibleInterfaces = new List<string>()
            {"ServerClientInterface", "ClientInterface"};
            ((PrtInterfaceType)(Types.type_ServerHelperInterface)).permissions = interfaceMap["ServerHelperInterface"];
            ((PrtInterfaceType)(Types.type_ClientInterface)).permissions = interfaceMap["ClientInterface"];
            ((PrtInterfaceType)(Types.type_ServerClientInterface)).permissions = interfaceMap["ServerClientInterface"];
            ((PrtInterfaceType)(Types.type_HelperInterface)).permissions = interfaceMap["HelperInterface"];
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("ClientInterface", "ClientInterface");
            (_temp).Add("ServerClientInterface", "ServerClientInterface");
            (linkMap).Add("TestDriver0", _temp);
        }
    }
}
