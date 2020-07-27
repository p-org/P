using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Plang.CoyoteRuntime;
using Plang.CoyoteRuntime.Values;
using Plang.CoyoteRuntime.Exceptions;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 162, 219, 414, 1998
namespace elevator
{
    public static partial class GlobalFunctions { }
    internal partial class eOpenDoor : PEvent
    {
        public eOpenDoor() : base() { }
        public eOpenDoor(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eOpenDoor(); }
    }
    internal partial class eCloseDoor : PEvent
    {
        public eCloseDoor() : base() { }
        public eCloseDoor(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eCloseDoor(); }
    }
    internal partial class eResetDoor : PEvent
    {
        public eResetDoor() : base() { }
        public eResetDoor(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eResetDoor(); }
    }
    internal partial class eDoorOpened : PEvent
    {
        public eDoorOpened() : base() { }
        public eDoorOpened(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eDoorOpened(); }
    }
    internal partial class eDoorClosed : PEvent
    {
        public eDoorClosed() : base() { }
        public eDoorClosed(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eDoorClosed(); }
    }
    internal partial class eDoorStopped : PEvent
    {
        public eDoorStopped() : base() { }
        public eDoorStopped(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eDoorStopped(); }
    }
    internal partial class eObjectDetected : PEvent
    {
        public eObjectDetected() : base() { }
        public eObjectDetected(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eObjectDetected(); }
    }
    internal partial class eTimerFired : PEvent
    {
        public eTimerFired() : base() { }
        public eTimerFired(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eTimerFired(); }
    }
    internal partial class eOperationSuccess : PEvent
    {
        public eOperationSuccess() : base() { }
        public eOperationSuccess(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eOperationSuccess(); }
    }
    internal partial class eOperationFailure : PEvent
    {
        public eOperationFailure() : base() { }
        public eOperationFailure(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eOperationFailure(); }
    }
    internal partial class eSendCommandToOpenDoor : PEvent
    {
        public eSendCommandToOpenDoor() : base() { }
        public eSendCommandToOpenDoor(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eSendCommandToOpenDoor(); }
    }
    internal partial class eSendCommandToCloseDoor : PEvent
    {
        public eSendCommandToCloseDoor() : base() { }
        public eSendCommandToCloseDoor(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eSendCommandToCloseDoor(); }
    }
    internal partial class eSendCommandToStopDoor : PEvent
    {
        public eSendCommandToStopDoor() : base() { }
        public eSendCommandToStopDoor(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eSendCommandToStopDoor(); }
    }
    internal partial class eSendCommandToResetDoor : PEvent
    {
        public eSendCommandToResetDoor() : base() { }
        public eSendCommandToResetDoor(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eSendCommandToResetDoor(); }
    }
    internal partial class eUnit : PEvent
    {
        public eUnit() : base() { }
        public eUnit(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eUnit(); }
    }
    internal partial class eStartTimer : PEvent
    {
        public eStartTimer() : base() { }
        public eStartTimer(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eStartTimer(); }
    }
    internal partial class eObjectEncountered : PEvent
    {
        public eObjectEncountered() : base() { }
        public eObjectEncountered(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eObjectEncountered(); }
    }
    internal partial class Elevator : PMachine
    {
        private PMachineValue TimerV = null;
        private PMachineValue DoorV = null;
        public class ConstructorEvent : PEvent { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Elevator()
        {
            this.sends.Add(nameof(eCloseDoor));
            this.sends.Add(nameof(eDoorClosed));
            this.sends.Add(nameof(eDoorOpened));
            this.sends.Add(nameof(eDoorStopped));
            this.sends.Add(nameof(eObjectDetected));
            this.sends.Add(nameof(eObjectEncountered));
            this.sends.Add(nameof(eOpenDoor));
            this.sends.Add(nameof(eOperationFailure));
            this.sends.Add(nameof(eOperationSuccess));
            this.sends.Add(nameof(eResetDoor));
            this.sends.Add(nameof(eSendCommandToCloseDoor));
            this.sends.Add(nameof(eSendCommandToOpenDoor));
            this.sends.Add(nameof(eSendCommandToResetDoor));
            this.sends.Add(nameof(eSendCommandToStopDoor));
            this.sends.Add(nameof(eStartTimer));
            this.sends.Add(nameof(eTimerFired));
            this.sends.Add(nameof(eUnit));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(eCloseDoor));
            this.receives.Add(nameof(eDoorClosed));
            this.receives.Add(nameof(eDoorOpened));
            this.receives.Add(nameof(eDoorStopped));
            this.receives.Add(nameof(eObjectDetected));
            this.receives.Add(nameof(eObjectEncountered));
            this.receives.Add(nameof(eOpenDoor));
            this.receives.Add(nameof(eOperationFailure));
            this.receives.Add(nameof(eOperationSuccess));
            this.receives.Add(nameof(eResetDoor));
            this.receives.Add(nameof(eSendCommandToCloseDoor));
            this.receives.Add(nameof(eSendCommandToOpenDoor));
            this.receives.Add(nameof(eSendCommandToResetDoor));
            this.receives.Add(nameof(eSendCommandToStopDoor));
            this.receives.Add(nameof(eStartTimer));
            this.receives.Add(nameof(eTimerFired));
            this.receives.Add(nameof(eUnit));
            this.receives.Add(nameof(PHalt));
            this.creates.Add(nameof(I_Door));
            this.creates.Add(nameof(I_Timer));
        }

