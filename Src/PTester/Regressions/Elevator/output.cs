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
            Types.Types_Elevator();
            Events.Events_Elevator();
            (isSafeMap).Add("Elevator", false);
            (isSafeMap).Add("Main", false);
            (isSafeMap).Add("Door", false);
            (isSafeMap).Add("Timer", false);
            (renameMap).Add("Elevator", "Elevator");
            (renameMap).Add("Main", "Main");
            (renameMap).Add("Door", "Door");
            (renameMap).Add("Timer", "Timer");
            (createMachineMap).Add("Elevator", CreateMachine_Elevator);
            (createMachineMap).Add("Main", CreateMachine_Main);
            (createMachineMap).Add("Door", CreateMachine_Door);
            (createMachineMap).Add("Timer", CreateMachine_Timer);
            interfaceMap["DoorInterface"] = new List<PrtEventValue>()
            {Events.eSendCommandToCloseDoor, Events.eSendCommandToOpenDoor, Events.eSendCommandToStopDoor, Events.eSendCommandToResetDoor};
            interfaceMap["TimerInterface"] = new List<PrtEventValue>()
            {Events.eStartDoorCloseTimer, Events.eStopDoorCloseTimer};
            interfaceMap["ElevatorInterface"] = new List<PrtEventValue>()
            {Events.eTimerFired, Events.eStopTimerReturned, Events.eOperationSuccess, Events.eOperationFailure, Events.eOpenDoor, Events.eObjectDetected, Events.eDoorStopped, Events.eDoorOpened, Events.eDoorClosed, Events.eCloseDoor};
            ((PrtInterfaceType)(Types.type_DoorInterface)).permissions = interfaceMap["DoorInterface"];
            ((PrtInterfaceType)(Types.type_TimerInterface)).permissions = interfaceMap["TimerInterface"];
            ((PrtInterfaceType)(Types.type_ElevatorInterface)).permissions = interfaceMap["ElevatorInterface"];
            Dictionary<string, string> _temp;
            _temp = new Dictionary<string, string>();
            (_temp).Add("TimerInterface", "Timer");
            (_temp).Add("DoorInterface", "Door");
            (_temp).Add("ElevatorInterface", "Elevator");
            (linkMap).Add("Door", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("DoorInterface", "Door");
            (_temp).Add("TimerInterface", "Timer");
            (_temp).Add("ElevatorInterface", "Elevator");
            (linkMap).Add("Timer", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("DoorInterface", "Door");
            (_temp).Add("TimerInterface", "Timer");
            (_temp).Add("ElevatorInterface", "Elevator");
            (linkMap).Add("Elevator", _temp);
            _temp = new Dictionary<string, string>();
            (_temp).Add("TimerInterface", "Timer");
            (_temp).Add("ElevatorInterface", "Elevator");
            (_temp).Add("DoorInterface", "Door");
            (linkMap).Add("Main", _temp);
        }
    }
}
