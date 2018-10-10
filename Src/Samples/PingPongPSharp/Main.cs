using Microsoft.PSharp;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PrtSharp;
using PrtSharp.Values;
using System.Threading;
using System.Threading.Tasks;

namespace Main
{
    public static partial class GlobalFunctions_Main { }
    internal class eD0Entry : PEvent<object>
    {
        static eD0Entry() { AssertVal = -1; AssumeVal = 1; }
        public eD0Entry() : base() { }
        public eD0Entry(object payload) : base(payload) { }
    }
    internal class eD0Exit : PEvent<object>
    {
        static eD0Exit() { AssertVal = -1; AssumeVal = 1; }
        public eD0Exit() : base() { }
        public eD0Exit(object payload) : base(payload) { }
    }
    internal class eTimerFired : PEvent<object>
    {
        static eTimerFired() { AssertVal = 1; AssumeVal = -1; }
        public eTimerFired() : base() { }
        public eTimerFired(object payload) : base(payload) { }
    }
    internal class eSwitchStatusChange : PEvent<object>
    {
        static eSwitchStatusChange() { AssertVal = -1; AssumeVal = 1; }
        public eSwitchStatusChange() : base() { }
        public eSwitchStatusChange(object payload) : base(payload) { }
    }
    internal class eTransferSuccess : PEvent<object>
    {
        static eTransferSuccess() { AssertVal = -1; AssumeVal = 1; }
        public eTransferSuccess() : base() { }
        public eTransferSuccess(object payload) : base(payload) { }
    }
    internal class eTransferFailure : PEvent<object>
    {
        static eTransferFailure() { AssertVal = -1; AssumeVal = 1; }
        public eTransferFailure() : base() { }
        public eTransferFailure(object payload) : base(payload) { }
    }
    internal class eStopTimer : PEvent<object>
    {
        static eStopTimer() { AssertVal = -1; AssumeVal = 1; }
        public eStopTimer() : base() { }
        public eStopTimer(object payload) : base(payload) { }
    }
    internal class eUpdateBarGraphStateUsingControlTransfer : PEvent<object>
    {
        static eUpdateBarGraphStateUsingControlTransfer() { AssertVal = -1; AssumeVal = 1; }
        public eUpdateBarGraphStateUsingControlTransfer() : base() { }
        public eUpdateBarGraphStateUsingControlTransfer(object payload) : base(payload) { }
    }
    internal class eSetLedStateToUnstableUsingControlTransfer : PEvent<object>
    {
        static eSetLedStateToUnstableUsingControlTransfer() { AssertVal = -1; AssumeVal = 1; }
        public eSetLedStateToUnstableUsingControlTransfer() : base() { }
        public eSetLedStateToUnstableUsingControlTransfer(object payload) : base(payload) { }
    }
    internal class eStartDebounceTimer : PEvent<object>
    {
        static eStartDebounceTimer() { AssertVal = -1; AssumeVal = 1; }
        public eStartDebounceTimer() : base() { }
        public eStartDebounceTimer(object payload) : base(payload) { }
    }
    internal class eSetLedStateToStableUsingControlTransfer : PEvent<object>
    {
        static eSetLedStateToStableUsingControlTransfer() { AssertVal = -1; AssumeVal = 1; }
        public eSetLedStateToStableUsingControlTransfer() : base() { }
        public eSetLedStateToStableUsingControlTransfer(object payload) : base(payload) { }
    }
    internal class eStoppingSuccess : PEvent<object>
    {
        static eStoppingSuccess() { AssertVal = 1; AssumeVal = -1; }
        public eStoppingSuccess() : base() { }
        public eStoppingSuccess(object payload) : base(payload) { }
    }
    internal class eStoppingFailure : PEvent<object>
    {
        static eStoppingFailure() { AssertVal = 1; AssumeVal = -1; }
        public eStoppingFailure() : base() { }
        public eStoppingFailure(object payload) : base(payload) { }
    }
    internal class eOperationSuccess : PEvent<object>
    {
        static eOperationSuccess() { AssertVal = 1; AssumeVal = -1; }
        public eOperationSuccess() : base() { }
        public eOperationSuccess(object payload) : base(payload) { }
    }
    internal class eOperationFailure : PEvent<object>
    {
        static eOperationFailure() { AssertVal = 1; AssumeVal = -1; }
        public eOperationFailure() : base() { }
        public eOperationFailure(object payload) : base(payload) { }
    }
    internal class eTimerStopped : PEvent<object>
    {
        static eTimerStopped() { AssertVal = 1; AssumeVal = -1; }
        public eTimerStopped() : base() { }
        public eTimerStopped(object payload) : base(payload) { }
    }
    internal class eYes : PEvent<object>
    {
        static eYes() { AssertVal = 1; AssumeVal = -1; }
        public eYes() : base() { }
        public eYes(object payload) : base(payload) { }
    }
    internal class eNo : PEvent<object>
    {
        static eNo() { AssertVal = 1; AssumeVal = -1; }
        public eNo() : base() { }
        public eNo(object payload) : base(payload) { }
    }
    internal class eUnit : PEvent<object>
    {
        static eUnit() { AssertVal = 1; AssumeVal = -1; }
        public eUnit() : base() { }
        public eUnit(object payload) : base(payload) { }
    }
    internal class SwitchMachine : PMachine
    {
        private PMachineValue Driver = null;
        public SwitchMachine()
        {
            this.sends.Add(nameof(eSwitchStatusChange));
            this.receives.Add(nameof(eD0Entry));
            this.receives.Add(nameof(eD0Exit));
            this.receives.Add(nameof(eNo));
            this.receives.Add(nameof(eOperationFailure));
            this.receives.Add(nameof(eOperationSuccess));
            this.receives.Add(nameof(eSetLedStateToStableUsingControlTransfer));
            this.receives.Add(nameof(eSetLedStateToUnstableUsingControlTransfer));
            this.receives.Add(nameof(eStartDebounceTimer));
            this.receives.Add(nameof(eStopTimer));
            this.receives.Add(nameof(eStoppingFailure));
            this.receives.Add(nameof(eStoppingSuccess));
            this.receives.Add(nameof(eSwitchStatusChange));
            this.receives.Add(nameof(eTimerFired));
            this.receives.Add(nameof(eTimerStopped));
            this.receives.Add(nameof(eTransferFailure));
            this.receives.Add(nameof(eTransferSuccess));
            this.receives.Add(nameof(eUnit));
            this.receives.Add(nameof(eUpdateBarGraphStateUsingControlTransfer));
            this.receives.Add(nameof(eYes));
        }