        public void Anon(Event currentMachine_dequeuedEvent)
        {
            Elevator currentMachine = this;
            PMachineValue TMP_tmp0 = null;
            PMachineValue TMP_tmp1 = null;
            PMachineValue TMP_tmp2 = null;
            PMachineValue TMP_tmp3 = null;
            PEvent TMP_tmp4 = null;
            TMP_tmp0 = (PMachineValue)(currentMachine.self);
            TMP_tmp1 = (PMachineValue)(currentMachine.CreateInterface<I_Timer>(currentMachine, TMP_tmp0));
            TimerV = (PMachineValue)TMP_tmp1;
            TMP_tmp2 = (PMachineValue)(currentMachine.self);
            TMP_tmp3 = (PMachineValue)(currentMachine.CreateInterface<I_Door>(currentMachine, TMP_tmp2));
            DoorV = (PMachineValue)TMP_tmp3;
            TMP_tmp4 = (PEvent)(new eUnit(null));
            currentMachine.TryRaiseEvent((Event)TMP_tmp4);
            return;
        }
        public void Anon_1(Event currentMachine_dequeuedEvent)
        {
            Elevator currentMachine = this;
            PMachineValue TMP_tmp0_1 = null;
            PEvent TMP_tmp1_1 = null;
            TMP_tmp0_1 = (PMachineValue)(((PMachineValue)((IPrtValue)DoorV)?.Clone()));
            TMP_tmp1_1 = (PEvent)(new eSendCommandToResetDoor(null));
            currentMachine.TrySendEvent(TMP_tmp0_1, (Event)TMP_tmp1_1);
        }
        public void Anon_2(Event currentMachine_dequeuedEvent)
        {
            Elevator currentMachine = this;
            PMachineValue TMP_tmp0_2 = null;
            PEvent TMP_tmp1_2 = null;
            TMP_tmp0_2 = (PMachineValue)(((PMachineValue)((IPrtValue)DoorV)?.Clone()));
            TMP_tmp1_2 = (PEvent)(new eSendCommandToOpenDoor(null));
            currentMachine.TrySendEvent(TMP_tmp0_2, (Event)TMP_tmp1_2);
        }
        public void Anon_3(Event currentMachine_dequeuedEvent)
        {
            Elevator currentMachine = this;
            PMachineValue TMP_tmp0_3 = null;
            PEvent TMP_tmp1_3 = null;
            PMachineValue TMP_tmp2_1 = null;
            PEvent TMP_tmp3_1 = null;
            TMP_tmp0_3 = (PMachineValue)(((PMachineValue)((IPrtValue)DoorV)?.Clone()));
            TMP_tmp1_3 = (PEvent)(new eSendCommandToResetDoor(null));
            currentMachine.TrySendEvent(TMP_tmp0_3, (Event)TMP_tmp1_3);
            TMP_tmp2_1 = (PMachineValue)(((PMachineValue)((IPrtValue)TimerV)?.Clone()));
            TMP_tmp3_1 = (PEvent)(new eStartTimer(null));
            currentMachine.TrySendEvent(TMP_tmp2_1, (Event)TMP_tmp3_1);
        }
        public void Anon_4(Event currentMachine_dequeuedEvent)
        {
            Elevator currentMachine = this;
            PrtBool TMP_tmp0_4 = ((PrtBool)false);
            PEvent TMP_tmp1_4 = null;
            TMP_tmp0_4 = (PrtBool)(((PrtBool)currentMachine.TryRandomBool()));
            if (TMP_tmp0_4)
            {
                TMP_tmp1_4 = (PEvent)(new eCloseDoor(null));
                currentMachine.TryRaiseEvent((Event)TMP_tmp1_4);
                return;
            }
        }
        public void Anon_5(Event currentMachine_dequeuedEvent)
        {
            Elevator currentMachine = this;
            PMachineValue TMP_tmp0_5 = null;
            PEvent TMP_tmp1_5 = null;
            TMP_tmp0_5 = (PMachineValue)(((PMachineValue)((IPrtValue)DoorV)?.Clone()));
            TMP_tmp1_5 = (PEvent)(new eSendCommandToCloseDoor(null));
            currentMachine.TrySendEvent(TMP_tmp0_5, (Event)TMP_tmp1_5);
        }
        public void Anon_6(Event currentMachine_dequeuedEvent)
        {
            Elevator currentMachine = this;
            PMachineValue TMP_tmp0_6 = null;
            PEvent TMP_tmp1_6 = null;
            TMP_tmp0_6 = (PMachineValue)(((PMachineValue)((IPrtValue)DoorV)?.Clone()));
            TMP_tmp1_6 = (PEvent)(new eSendCommandToStopDoor(null));
            currentMachine.TrySendEvent(TMP_tmp0_6, (Event)TMP_tmp1_6);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init))]
        class __InitState__ : State { }

        [OnEntry(nameof(Anon))]
        [OnEventGotoState(typeof(eUnit), typeof(DoorClosed))]
        class Init : State
        {
        }
        [OnEntry(nameof(Anon_1))]
        [OnEventGotoState(typeof(eOpenDoor), typeof(DoorOpening))]
        [IgnoreEvents(typeof(eCloseDoor))]
        class DoorClosed : State
        {
        }
        [OnEntry(nameof(Anon_2))]
        [OnEventGotoState(typeof(eDoorOpened), typeof(DoorOpened))]
        [DeferEvents(typeof(eCloseDoor))]
        [IgnoreEvents(typeof(eOpenDoor))]
        class DoorOpening : State
        {
        }
        [OnEntry(nameof(Anon_3))]
        [OnEventGotoState(typeof(eTimerFired), typeof(DoorOpenedOkToClose))]
        [DeferEvents(typeof(eCloseDoor))]
        [IgnoreEvents(typeof(eOpenDoor))]
        class DoorOpened : State
        {
        }
        [OnEntry(nameof(Anon_4))]
        [OnEventGotoState(typeof(eCloseDoor), typeof(DoorClosing))]
        [DeferEvents(typeof(eOpenDoor))]
        class DoorOpenedOkToClose : State
        {
        }
        [OnEntry(nameof(Anon_5))]
        [OnEventGotoState(typeof(eOpenDoor), typeof(StoppingDoor))]
        [OnEventGotoState(typeof(eDoorClosed), typeof(DoorClosed))]
        [OnEventGotoState(typeof(eObjectDetected), typeof(DoorOpening))]
        [DeferEvents(typeof(eCloseDoor))]
        class DoorClosing : State
        {
        }
        [OnEntry(nameof(Anon_6))]
        [OnEventGotoState(typeof(eDoorOpened), typeof(DoorOpened))]
        [OnEventGotoState(typeof(eDoorClosed), typeof(DoorClosed))]
        [OnEventGotoState(typeof(eDoorStopped), typeof(DoorOpening))]
        [DeferEvents(typeof(eCloseDoor))]
        [IgnoreEvents(typeof(eOpenDoor), typeof(eObjectDetected))]
        class StoppingDoor : State
        {
        }
    }
    internal partial class Main : PMachine
    {
        private PMachineValue ElevatorV = null;
        private PrtInt count = ((PrtInt)0);
        public class ConstructorEvent : PEvent { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Main()
        {
            this.sends.Add(nameof(eCloseDoor));
            this.sends.Add(nameof(eDoorClosed));
            this.sends.Add(nameof(eDoorOpened));
            this.sends.Add(nameof(eDoorStopped));
            this.sends.Add(nameof(eObjectDetected));
            this.sends.Add(nameof(eObjectEncountered));
            this.sends.Add(nameof(eOpenDoor));
            this.sends.Add(nameof(eOperationFailure));
            this.sends.Add(nameof(eOperationSuccess));
            this.sends.Add(nameof(eResetDoor));
            this.sends.Add(nameof(eSendCommandToCloseDoor));
            this.sends.Add(nameof(eSendCommandToOpenDoor));
            this.sends.Add(nameof(eSendCommandToResetDoor));
            this.sends.Add(nameof(eSendCommandToStopDoor));
            this.sends.Add(nameof(eStartTimer));
            this.sends.Add(nameof(eTimerFired));
            this.sends.Add(nameof(eUnit));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(eCloseDoor));
            this.receives.Add(nameof(eDoorClosed));
            this.receives.Add(nameof(eDoorOpened));
            this.receives.Add(nameof(eDoorStopped));
            this.receives.Add(nameof(eObjectDetected));
            this.receives.Add(nameof(eObjectEncountered));
            this.receives.Add(nameof(eOpenDoor));
            this.receives.Add(nameof(eOperationFailure));
            this.receives.Add(nameof(eOperationSuccess));
            this.receives.Add(nameof(eResetDoor));
            this.receives.Add(nameof(eSendCommandToCloseDoor));
            this.receives.Add(nameof(eSendCommandToOpenDoor));
            this.receives.Add(nameof(eSendCommandToResetDoor));
            this.receives.Add(nameof(eSendCommandToStopDoor));
            this.receives.Add(nameof(eStartTimer));
            this.receives.Add(nameof(eTimerFired));
            this.receives.Add(nameof(eUnit));
            this.receives.Add(nameof(PHalt));
            this.creates.Add(nameof(I_Elevator));
        }

        public void Anon_7(Event currentMachine_dequeuedEvent)
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0_7 = null;
            TMP_tmp0_7 = (PMachineValue)(currentMachine.CreateInterface<I_Elevator>(currentMachine));
            ElevatorV = (PMachineValue)TMP_tmp0_7;
            currentMachine.TryGotoState<Main.Loop>();
            return;
        }
        public void Anon_8(Event currentMachine_dequeuedEvent)
        {
            Main currentMachine = this;
            PrtBool TMP_tmp0_8 = ((PrtBool)false);
            PMachineValue TMP_tmp1_7 = null;
            PEvent TMP_tmp2_2 = null;
            PrtBool TMP_tmp3_2 = ((PrtBool)false);
            PMachineValue TMP_tmp4_1 = null;
            PEvent TMP_tmp5 = null;
            PrtBool TMP_tmp6 = ((PrtBool)false);
            PEvent TMP_tmp7 = null;
            PrtInt TMP_tmp8 = ((PrtInt)0);
            TMP_tmp0_8 = (PrtBool)(((PrtBool)currentMachine.TryRandomBool()));
            if (TMP_tmp0_8)
            {
                TMP_tmp1_7 = (PMachineValue)(((PMachineValue)((IPrtValue)ElevatorV)?.Clone()));
                TMP_tmp2_2 = (PEvent)(new eOpenDoor(null));
                currentMachine.TrySendEvent(TMP_tmp1_7, (Event)TMP_tmp2_2);
            }
            else
            {
                TMP_tmp3_2 = (PrtBool)(((PrtBool)currentMachine.TryRandomBool()));
                if (TMP_tmp3_2)
                {
                    TMP_tmp4_1 = (PMachineValue)(((PMachineValue)((IPrtValue)ElevatorV)?.Clone()));
                    TMP_tmp5 = (PEvent)(new eCloseDoor(null));
                    currentMachine.TrySendEvent(TMP_tmp4_1, (Event)TMP_tmp5);
                }
            }
            TMP_tmp6 = (PrtBool)((PrtValues.SafeEquals(count, ((PrtInt)5))));
            if (TMP_tmp6)
            {
                TMP_tmp7 = (PEvent)(new PHalt(null));
                currentMachine.TryRaiseEvent((Event)TMP_tmp7);
                return;
            }
            TMP_tmp8 = (PrtInt)((count) + (((PrtInt)1)));
            count = TMP_tmp8;
            currentMachine.TryGotoState<Main.Loop>();
            return;
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_1))]
        class __InitState__ : State { }

        [OnEntry(nameof(Anon_7))]
        class Init_1 : State
        {
        }
        [OnEntry(nameof(Anon_8))]
        class Loop : State
        {
        }
    }
    internal partial class Door : PMachine
    {
        private PMachineValue ElevatorV_1 = null;
        public class ConstructorEvent : PEvent { public ConstructorEvent(PMachineValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PMachineValue)value); }
        public Door()
        {
            this.sends.Add(nameof(eCloseDoor));
            this.sends.Add(nameof(eDoorClosed));
            this.sends.Add(nameof(eDoorOpened));
            this.sends.Add(nameof(eDoorStopped));
            this.sends.Add(nameof(eObjectDetected));
            this.sends.Add(nameof(eObjectEncountered));
            this.sends.Add(nameof(eOpenDoor));
            this.sends.Add(nameof(eOperationFailure));
            this.sends.Add(nameof(eOperationSuccess));
            this.sends.Add(nameof(eResetDoor));
            this.sends.Add(nameof(eSendCommandToCloseDoor));
            this.sends.Add(nameof(eSendCommandToOpenDoor));
            this.sends.Add(nameof(eSendCommandToResetDoor));
            this.sends.Add(nameof(eSendCommandToStopDoor));
            this.sends.Add(nameof(eStartTimer));
            this.sends.Add(nameof(eTimerFired));
            this.sends.Add(nameof(eUnit));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(eCloseDoor));
            this.receives.Add(nameof(eDoorClosed));
            this.receives.Add(nameof(eDoorOpened));
            this.receives.Add(nameof(eDoorStopped));
            this.receives.Add(nameof(eObjectDetected));
            this.receives.Add(nameof(eObjectEncountered));
            this.receives.Add(nameof(eOpenDoor));
            this.receives.Add(nameof(eOperationFailure));
            this.receives.Add(nameof(eOperationSuccess));
            this.receives.Add(nameof(eResetDoor));
            this.receives.Add(nameof(eSendCommandToCloseDoor));
            this.receives.Add(nameof(eSendCommandToOpenDoor));
            this.receives.Add(nameof(eSendCommandToResetDoor));
            this.receives.Add(nameof(eSendCommandToStopDoor));
            this.receives.Add(nameof(eStartTimer));
            this.receives.Add(nameof(eTimerFired));
            this.receives.Add(nameof(eUnit));
            this.receives.Add(nameof(PHalt));
        }

        public void Anon_9(Event currentMachine_dequeuedEvent)
        {
            Door currentMachine = this;
            PMachineValue payload = (PMachineValue)(gotoPayload ?? ((PEvent)currentMachine_dequeuedEvent).Payload);
            this.gotoPayload = null;
            ElevatorV_1 = (PMachineValue)(((PMachineValue)((IPrtValue)payload)?.Clone()));
        }
        public void Anon_10(Event currentMachine_dequeuedEvent)
        {
            Door currentMachine = this;
            PMachineValue TMP_tmp0_9 = null;
            PEvent TMP_tmp1_8 = null;
            PEvent TMP_tmp2_3 = null;
            TMP_tmp0_9 = (PMachineValue)(((PMachineValue)((IPrtValue)ElevatorV_1)?.Clone()));
            TMP_tmp1_8 = (PEvent)(new eDoorOpened(null));
            currentMachine.TrySendEvent(TMP_tmp0_9, (Event)TMP_tmp1_8);
            TMP_tmp2_3 = (PEvent)(new eUnit(null));
            currentMachine.TryRaiseEvent((Event)TMP_tmp2_3);
            return;
        }
        public void Anon_11(Event currentMachine_dequeuedEvent)
        {
            Door currentMachine = this;
            PrtBool TMP_tmp0_10 = ((PrtBool)false);
            PEvent TMP_tmp1_9 = null;
            PrtBool TMP_tmp2_4 = ((PrtBool)false);
            PEvent TMP_tmp3_3 = null;
            TMP_tmp0_10 = (PrtBool)(((PrtBool)currentMachine.TryRandomBool()));
            if (TMP_tmp0_10)
            {
                TMP_tmp1_9 = (PEvent)(new eUnit(null));
                currentMachine.TryRaiseEvent((Event)TMP_tmp1_9);
                return;
            }
            else
            {
                TMP_tmp2_4 = (PrtBool)(((PrtBool)currentMachine.TryRandomBool()));
                if (TMP_tmp2_4)
                {
                    TMP_tmp3_3 = (PEvent)(new eObjectEncountered(null));
                    currentMachine.TryRaiseEvent((Event)TMP_tmp3_3);
                    return;
                }
            }
        }
        public void Anon_12(Event currentMachine_dequeuedEvent)
        {
            Door currentMachine = this;
            PMachineValue TMP_tmp0_11 = null;
            PEvent TMP_tmp1_10 = null;
            TMP_tmp0_11 = (PMachineValue)(((PMachineValue)((IPrtValue)ElevatorV_1)?.Clone()));
            TMP_tmp1_10 = (PEvent)(new eObjectDetected(null));
            currentMachine.TrySendEvent(TMP_tmp0_11, (Event)TMP_tmp1_10);
            currentMachine.TryGotoState<Door.Init_2>();
            return;
        }
        public void Anon_13(Event currentMachine_dequeuedEvent)
        {
            Door currentMachine = this;
            PMachineValue TMP_tmp0_12 = null;
            PEvent TMP_tmp1_11 = null;
            TMP_tmp0_12 = (PMachineValue)(((PMachineValue)((IPrtValue)ElevatorV_1)?.Clone()));
            TMP_tmp1_11 = (PEvent)(new eDoorClosed(null));
            currentMachine.TrySendEvent(TMP_tmp0_12, (Event)TMP_tmp1_11);
            currentMachine.TryGotoState<Door.ResetDoor>();
            return;
        }
        public void Anon_14(Event currentMachine_dequeuedEvent)
        {
            Door currentMachine = this;
            PMachineValue TMP_tmp0_13 = null;
            PEvent TMP_tmp1_12 = null;
            TMP_tmp0_13 = (PMachineValue)(((PMachineValue)((IPrtValue)ElevatorV_1)?.Clone()));
            TMP_tmp1_12 = (PEvent)(new eDoorStopped(null));
            currentMachine.TrySendEvent(TMP_tmp0_13, (Event)TMP_tmp1_12);
            currentMachine.TryGotoState<Door.OpenDoor>();
            return;
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_2))]
        class __InitState__ : State { }

        [OnEntry(nameof(Anon_9))]
        [OnEventGotoState(typeof(eSendCommandToOpenDoor), typeof(OpenDoor))]
        [OnEventGotoState(typeof(eSendCommandToCloseDoor), typeof(ConsiderClosingDoor))]
        [IgnoreEvents(typeof(eSendCommandToStopDoor), typeof(eSendCommandToResetDoor), typeof(eResetDoor))]
        class Init_2 : State
        {
        }
        [OnEntry(nameof(Anon_10))]
        [OnEventGotoState(typeof(eUnit), typeof(ResetDoor))]
        class OpenDoor : State
        {
        }
        [OnEntry(nameof(Anon_11))]
        [OnEventGotoState(typeof(eUnit), typeof(CloseDoor))]
        [OnEventGotoState(typeof(eObjectEncountered), typeof(ObjectEncountered))]
        [OnEventGotoState(typeof(eSendCommandToStopDoor), typeof(StopDoor))]
        class ConsiderClosingDoor : State
        {
        }
        [OnEntry(nameof(Anon_12))]
        class ObjectEncountered : State
        {
        }
        [OnEntry(nameof(Anon_13))]
        class CloseDoor : State
        {
        }
        [OnEntry(nameof(Anon_14))]
        class StopDoor : State
        {
        }
        [OnEventGotoState(typeof(eSendCommandToResetDoor), typeof(Init_2))]
        [IgnoreEvents(typeof(eSendCommandToOpenDoor), typeof(eSendCommandToCloseDoor), typeof(eSendCommandToStopDoor))]
        class ResetDoor : State
        {
        }
    }
    internal partial class Timer : PMachine
    {
        private PMachineValue creator = null;
        public class ConstructorEvent : PEvent { public ConstructorEvent(PMachineValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PMachineValue)value); }
        public Timer()
        {
            this.sends.Add(nameof(eCloseDoor));
            this.sends.Add(nameof(eDoorClosed));
            this.sends.Add(nameof(eDoorOpened));
            this.sends.Add(nameof(eDoorStopped));
            this.sends.Add(nameof(eObjectDetected));
            this.sends.Add(nameof(eObjectEncountered));
            this.sends.Add(nameof(eOpenDoor));
            this.sends.Add(nameof(eOperationFailure));
            this.sends.Add(nameof(eOperationSuccess));
            this.sends.Add(nameof(eResetDoor));
            this.sends.Add(nameof(eSendCommandToCloseDoor));
            this.sends.Add(nameof(eSendCommandToOpenDoor));
            this.sends.Add(nameof(eSendCommandToResetDoor));
            this.sends.Add(nameof(eSendCommandToStopDoor));
            this.sends.Add(nameof(eStartTimer));
            this.sends.Add(nameof(eTimerFired));
            this.sends.Add(nameof(eUnit));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(eCloseDoor));
            this.receives.Add(nameof(eDoorClosed));
            this.receives.Add(nameof(eDoorOpened));
            this.receives.Add(nameof(eDoorStopped));
            this.receives.Add(nameof(eObjectDetected));
            this.receives.Add(nameof(eObjectEncountered));
            this.receives.Add(nameof(eOpenDoor));
            this.receives.Add(nameof(eOperationFailure));
            this.receives.Add(nameof(eOperationSuccess));
            this.receives.Add(nameof(eResetDoor));
            this.receives.Add(nameof(eSendCommandToCloseDoor));
            this.receives.Add(nameof(eSendCommandToOpenDoor));
            this.receives.Add(nameof(eSendCommandToResetDoor));
            this.receives.Add(nameof(eSendCommandToStopDoor));
            this.receives.Add(nameof(eStartTimer));
            this.receives.Add(nameof(eTimerFired));
            this.receives.Add(nameof(eUnit));
            this.receives.Add(nameof(PHalt));
        }

        public void Anon_15(Event currentMachine_dequeuedEvent)
        {
            Timer currentMachine = this;
            PMachineValue client = (PMachineValue)(gotoPayload ?? ((PEvent)currentMachine_dequeuedEvent).Payload);
            this.gotoPayload = null;
            creator = (PMachineValue)(((PMachineValue)((IPrtValue)client)?.Clone()));
        }
        public void Anon_16(Event currentMachine_dequeuedEvent)
        {
            Timer currentMachine = this;
            PMachineValue TMP_tmp0_14 = null;
            PEvent TMP_tmp1_13 = null;
            TMP_tmp0_14 = (PMachineValue)(((PMachineValue)((IPrtValue)creator)?.Clone()));
            TMP_tmp1_13 = (PEvent)(new eTimerFired(null));
            currentMachine.TrySendEvent(TMP_tmp0_14, (Event)TMP_tmp1_13);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_3))]
        class __InitState__ : State { }

        [OnEntry(nameof(Anon_15))]
        [OnEventDoAction(typeof(eStartTimer), nameof(Anon_16))]
        class Init_3 : State
        {
        }
    }
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap.Clear();
            PModule.linkMap[nameof(I_Elevator)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Elevator)].Add(nameof(I_Door), nameof(I_Door));
            PModule.linkMap[nameof(I_Elevator)].Add(nameof(I_Timer), nameof(I_Timer));
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_Elevator), nameof(I_Elevator));
            PModule.linkMap[nameof(I_Door)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Timer)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Clear();
            PModule.interfaceDefinitionMap.Add(nameof(I_Elevator), typeof(Elevator));
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_Door), typeof(Door));
            PModule.interfaceDefinitionMap.Add(nameof(I_Timer), typeof(Timer));
        }

        public static void InitializeMonitorObserves()
        {
            PModule.monitorObserves.Clear();
        }

        public static void InitializeMonitorMap(IActorRuntime runtime)
        {
            PModule.monitorMap.Clear();
        }


        [Microsoft.Coyote.SystematicTesting.Test]
        public static void Execute(IActorRuntime runtime)
        {
            runtime.RegisterLog(new PLogFormatter());
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            PHelper.InitializeEnums();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateActor(typeof(_GodMachine), new _GodMachine.Config(typeof(Main)));
        }
    }
    public class I_Elevator : PMachineValue
    {
        public I_Elevator(ActorId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_Main : PMachineValue
    {
        public I_Main(ActorId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_Door : PMachineValue
    {
        public I_Door(ActorId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_Timer : PMachineValue
    {
        public I_Timer(ActorId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.Clear();
            PInterfaces.AddInterface(nameof(I_Elevator), nameof(eCloseDoor), nameof(eDoorClosed), nameof(eDoorOpened), nameof(eDoorStopped), nameof(eObjectDetected), nameof(eObjectEncountered), nameof(eOpenDoor), nameof(eOperationFailure), nameof(eOperationSuccess), nameof(eResetDoor), nameof(eSendCommandToCloseDoor), nameof(eSendCommandToOpenDoor), nameof(eSendCommandToResetDoor), nameof(eSendCommandToStopDoor), nameof(eStartTimer), nameof(eTimerFired), nameof(eUnit), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_Main), nameof(eCloseDoor), nameof(eDoorClosed), nameof(eDoorOpened), nameof(eDoorStopped), nameof(eObjectDetected), nameof(eObjectEncountered), nameof(eOpenDoor), nameof(eOperationFailure), nameof(eOperationSuccess), nameof(eResetDoor), nameof(eSendCommandToCloseDoor), nameof(eSendCommandToOpenDoor), nameof(eSendCommandToResetDoor), nameof(eSendCommandToStopDoor), nameof(eStartTimer), nameof(eTimerFired), nameof(eUnit), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_Door), nameof(eCloseDoor), nameof(eDoorClosed), nameof(eDoorOpened), nameof(eDoorStopped), nameof(eObjectDetected), nameof(eObjectEncountered), nameof(eOpenDoor), nameof(eOperationFailure), nameof(eOperationSuccess), nameof(eResetDoor), nameof(eSendCommandToCloseDoor), nameof(eSendCommandToOpenDoor), nameof(eSendCommandToResetDoor), nameof(eSendCommandToStopDoor), nameof(eStartTimer), nameof(eTimerFired), nameof(eUnit), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_Timer), nameof(eCloseDoor), nameof(eDoorClosed), nameof(eDoorOpened), nameof(eDoorStopped), nameof(eObjectDetected), nameof(eObjectEncountered), nameof(eOpenDoor), nameof(eOperationFailure), nameof(eOperationSuccess), nameof(eResetDoor), nameof(eSendCommandToCloseDoor), nameof(eSendCommandToOpenDoor), nameof(eSendCommandToResetDoor), nameof(eSendCommandToStopDoor), nameof(eStartTimer), nameof(eTimerFired), nameof(eUnit), nameof(PHalt));
        }
    }

    public partial class PHelper
    {
        public static void InitializeEnums()
        {
            PrtEnum.Clear();
        }
    }

}
#pragma warning restore 162, 219, 414
