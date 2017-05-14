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
            CreateMainMachine();
        }

        public override StateImpl MakeSkeleton()
        {
            return new Application();
        }

        static Application()
        {
            Types.Types_testdriver();
            Events.Events_testdriver();
            (isSafeMap).Add("Main", false);
            (isSafeMap).Add("ServerMachine", false);
            (renameMap).Add("ServerMachine", "ServerAbstractionMachine");
            (renameMap).Add("Main", "TestDriver_Refinement");
            (createMachineMap).Add("ServerAbstractionMachine", CreateMachine_ServerAbstractionMachine);
            (createMachineMap).Add("TestDriver_Refinement", CreateMachine_TestDriver_Refinement);
            interfaceMap["ServerHelperInterface"] = new List<PrtEventValue>()
            {Events.event_eReqSuccessful, Events.event_eReqFailed};
            interfaceMap["ServerClientInterface"] = new List<PrtEventValue>()
            {Events.event_eRequest};
            interfaceMap["ClientInterface"] = new List<PrtEventValue>()
            {Events.event_eResponse};
            interfaceMap["HelperInterface"] = new List<PrtEventValue>()
            {Events.event_eProcessReq};
            visibleEvents = new List<string>()
            {"eRequest", "eResponse"};
            visibleInterfaces = new List<string>()
            {"ServerClientInterface"};
            ((PrtInterfaceType)(Types.type_ServerHelperInterface)).permissions = interfaceMap["ServerHelperInterface"];
            ((PrtInterfaceType)(Types.type_ServerClientInterface)).permissions = interfaceMap["ServerClientInterface"];
            ((PrtInterfaceType)(Types.type_ClientInterface)).permissions = interfaceMap["ClientInterface"];
            ((PrtInterfaceType)(Types.type_HelperInterface)).permissions = interfaceMap["HelperInterface"];
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("ClientInterface", "Main");
            (_temp).Add("ServerClientInterface", "ServerMachine");
            (linkMap).Add("Main", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("ServerClientInterface", "ServerMachine");
            (_temp).Add("ClientInterface", "Main");
            (linkMap).Add("ServerMachine", _temp);
        }
    }
}