        public void Anon()
        {
            SwitchMachine currentMachine = this;
            PMachineValue payload = (currentMachine.ReceivedEvent as PEvent<PMachineValue>).Payload;
            PMachineValue TMP_tmp0 = null;
            IEventWithPayload<object> TMP_tmp1 = null;
            TMP_tmp0 = ((PMachineValue)payload);
            Driver = TMP_tmp0;
            TMP_tmp1 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp1);
            throw new PUnreachableCodeException();
        }
        public void Anon_1()
        {
            SwitchMachine currentMachine = this;
            IEventWithPayload<object> TMP_tmp0_1 = null;
            TMP_tmp0_1 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp0_1);
            throw new PUnreachableCodeException();
        }
        public void Anon_2()
        {
            SwitchMachine currentMachine = this;
            PMachineValue TMP_tmp0_2 = null;
            IEventWithPayload<object> TMP_tmp1_1 = null;
            IEventWithPayload<object> TMP_tmp2 = null;
            TMP_tmp0_2 = ((PMachineValue)((IPrtValue)Driver).Clone());
            TMP_tmp1_1 = new eSwitchStatusChange(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_2, (Event)TMP_tmp1_1);
            TMP_tmp2 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp2);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(_Init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon))]
        [OnEventGotoState(typeof(eUnit), typeof(Switch_Init))]
        class _Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_1))]
        [OnEventGotoState(typeof(eUnit), typeof(ChangeSwitchStatus))]
        class Switch_Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_2))]
        [OnEventGotoState(typeof(eUnit), typeof(ChangeSwitchStatus))]
        class ChangeSwitchStatus : MachineState
        {
        }
    }
    internal class LEDMachine : PMachine
    {
        private PMachineValue Driver_1 = null;
        public LEDMachine()
        {
            this.sends.Add(nameof(eTransferFailure));
            this.sends.Add(nameof(eTransferSuccess));
            this.receives.Add(nameof(eD0Entry));
            this.receives.Add(nameof(eD0Exit));
            this.receives.Add(nameof(eNo));
            this.receives.Add(nameof(eOperationFailure));
            this.receives.Add(nameof(eOperationSuccess));
            this.receives.Add(nameof(eSetLedStateToStableUsingControlTransfer));
            this.receives.Add(nameof(eSetLedStateToUnstableUsingControlTransfer));
            this.receives.Add(nameof(eStartDebounceTimer));
            this.receives.Add(nameof(eStopTimer));
            this.receives.Add(nameof(eStoppingFailure));
            this.receives.Add(nameof(eStoppingSuccess));
            this.receives.Add(nameof(eSwitchStatusChange));
            this.receives.Add(nameof(eTimerFired));
            this.receives.Add(nameof(eTimerStopped));
            this.receives.Add(nameof(eTransferFailure));
            this.receives.Add(nameof(eTransferSuccess));
            this.receives.Add(nameof(eUnit));
            this.receives.Add(nameof(eUpdateBarGraphStateUsingControlTransfer));
            this.receives.Add(nameof(eYes));
        }

        public void Anon_3()
        {
            LEDMachine currentMachine = this;
            PMachineValue payload_1 = (currentMachine.ReceivedEvent as PEvent<PMachineValue>).Payload;
            PMachineValue TMP_tmp0_3 = null;
            IEventWithPayload<object> TMP_tmp1_2 = null;
            TMP_tmp0_3 = ((PMachineValue)payload_1);
            Driver_1 = TMP_tmp0_3;
            TMP_tmp1_2 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp1_2);
            throw new PUnreachableCodeException();
        }
        public void Anon_4()
        {
            LEDMachine currentMachine = this;
            PrtBool TMP_tmp0_4 = ((PrtBool)false);
            PMachineValue TMP_tmp1_3 = null;
            IEventWithPayload<object> TMP_tmp2_1 = null;
            PMachineValue TMP_tmp3 = null;
            IEventWithPayload<object> TMP_tmp4 = null;
            IEventWithPayload<object> TMP_tmp5 = null;
            TMP_tmp0_4 = ((PrtBool)currentMachine.Random());
            if (TMP_tmp0_4)
            {
                TMP_tmp1_3 = ((PMachineValue)((IPrtValue)Driver_1).Clone());
                TMP_tmp2_1 = new eTransferSuccess(null);
                currentMachine.SendEvent(currentMachine, TMP_tmp1_3, (Event)TMP_tmp2_1);
            }
            else
            {
                TMP_tmp3 = ((PMachineValue)((IPrtValue)Driver_1).Clone());
                TMP_tmp4 = new eTransferFailure(null);
                currentMachine.SendEvent(currentMachine, TMP_tmp3, (Event)TMP_tmp4);
            }
            TMP_tmp5 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp5);
            throw new PUnreachableCodeException();
        }
        public void Anon_5()
        {
            LEDMachine currentMachine = this;
            PMachineValue TMP_tmp0_5 = null;
            IEventWithPayload<object> TMP_tmp1_4 = null;
            TMP_tmp0_5 = ((PMachineValue)((IPrtValue)Driver_1).Clone());
            TMP_tmp1_4 = new eTransferSuccess(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_5, (Event)TMP_tmp1_4);
        }
        public void Anon_6()
        {
            LEDMachine currentMachine = this;
            PMachineValue TMP_tmp0_6 = null;
            IEventWithPayload<object> TMP_tmp1_5 = null;
            IEventWithPayload<object> TMP_tmp2_2 = null;
            TMP_tmp0_6 = ((PMachineValue)((IPrtValue)Driver_1).Clone());
            TMP_tmp1_5 = new eTransferSuccess(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_6, (Event)TMP_tmp1_5);
            TMP_tmp2_2 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp2_2);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(_Init_1))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_3))]
        [OnEventGotoState(typeof(eUnit), typeof(LED_Init))]
        class _Init_1 : MachineState
        {
        }
        [OnEventGotoState(typeof(eUpdateBarGraphStateUsingControlTransfer), typeof(ProcessUpdateLED))]
        [OnEventGotoState(typeof(eSetLedStateToUnstableUsingControlTransfer), typeof(UnstableLED))]
        [OnEventGotoState(typeof(eSetLedStateToStableUsingControlTransfer), typeof(StableLED))]
        class LED_Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_4))]
        [OnEventGotoState(typeof(eUnit), typeof(LED_Init))]
        class ProcessUpdateLED : MachineState
        {
        }
        [OnEntry(nameof(Anon_5))]
        [OnEventGotoState(typeof(eSetLedStateToStableUsingControlTransfer), typeof(LED_Init))]
        [OnEventGotoState(typeof(eUpdateBarGraphStateUsingControlTransfer), typeof(ProcessUpdateLED))]
        class UnstableLED : MachineState
        {
        }
        [OnEntry(nameof(Anon_6))]
        [OnEventGotoState(typeof(eUnit), typeof(LED_Init))]
        class StableLED : MachineState
        {
        }
    }
    internal class TimerMachine : PMachine
    {
        private PMachineValue Driver_2 = null;
        public TimerMachine()
        {
            this.sends.Add(nameof(eStoppingFailure));
            this.sends.Add(nameof(eStoppingSuccess));
            this.sends.Add(nameof(eTimerFired));
            this.receives.Add(nameof(eD0Entry));
            this.receives.Add(nameof(eD0Exit));
            this.receives.Add(nameof(eNo));
            this.receives.Add(nameof(eOperationFailure));
            this.receives.Add(nameof(eOperationSuccess));
            this.receives.Add(nameof(eSetLedStateToStableUsingControlTransfer));
            this.receives.Add(nameof(eSetLedStateToUnstableUsingControlTransfer));
            this.receives.Add(nameof(eStartDebounceTimer));
            this.receives.Add(nameof(eStopTimer));
            this.receives.Add(nameof(eStoppingFailure));
            this.receives.Add(nameof(eStoppingSuccess));
            this.receives.Add(nameof(eSwitchStatusChange));
            this.receives.Add(nameof(eTimerFired));
            this.receives.Add(nameof(eTimerStopped));
            this.receives.Add(nameof(eTransferFailure));
            this.receives.Add(nameof(eTransferSuccess));
            this.receives.Add(nameof(eUnit));
            this.receives.Add(nameof(eUpdateBarGraphStateUsingControlTransfer));
            this.receives.Add(nameof(eYes));
        }

        public void Anon_7()
        {
            TimerMachine currentMachine = this;
            PMachineValue payload_2 = (currentMachine.ReceivedEvent as PEvent<PMachineValue>).Payload;
            PMachineValue TMP_tmp0_7 = null;
            IEventWithPayload<object> TMP_tmp1_6 = null;
            TMP_tmp0_7 = ((PMachineValue)payload_2);
            Driver_2 = TMP_tmp0_7;
            TMP_tmp1_6 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp1_6);
            throw new PUnreachableCodeException();
        }
        public void Anon_8()
        {
            TimerMachine currentMachine = this;
        }
        public void Anon_9()
        {
            TimerMachine currentMachine = this;
            PrtBool TMP_tmp0_8 = ((PrtBool)false);
            IEventWithPayload<object> TMP_tmp1_7 = null;
            TMP_tmp0_8 = ((PrtBool)currentMachine.Random());
            if (TMP_tmp0_8)
            {
                TMP_tmp1_7 = new eUnit(null);
                currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp1_7);
                throw new PUnreachableCodeException();
            }
        }
        public void Anon_10()
        {
            TimerMachine currentMachine = this;
            PMachineValue TMP_tmp0_9 = null;
            IEventWithPayload<object> TMP_tmp1_8 = null;
            IEventWithPayload<object> TMP_tmp2_3 = null;
            TMP_tmp0_9 = ((PMachineValue)((IPrtValue)Driver_2).Clone());
            TMP_tmp1_8 = new eTimerFired(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_9, (Event)TMP_tmp1_8);
            TMP_tmp2_3 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp2_3);
            throw new PUnreachableCodeException();
        }
        public void Anon_11()
        {
            TimerMachine currentMachine = this;
            PrtBool TMP_tmp0_10 = ((PrtBool)false);
            PMachineValue TMP_tmp1_9 = null;
            IEventWithPayload<object> TMP_tmp2_4 = null;
            PMachineValue TMP_tmp3_1 = null;
            IEventWithPayload<object> TMP_tmp4_1 = null;
            PMachineValue TMP_tmp5_1 = null;
            IEventWithPayload<object> TMP_tmp6 = null;
            IEventWithPayload<object> TMP_tmp7 = null;
            TMP_tmp0_10 = ((PrtBool)currentMachine.Random());
            if (TMP_tmp0_10)
            {
                TMP_tmp1_9 = ((PMachineValue)((IPrtValue)Driver_2).Clone());
                TMP_tmp2_4 = new eStoppingFailure(null);
                currentMachine.SendEvent(currentMachine, TMP_tmp1_9, (Event)TMP_tmp2_4);
                TMP_tmp3_1 = ((PMachineValue)((IPrtValue)Driver_2).Clone());
                TMP_tmp4_1 = new eTimerFired(null);
                currentMachine.SendEvent(currentMachine, TMP_tmp3_1, (Event)TMP_tmp4_1);
            }
            else
            {
                TMP_tmp5_1 = ((PMachineValue)((IPrtValue)Driver_2).Clone());
                TMP_tmp6 = new eStoppingSuccess(null);
                currentMachine.SendEvent(currentMachine, TMP_tmp5_1, (Event)TMP_tmp6);
            }
            TMP_tmp7 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp7);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(_Init_2))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_7))]
        [OnEventGotoState(typeof(eUnit), typeof(Timer_Init))]
        class _Init_2 : MachineState
        {
        }
        [OnEntry(nameof(Anon_8))]
        [OnEventGotoState(typeof(eStartDebounceTimer), typeof(TimerStarted))]
        [IgnoreEvents(typeof(eStopTimer))]
        class Timer_Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_9))]
        [OnEventGotoState(typeof(eUnit), typeof(SendTimerFired))]
        [OnEventGotoState(typeof(eStopTimer), typeof(ConsmachineeringStoppingTimer))]
        [DeferEvents(typeof(eStartDebounceTimer))]
        class TimerStarted : MachineState
        {
        }
        [OnEntry(nameof(Anon_10))]
        [OnEventGotoState(typeof(eUnit), typeof(Timer_Init))]
        [DeferEvents(typeof(eStartDebounceTimer))]
        class SendTimerFired : MachineState
        {
        }
        [OnEntry(nameof(Anon_11))]
        [OnEventGotoState(typeof(eUnit), typeof(Timer_Init))]
        [DeferEvents(typeof(eStartDebounceTimer))]
        class ConsmachineeringStoppingTimer : MachineState
        {
        }
    }
    internal class OSRDriverMachine : PMachine
    {
        private PMachineValue TimerV = null;
        private PMachineValue LEDV = null;
        private PMachineValue SwitchV = null;
        private PrtBool check = ((PrtBool)false);
        public OSRDriverMachine()
        {
            this.sends.Add(nameof(eSetLedStateToStableUsingControlTransfer));
            this.sends.Add(nameof(eSetLedStateToUnstableUsingControlTransfer));
            this.sends.Add(nameof(eStartDebounceTimer));
            this.sends.Add(nameof(eStopTimer));
            this.sends.Add(nameof(eUpdateBarGraphStateUsingControlTransfer));
            this.receives.Add(nameof(eD0Entry));
            this.receives.Add(nameof(eD0Exit));
            this.receives.Add(nameof(eNo));
            this.receives.Add(nameof(eOperationFailure));
            this.receives.Add(nameof(eOperationSuccess));
            this.receives.Add(nameof(eSetLedStateToStableUsingControlTransfer));
            this.receives.Add(nameof(eSetLedStateToUnstableUsingControlTransfer));
            this.receives.Add(nameof(eStartDebounceTimer));
            this.receives.Add(nameof(eStopTimer));
            this.receives.Add(nameof(eStoppingFailure));
            this.receives.Add(nameof(eStoppingSuccess));
            this.receives.Add(nameof(eSwitchStatusChange));
            this.receives.Add(nameof(eTimerFired));
            this.receives.Add(nameof(eTimerStopped));
            this.receives.Add(nameof(eTransferFailure));
            this.receives.Add(nameof(eTransferSuccess));
            this.receives.Add(nameof(eUnit));
            this.receives.Add(nameof(eUpdateBarGraphStateUsingControlTransfer));
            this.receives.Add(nameof(eYes));
            this.creates.Add(nameof(I_LEDInterface));
            this.creates.Add(nameof(I_SwitchInterface));
            this.creates.Add(nameof(I_TimerInterface));
        }

        public void Anon_12()
        {
            OSRDriverMachine currentMachine = this;
            PMachineValue TMP_tmp0_11 = null;
            PMachineValue TMP_tmp1_10 = null;
            PMachineValue TMP_tmp2_5 = null;
            PMachineValue TMP_tmp3_2 = null;
            PMachineValue TMP_tmp4_2 = null;
            PMachineValue TMP_tmp5_2 = null;
            IEventWithPayload<object> TMP_tmp6_1 = null;
            TMP_tmp0_11 = (PInterfaces.IsCoercionAllowed(currentMachine.self,"I_OSRDriverInterface") ? new PMachineValue((currentMachine.self).Id, PInterfaces.GetPermissions("I_OSRDriverInterface")) : null);
            TMP_tmp1_10 = currentMachine.CreateInterface<I_TimerInterface>(currentMachine, TMP_tmp0_11);
            TimerV = TMP_tmp1_10;
            TMP_tmp2_5 = (PInterfaces.IsCoercionAllowed(currentMachine.selfPInterfaces.GetPermissions("I_OSRDriverInterface")) ? new PMachineValue((currentMachine.self).Id, PInterfaces.GetPermissions("I_OSRDriverInterface")) : null);
            TMP_tmp3_2 = currentMachine.CreateInterface<I_LEDInterface>(currentMachine, TMP_tmp2_5);
            LEDV = TMP_tmp3_2;
            TMP_tmp4_2 = (PInterfaces.IsCoercionAllowed(currentMachine.selfPInterfaces.GetPermissions("I_OSRDriverInterface")) ? new PMachineValue((currentMachine.self).Id, PInterfaces.GetPermissions("I_OSRDriverInterface")) : null);
            TMP_tmp5_2 = currentMachine.CreateInterface<I_SwitchInterface>(currentMachine, TMP_tmp4_2);
            SwitchV = TMP_tmp5_2;
            TMP_tmp6_1 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp6_1);
            throw new PUnreachableCodeException();
        }
        public void Anon_13()
        {
            OSRDriverMachine currentMachine = this;
            IEventWithPayload<object> TMP_tmp0_12 = null;
            CompleteDStateTransition();
            TMP_tmp0_12 = new eOperationSuccess(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp0_12);
            throw new PUnreachableCodeException();
        }
        public void CompleteDStateTransition()
        {
            OSRDriverMachine currentMachine = this;
        }
        public void Anon_14()
        {
            OSRDriverMachine currentMachine = this;
        }
        public void Anon_15()
        {
            OSRDriverMachine currentMachine = this;
            IEventWithPayload<object> TMP_tmp0_13 = null;
            CompleteDStateTransition();
            TMP_tmp0_13 = new eOperationSuccess(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp0_13);
            throw new PUnreachableCodeException();
        }
        public void StoreSwitchAndEnableSwitchStatusChange()
        {
            OSRDriverMachine currentMachine = this;
        }
        public PrtBool CheckIfSwitchStatusChanged()
        {
            OSRDriverMachine currentMachine = this;
            PrtBool TMP_tmp0_14 = ((PrtBool)false);
            TMP_tmp0_14 = ((PrtBool)currentMachine.Random());
            if (TMP_tmp0_14)
            {
                return ((PrtBool)true);
            }
            else
            {
                return ((PrtBool)false);
            }
        }
        public void UpdateBarGraphStateUsingControlTransfer()
        {
            OSRDriverMachine currentMachine = this;
            PMachineValue TMP_tmp0_15 = null;
            IEventWithPayload<object> TMP_tmp1_11 = null;
            TMP_tmp0_15 = ((PMachineValue)((IPrtValue)LEDV).Clone());
            TMP_tmp1_11 = new eUpdateBarGraphStateUsingControlTransfer(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_15, (Event)TMP_tmp1_11);
        }
        public void SetLedStateToStableUsingControlTransfer()
        {
            OSRDriverMachine currentMachine = this;
            PMachineValue TMP_tmp0_16 = null;
            IEventWithPayload<object> TMP_tmp1_12 = null;
            TMP_tmp0_16 = ((PMachineValue)((IPrtValue)LEDV).Clone());
            TMP_tmp1_12 = new eSetLedStateToStableUsingControlTransfer(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_16, (Event)TMP_tmp1_12);
        }
        public void SetLedStateToUnstableUsingControlTransfer()
        {
            OSRDriverMachine currentMachine = this;
            PMachineValue TMP_tmp0_17 = null;
            IEventWithPayload<object> TMP_tmp1_13 = null;
            TMP_tmp0_17 = ((PMachineValue)((IPrtValue)LEDV).Clone());
            TMP_tmp1_13 = new eSetLedStateToUnstableUsingControlTransfer(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_17, (Event)TMP_tmp1_13);
        }
        public void StartDebounceTimer()
        {
            OSRDriverMachine currentMachine = this;
            PMachineValue TMP_tmp0_18 = null;
            IEventWithPayload<object> TMP_tmp1_14 = null;
            TMP_tmp0_18 = ((PMachineValue)((IPrtValue)TimerV).Clone());
            TMP_tmp1_14 = new eStartDebounceTimer(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_18, (Event)TMP_tmp1_14);
        }
        public void Anon_16()
        {
            OSRDriverMachine currentMachine = this;
            PrtBool TMP_tmp0_19 = ((PrtBool)false);
            IEventWithPayload<object> TMP_tmp1_15 = null;
            IEventWithPayload<object> TMP_tmp2_6 = null;
            StoreSwitchAndEnableSwitchStatusChange();
            TMP_tmp0_19 = CheckIfSwitchStatusChanged();
            check = TMP_tmp0_19;
            if (check)
            {
                TMP_tmp1_15 = new eYes(null);
                currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp1_15);
                throw new PUnreachableCodeException();
            }
            else
            {
                TMP_tmp2_6 = new eNo(null);
                currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp2_6);
                throw new PUnreachableCodeException();
            }
        }
        public void Anon_17()
        {
            OSRDriverMachine currentMachine = this;
            UpdateBarGraphStateUsingControlTransfer();
        }
        public void Anon_18()
        {
            OSRDriverMachine currentMachine = this;
            SetLedStateToUnstableUsingControlTransfer();
        }
        public void Anon_19()
        {
            OSRDriverMachine currentMachine = this;
            StartDebounceTimer();
        }
        public void Anon_20()
        {
            OSRDriverMachine currentMachine = this;
            SetLedStateToStableUsingControlTransfer();
        }
        public void Anon_21()
        {
            OSRDriverMachine currentMachine = this;
            IEventWithPayload<object> TMP_tmp0_20 = null;
            TMP_tmp0_20 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp0_20);
            throw new PUnreachableCodeException();
        }
        public void Anon_22()
        {
            OSRDriverMachine currentMachine = this;
            IEventWithPayload<object> TMP_tmp0_21 = null;
            TMP_tmp0_21 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp0_21);
            throw new PUnreachableCodeException();
        }
        public void Anon_23()
        {
            OSRDriverMachine currentMachine = this;
            PMachineValue TMP_tmp0_22 = null;
            IEventWithPayload<object> TMP_tmp1_16 = null;
            TMP_tmp0_22 = ((PMachineValue)((IPrtValue)TimerV).Clone());
            TMP_tmp1_16 = new eStopTimer(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_22, (Event)TMP_tmp1_16);
        }
        public void Anon_24()
        {
            OSRDriverMachine currentMachine = this;
        }
        public void Anon_25()
        {
            OSRDriverMachine currentMachine = this;
            IEventWithPayload<object> TMP_tmp0_23 = null;
            TMP_tmp0_23 = new eTimerStopped(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp0_23);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Driver_Init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_12))]
        [OnEventGotoState(typeof(eUnit), typeof(sDxDriver))]
        [DeferEvents(typeof(eSwitchStatusChange))]
        class Driver_Init : MachineState
        {
        }
        [OnEventGotoState(typeof(eD0Entry), typeof(sCompleteD0EntryDriver))]
        [DeferEvents(typeof(eSwitchStatusChange))]
        [IgnoreEvents(typeof(eD0Exit))]
        class sDxDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_13))]
        [OnEventGotoState(typeof(eOperationSuccess), typeof(sWaitingForSwitchStatusChangeDriver))]
        [DeferEvents(typeof(eSwitchStatusChange))]
        class sCompleteD0EntryDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_14))]
        [OnEventGotoState(typeof(eD0Exit), typeof(sCompletingD0ExitDriver))]
        [OnEventGotoState(typeof(eSwitchStatusChange), typeof(sStoringSwitchAndCheckingIfStateChangedDriver))]
        [IgnoreEvents(typeof(eD0Entry))]
        class sWaitingForSwitchStatusChangeDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_15))]
        [OnEventGotoState(typeof(eOperationSuccess), typeof(sDxDriver))]
        class sCompletingD0ExitDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_16))]
        [OnEventGotoState(typeof(eYes), typeof(sUpdatingBarGraphStateDriver))]
        [OnEventGotoState(typeof(eNo), typeof(sWaitingForTimerDriver))]
        [IgnoreEvents(typeof(eD0Entry))]
        class sStoringSwitchAndCheckingIfStateChangedDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_17))]
        [OnEventGotoState(typeof(eTransferSuccess), typeof(sUpdatingLedStateToUnstableDriver))]
        [OnEventGotoState(typeof(eTransferFailure), typeof(sUpdatingLedStateToUnstableDriver))]
        [DeferEvents(typeof(eD0Exit), typeof(eSwitchStatusChange))]
        [IgnoreEvents(typeof(eD0Entry))]
        class sUpdatingBarGraphStateDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_18))]
        [OnEventGotoState(typeof(eTransferSuccess), typeof(sWaitingForTimerDriver))]
        [DeferEvents(typeof(eD0Exit), typeof(eSwitchStatusChange))]
        [IgnoreEvents(typeof(eD0Entry))]
        class sUpdatingLedStateToUnstableDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_19))]
        [OnEventGotoState(typeof(eTimerFired), typeof(sUpdatingLedStateToStableDriver))]
        [OnEventGotoState(typeof(eSwitchStatusChange), typeof(sStoppingTimerOnStatusChangeDriver))]
        [OnEventGotoState(typeof(eD0Exit), typeof(sStoppingTimerOnD0ExitDriver))]
        [IgnoreEvents(typeof(eD0Entry))]
        class sWaitingForTimerDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_20))]
        [OnEventGotoState(typeof(eTransferSuccess), typeof(sWaitingForSwitchStatusChangeDriver))]
        [DeferEvents(typeof(eD0Exit), typeof(eSwitchStatusChange))]
        [IgnoreEvents(typeof(eD0Entry))]
        class sUpdatingLedStateToStableDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_21))]
        [OnEventPushState(typeof(eUnit), typeof(sStoppingTimerDriver))]
        [OnEventGotoState(typeof(eTimerStopped), typeof(sStoringSwitchAndCheckingIfStateChangedDriver))]
        [DeferEvents(typeof(eD0Exit), typeof(eSwitchStatusChange))]
        [IgnoreEvents(typeof(eD0Entry))]
        class sStoppingTimerOnStatusChangeDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_22))]
        [OnEventGotoState(typeof(eTimerStopped), typeof(sCompletingD0ExitDriver))]
        [OnEventPushState(typeof(eUnit), typeof(sStoppingTimerDriver))]
        [DeferEvents(typeof(eD0Exit), typeof(eSwitchStatusChange))]
        [IgnoreEvents(typeof(eD0Entry))]
        class sStoppingTimerOnD0ExitDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_23))]
        [OnEventGotoState(typeof(eStoppingSuccess), typeof(sReturningTimerStoppedDriver))]
        [OnEventGotoState(typeof(eStoppingFailure), typeof(sWaitingForTimerToFlushDriver))]
        [OnEventGotoState(typeof(eTimerFired), typeof(sReturningTimerStoppedDriver))]
        [IgnoreEvents(typeof(eD0Entry))]
        class sStoppingTimerDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_24))]
        [OnEventGotoState(typeof(eTimerFired), typeof(sReturningTimerStoppedDriver))]
        [DeferEvents(typeof(eD0Exit), typeof(eSwitchStatusChange))]
        [IgnoreEvents(typeof(eD0Entry))]
        class sWaitingForTimerToFlushDriver : MachineState
        {
        }
        [OnEntry(nameof(Anon_25))]
        [IgnoreEvents(typeof(eD0Entry))]
        class sReturningTimerStoppedDriver : MachineState
        {
        }
    }
    internal class Main : PMachine
    {
        private PMachineValue Driver_3 = null;
        public Main()
        {
            this.sends.Add(nameof(eD0Entry));
            this.sends.Add(nameof(eD0Exit));
            this.receives.Add(nameof(eD0Entry));
            this.receives.Add(nameof(eD0Exit));
            this.receives.Add(nameof(eNo));
            this.receives.Add(nameof(eOperationFailure));
            this.receives.Add(nameof(eOperationSuccess));
            this.receives.Add(nameof(eSetLedStateToStableUsingControlTransfer));
            this.receives.Add(nameof(eSetLedStateToUnstableUsingControlTransfer));
            this.receives.Add(nameof(eStartDebounceTimer));
            this.receives.Add(nameof(eStopTimer));
            this.receives.Add(nameof(eStoppingFailure));
            this.receives.Add(nameof(eStoppingSuccess));
            this.receives.Add(nameof(eSwitchStatusChange));
            this.receives.Add(nameof(eTimerFired));
            this.receives.Add(nameof(eTimerStopped));
            this.receives.Add(nameof(eTransferFailure));
            this.receives.Add(nameof(eTransferSuccess));
            this.receives.Add(nameof(eUnit));
            this.receives.Add(nameof(eUpdateBarGraphStateUsingControlTransfer));
            this.receives.Add(nameof(eYes));
            this.creates.Add(nameof(I_OSRDriverInterface));
        }

        public void Anon_26()
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0_24 = null;
            IEventWithPayload<object> TMP_tmp1_17 = null;
            TMP_tmp0_24 = currentMachine.CreateInterface<I_OSRDriverInterface>(currentMachine);
            Driver_3 = TMP_tmp0_24;
            TMP_tmp1_17 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp1_17);
            throw new PUnreachableCodeException();
        }
        public void Anon_27()
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0_25 = null;
            IEventWithPayload<object> TMP_tmp1_18 = null;
            IEventWithPayload<object> TMP_tmp2_7 = null;
            TMP_tmp0_25 = ((PMachineValue)((IPrtValue)Driver_3).Clone());
            TMP_tmp1_18 = new eD0Entry(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_25, (Event)TMP_tmp1_18);
            TMP_tmp2_7 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp2_7);
            throw new PUnreachableCodeException();
        }
        public void Anon_28()
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0_26 = null;
            IEventWithPayload<object> TMP_tmp1_19 = null;
            IEventWithPayload<object> TMP_tmp2_8 = null;
            TMP_tmp0_26 = ((PMachineValue)((IPrtValue)Driver_3).Clone());
            TMP_tmp1_19 = new eD0Exit(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_26, (Event)TMP_tmp1_19);
            TMP_tmp2_8 = new eUnit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp2_8);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(User_Init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_26))]
        [OnEventGotoState(typeof(eUnit), typeof(S0))]
        class User_Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_27))]
        [OnEventGotoState(typeof(eUnit), typeof(S1))]
        class S0 : MachineState
        {
        }
        [OnEntry(nameof(Anon_28))]
        [OnEventGotoState(typeof(eUnit), typeof(S0))]
        class S1 : MachineState
        {
        }
    }
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_OSRDriverInterface), nameof(I_OSRDriverInterface));
            PModule.linkMap[nameof(I_LEDInterface)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_TimerInterface)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_SwitchInterface)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_OSRDriverInterface)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_OSRDriverInterface)].Add(nameof(I_LEDInterface), nameof(I_LEDInterface));
            PModule.linkMap[nameof(I_OSRDriverInterface)].Add(nameof(I_SwitchInterface), nameof(I_SwitchInterface));
            PModule.linkMap[nameof(I_OSRDriverInterface)].Add(nameof(I_TimerInterface), nameof(I_TimerInterface));
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_LEDInterface), typeof(LEDMachine));
            PModule.interfaceDefinitionMap.Add(nameof(I_TimerInterface), typeof(TimerMachine));
            PModule.interfaceDefinitionMap.Add(nameof(I_SwitchInterface), typeof(SwitchMachine));
            PModule.interfaceDefinitionMap.Add(nameof(I_OSRDriverInterface), typeof(OSRDriverMachine));
        }

        public static void InitializeMonitorObserves()
        {
        }

        public static void InitializeMonitorMap(PSharpRuntime runtime)
        {
        }


        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.SetLogger(new PLogger());
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof(Main)));
        }
    }
    // TODO: NamedModule OSModule
    // TODO: NamedModule OSRDriverModule
    // TODO: NamedModule UserModule
    public class I_OSRDriverInterface : PMachineValue
    {
        public I_OSRDriverInterface(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_SwitchInterface : PMachineValue
    {
        public I_SwitchInterface(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_TimerInterface : PMachineValue
    {
        public I_TimerInterface(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_LEDInterface : PMachineValue
    {
        public I_LEDInterface(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_SwitchMachine : PMachineValue
    {
        public I_SwitchMachine(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_LEDMachine : PMachineValue
    {
        public I_LEDMachine(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_TimerMachine : PMachineValue
    {
        public I_TimerMachine(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_OSRDriverMachine : PMachineValue
    {
        public I_OSRDriverMachine(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_Main : PMachineValue
    {
        public I_Main(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.AddInterface(nameof(I_OSRDriverInterface), nameof(eD0Entry), nameof(eD0Exit), nameof(eOperationSuccess), nameof(eStoppingFailure), nameof(eStoppingSuccess), nameof(eSwitchStatusChange), nameof(eTimerFired), nameof(eTimerStopped), nameof(eTransferFailure), nameof(eTransferSuccess));
            PInterfaces.AddInterface(nameof(I_SwitchInterface), nameof(eYes));
            PInterfaces.AddInterface(nameof(I_TimerInterface), nameof(eStartDebounceTimer), nameof(eStopTimer));
            PInterfaces.AddInterface(nameof(I_LEDInterface), nameof(eSetLedStateToStableUsingControlTransfer), nameof(eSetLedStateToUnstableUsingControlTransfer), nameof(eUpdateBarGraphStateUsingControlTransfer));
            PInterfaces.AddInterface(nameof(I_SwitchMachine), nameof(eD0Entry), nameof(eD0Exit), nameof(eNo), nameof(eOperationFailure), nameof(eOperationSuccess), nameof(eSetLedStateToStableUsingControlTransfer), nameof(eSetLedStateToUnstableUsingControlTransfer), nameof(eStartDebounceTimer), nameof(eStopTimer), nameof(eStoppingFailure), nameof(eStoppingSuccess), nameof(eSwitchStatusChange), nameof(eTimerFired), nameof(eTimerStopped), nameof(eTransferFailure), nameof(eTransferSuccess), nameof(eUnit), nameof(eUpdateBarGraphStateUsingControlTransfer), nameof(eYes));
            PInterfaces.AddInterface(nameof(I_LEDMachine), nameof(eD0Entry), nameof(eD0Exit), nameof(eNo), nameof(eOperationFailure), nameof(eOperationSuccess), nameof(eSetLedStateToStableUsingControlTransfer), nameof(eSetLedStateToUnstableUsingControlTransfer), nameof(eStartDebounceTimer), nameof(eStopTimer), nameof(eStoppingFailure), nameof(eStoppingSuccess), nameof(eSwitchStatusChange), nameof(eTimerFired), nameof(eTimerStopped), nameof(eTransferFailure), nameof(eTransferSuccess), nameof(eUnit), nameof(eUpdateBarGraphStateUsingControlTransfer), nameof(eYes));
            PInterfaces.AddInterface(nameof(I_TimerMachine), nameof(eD0Entry), nameof(eD0Exit), nameof(eNo), nameof(eOperationFailure), nameof(eOperationSuccess), nameof(eSetLedStateToStableUsingControlTransfer), nameof(eSetLedStateToUnstableUsingControlTransfer), nameof(eStartDebounceTimer), nameof(eStopTimer), nameof(eStoppingFailure), nameof(eStoppingSuccess), nameof(eSwitchStatusChange), nameof(eTimerFired), nameof(eTimerStopped), nameof(eTransferFailure), nameof(eTransferSuccess), nameof(eUnit), nameof(eUpdateBarGraphStateUsingControlTransfer), nameof(eYes));
            PInterfaces.AddInterface(nameof(I_OSRDriverMachine), nameof(eD0Entry), nameof(eD0Exit), nameof(eNo), nameof(eOperationFailure), nameof(eOperationSuccess), nameof(eSetLedStateToStableUsingControlTransfer), nameof(eSetLedStateToUnstableUsingControlTransfer), nameof(eStartDebounceTimer), nameof(eStopTimer), nameof(eStoppingFailure), nameof(eStoppingSuccess), nameof(eSwitchStatusChange), nameof(eTimerFired), nameof(eTimerStopped), nameof(eTransferFailure), nameof(eTransferSuccess), nameof(eUnit), nameof(eUpdateBarGraphStateUsingControlTransfer), nameof(eYes));
            PInterfaces.AddInterface(nameof(I_Main), nameof(eD0Entry), nameof(eD0Exit), nameof(eNo), nameof(eOperationFailure), nameof(eOperationSuccess), nameof(eSetLedStateToStableUsingControlTransfer), nameof(eSetLedStateToUnstableUsingControlTransfer), nameof(eStartDebounceTimer), nameof(eStopTimer), nameof(eStoppingFailure), nameof(eStoppingSuccess), nameof(eSwitchStatusChange), nameof(eTimerFired), nameof(eTimerStopped), nameof(eTransferFailure), nameof(eTransferSuccess), nameof(eUnit), nameof(eUpdateBarGraphStateUsingControlTransfer), nameof(eYes));
        }
    }

}
