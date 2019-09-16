using Microsoft.PSharp;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Plang.PrtSharp;
using Plang.PrtSharp.Values;
using Plang.PrtSharp.Exceptions;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 162, 219, 414
namespace Main
{
    public static partial class GlobalFunctions {}
    internal partial class local_event : PEvent
    {
        public local_event() : base() {}
        public local_event (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new local_event();}
    }
    internal partial class ePrepare : PEvent
    {
        public ePrepare() : base() {}
        public ePrepare (PrtNamedTuple payload): base(payload){ }
        public override IPrtValue Clone() { return new ePrepare();}
    }
    internal partial class ePrepareSuccess : PEvent
    {
        public ePrepareSuccess() : base() {}
        public ePrepareSuccess (PrtInt payload): base(payload){ }
        public override IPrtValue Clone() { return new ePrepareSuccess();}
    }
    internal partial class ePrepareFailed : PEvent
    {
        public ePrepareFailed() : base() {}
        public ePrepareFailed (PrtInt payload): base(payload){ }
        public override IPrtValue Clone() { return new ePrepareFailed();}
    }
    internal partial class eGlobalAbort : PEvent
    {
        public eGlobalAbort() : base() {}
        public eGlobalAbort (PrtInt payload): base(payload){ }
        public override IPrtValue Clone() { return new eGlobalAbort();}
    }
    internal partial class eGlobalCommit : PEvent
    {
        public eGlobalCommit() : base() {}
        public eGlobalCommit (PrtInt payload): base(payload){ }
        public override IPrtValue Clone() { return new eGlobalCommit();}
    }
    internal partial class eWriteTransaction : PEvent
    {
        public eWriteTransaction() : base() {}
        public eWriteTransaction (PrtNamedTuple payload): base(payload){ }
        public override IPrtValue Clone() { return new eWriteTransaction();}
    }
    internal partial class eWriteTransFailed : PEvent
    {
        public eWriteTransFailed() : base() {}
        public eWriteTransFailed (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eWriteTransFailed();}
    }
    internal partial class eWriteTransSuccess : PEvent
    {
        public eWriteTransSuccess() : base() {}
        public eWriteTransSuccess (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eWriteTransSuccess();}
    }
    internal partial class eReadTransaction : PEvent
    {
        public eReadTransaction() : base() {}
        public eReadTransaction (PrtNamedTuple payload): base(payload){ }
        public override IPrtValue Clone() { return new eReadTransaction();}
    }
    internal partial class eReadTransFailed : PEvent
    {
        public eReadTransFailed() : base() {}
        public eReadTransFailed (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eReadTransFailed();}
    }
    internal partial class eReadTransSuccess : PEvent
    {
        public eReadTransSuccess() : base() {}
        public eReadTransSuccess (PrtInt payload): base(payload){ }
        public override IPrtValue Clone() { return new eReadTransSuccess();}
    }
    internal partial class eTimeOut : PEvent
    {
        public eTimeOut() : base() {}
        public eTimeOut (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eTimeOut();}
    }
    internal partial class eStartTimer : PEvent
    {
        public eStartTimer() : base() {}
        public eStartTimer (PrtInt payload): base(payload){ }
        public override IPrtValue Clone() { return new eStartTimer();}
    }
    internal partial class eCancelTimer : PEvent
    {
        public eCancelTimer() : base() {}
        public eCancelTimer (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eCancelTimer();}
    }
    internal partial class eCancelTimerFailed : PEvent
    {
        public eCancelTimerFailed() : base() {}
        public eCancelTimerFailed (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eCancelTimerFailed();}
    }
    internal partial class eCancelTimerSuccess : PEvent
    {
        public eCancelTimerSuccess() : base() {}
        public eCancelTimerSuccess (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eCancelTimerSuccess();}
    }
    internal partial class eMonitor_LocalCommit : PEvent
    {
        public eMonitor_LocalCommit() : base() {}
        public eMonitor_LocalCommit (PrtNamedTuple payload): base(payload){ }
        public override IPrtValue Clone() { return new eMonitor_LocalCommit();}
    }
    internal partial class eMonitor_AtomicityInitialize : PEvent
    {
        public eMonitor_AtomicityInitialize() : base() {}
        public eMonitor_AtomicityInitialize (PrtInt payload): base(payload){ }
        public override IPrtValue Clone() { return new eMonitor_AtomicityInitialize();}
    }
    public static partial class GlobalFunctions
    {
    }
    public static partial class GlobalFunctions
    {
        public static PMachineValue CreateTimer(PMachineValue client, PMachine currentMachine)
        {
            PMachineValue TMP_tmp0 = null;
            PMachineValue TMP_tmp1 = null;
            TMP_tmp0 = (PMachineValue)(((PMachineValue)((IPrtValue)client)?.Clone()));
            TMP_tmp1 = (PMachineValue)(currentMachine.CreateInterface<I_Timer>( currentMachine, TMP_tmp0));
            return ((PMachineValue)((IPrtValue)TMP_tmp1)?.Clone());
        }
    }
    public static partial class GlobalFunctions
    {
        public static void StartTimer(PMachineValue timer, PrtInt value, PMachine currentMachine)
        {
            PMachineValue TMP_tmp0_1 = null;
            PEvent TMP_tmp1_1 = null;
            PrtInt TMP_tmp2 = ((PrtInt)0);
            TMP_tmp0_1 = (PMachineValue)(((PMachineValue)((IPrtValue)timer)?.Clone()));
            TMP_tmp1_1 = (PEvent)(new eStartTimer(((PrtInt)0)));
            TMP_tmp2 = (PrtInt)(((PrtInt)((IPrtValue)value)?.Clone()));
            currentMachine.SendEvent(TMP_tmp0_1, (Event)TMP_tmp1_1, TMP_tmp2);
        }
    }
    public static partial class GlobalFunctions
    {
        public static async Task CancelTimer(PMachineValue timer_1, PMachine currentMachine)
        {
            PMachineValue TMP_tmp0_2 = null;
            PEvent TMP_tmp1_2 = null;
            TMP_tmp0_2 = (PMachineValue)(((PMachineValue)((IPrtValue)timer_1)?.Clone()));
            TMP_tmp1_2 = (PEvent)(new eCancelTimer(null));
            currentMachine.SendEvent(TMP_tmp0_2, (Event)TMP_tmp1_2);
            var PGEN_recvEvent = await currentMachine.ReceiveEvent(typeof(eCancelTimerSuccess), typeof(eCancelTimerFailed));
            switch (PGEN_recvEvent) {
                case eCancelTimerSuccess PGEN_evt: {
                    PModule.runtime.Logger.WriteLine("Timer Cancelled Successful");
                } break;
                case eCancelTimerFailed PGEN_evt_1: {
                    var PGEN_recvEvent_1 = await currentMachine.ReceiveEvent(typeof(eTimeOut));
                    switch (PGEN_recvEvent_1) {
                        case eTimeOut PGEN_evt_2: {
                            PModule.runtime.Logger.WriteLine("Timer Cancelled Successful");
                        } break;
                    }
                } break;
            }
        }
    }
    internal partial class Client : PMachine
    {
        private PMachineValue coordinator = null;
        private PrtNamedTuple randomTransaction = (new PrtNamedTuple(new string[]{"client","key","val"},null, ((PrtInt)0), ((PrtInt)0)));
        public class ConstructorEvent : PEvent{public ConstructorEvent(PMachineValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PMachineValue)value); }
        public Client() {
            this.sends.Add(nameof(eCancelTimer));
            this.sends.Add(nameof(eCancelTimerFailed));
            this.sends.Add(nameof(eCancelTimerSuccess));
            this.sends.Add(nameof(eGlobalAbort));
            this.sends.Add(nameof(eGlobalCommit));
            this.sends.Add(nameof(eMonitor_AtomicityInitialize));
            this.sends.Add(nameof(eMonitor_LocalCommit));
            this.sends.Add(nameof(ePrepare));
            this.sends.Add(nameof(ePrepareFailed));
            this.sends.Add(nameof(ePrepareSuccess));
            this.sends.Add(nameof(eReadTransFailed));
            this.sends.Add(nameof(eReadTransSuccess));
            this.sends.Add(nameof(eReadTransaction));
            this.sends.Add(nameof(eStartTimer));
            this.sends.Add(nameof(eTimeOut));
            this.sends.Add(nameof(eWriteTransFailed));
            this.sends.Add(nameof(eWriteTransSuccess));
            this.sends.Add(nameof(eWriteTransaction));
            this.sends.Add(nameof(PHalt));
            this.sends.Add(nameof(local_event));
            this.receives.Add(nameof(eCancelTimer));
            this.receives.Add(nameof(eCancelTimerFailed));
            this.receives.Add(nameof(eCancelTimerSuccess));
            this.receives.Add(nameof(eGlobalAbort));
            this.receives.Add(nameof(eGlobalCommit));
            this.receives.Add(nameof(eMonitor_AtomicityInitialize));
            this.receives.Add(nameof(eMonitor_LocalCommit));
            this.receives.Add(nameof(ePrepare));
            this.receives.Add(nameof(ePrepareFailed));
            this.receives.Add(nameof(ePrepareSuccess));
            this.receives.Add(nameof(eReadTransFailed));
            this.receives.Add(nameof(eReadTransSuccess));
            this.receives.Add(nameof(eReadTransaction));
            this.receives.Add(nameof(eStartTimer));
            this.receives.Add(nameof(eTimeOut));
            this.receives.Add(nameof(eWriteTransFailed));
            this.receives.Add(nameof(eWriteTransSuccess));
            this.receives.Add(nameof(eWriteTransaction));
            this.receives.Add(nameof(PHalt));
            this.receives.Add(nameof(local_event));
        }
        
        public void Anon()
        {
            Client currentMachine = this;
            PMachineValue payload = (PMachineValue)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            coordinator = (PMachineValue)(((PMachineValue)((IPrtValue)payload)?.Clone()));
            currentMachine.GotoState<Client.StartPumpingTransactions>();
            throw new PUnreachableCodeException();
        }
        public void Anon_1()
        {
            Client currentMachine = this;
            PrtNamedTuple TMP_tmp0_3 = (new PrtNamedTuple(new string[]{"client","key","val"},null, ((PrtInt)0), ((PrtInt)0)));
            PMachineValue TMP_tmp1_3 = null;
            PEvent TMP_tmp2_1 = null;
            PrtNamedTuple TMP_tmp3 = (new PrtNamedTuple(new string[]{"client","key","val"},null, ((PrtInt)0), ((PrtInt)0)));
            TMP_tmp0_3 = (PrtNamedTuple)(GlobalFunctions.ChooseTransaction(this));
            randomTransaction = TMP_tmp0_3;
            TMP_tmp1_3 = (PMachineValue)(((PMachineValue)((IPrtValue)coordinator)?.Clone()));
            TMP_tmp2_1 = (PEvent)(new eWriteTransaction((new PrtNamedTuple(new string[]{"client","key","val"},null, ((PrtInt)0), ((PrtInt)0)))));
            TMP_tmp3 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)randomTransaction)?.Clone()));
            currentMachine.SendEvent(TMP_tmp1_3, (Event)TMP_tmp2_1, TMP_tmp3);
        }
        public void Anon_2()
        {
            Client currentMachine = this;
            PMachineValue TMP_tmp0_4 = null;
            PEvent TMP_tmp1_4 = null;
            PMachineValue TMP_tmp2_2 = null;
            PrtInt TMP_tmp3_1 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp4 = (new PrtNamedTuple(new string[]{"client","key"},null, ((PrtInt)0)));
            TMP_tmp0_4 = (PMachineValue)(((PMachineValue)((IPrtValue)coordinator)?.Clone()));
            TMP_tmp1_4 = (PEvent)(new eReadTransaction((new PrtNamedTuple(new string[]{"client","key"},null, ((PrtInt)0)))));
            TMP_tmp2_2 = (PMachineValue)(currentMachine.self);
            TMP_tmp3_1 = (PrtInt)(((PrtNamedTuple)randomTransaction)["key"]);
            TMP_tmp4 = (PrtNamedTuple)((new PrtNamedTuple(new string[]{"client","key"}, TMP_tmp2_2, TMP_tmp3_1)));
            currentMachine.SendEvent(TMP_tmp0_4, (Event)TMP_tmp1_4, TMP_tmp4);
        }
        public void Anon_3()
        {
            Client currentMachine = this;
            currentMachine.Assert(((PrtBool)false),"Read Failed after Write!!");
            throw new PUnreachableCodeException();
        }
        public void Anon_4()
        {
            Client currentMachine = this;
            PrtInt payload_1 = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt TMP_tmp0_5 = ((PrtInt)0);
            PrtBool TMP_tmp1_5 = ((PrtBool)false);
            TMP_tmp0_5 = (PrtInt)(((PrtNamedTuple)randomTransaction)["val"]);
            TMP_tmp1_5 = (PrtBool)((PrtValues.SafeEquals(payload_1,TMP_tmp0_5)));
            currentMachine.Assert(TMP_tmp1_5,"Incorrect value returned !!");
        }
        public void Anon_5()
        {
            Client currentMachine = this;
            PEvent TMP_tmp0_6 = null;
            TMP_tmp0_6 = (PEvent)(new PHalt(null));
            currentMachine.RaiseEvent((Event)TMP_tmp0_6);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon))]
        class Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_1))]
        [OnEventGotoState(typeof(eWriteTransFailed), typeof(End))]
        [OnEventGotoState(typeof(eWriteTransSuccess), typeof(ConfirmTransaction))]
        class StartPumpingTransactions : MachineState
        {
        }
        [OnEntry(nameof(Anon_2))]
        [OnEventDoAction(typeof(eReadTransFailed), nameof(Anon_3))]
        [OnEventGotoState(typeof(eReadTransSuccess), typeof(End), nameof(Anon_4))]
        class ConfirmTransaction : MachineState
        {
        }
        [OnEntry(nameof(Anon_5))]
        class End : MachineState
        {
        }
    }
    internal partial class Coordinator : PMachine
    {
        private PrtSeq participants = new PrtSeq();
        private PrtNamedTuple pendingWrTrans = (new PrtNamedTuple(new string[]{"client","key","val"},null, ((PrtInt)0), ((PrtInt)0)));
        private PrtInt currTransId = ((PrtInt)0);
        private PMachineValue timer_2 = null;
        private PrtInt countPrepareResponses = ((PrtInt)0);
        public class ConstructorEvent : PEvent{public ConstructorEvent(PrtSeq val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PrtSeq)value); }
        public Coordinator() {
            this.sends.Add(nameof(eCancelTimer));
            this.sends.Add(nameof(eCancelTimerFailed));
            this.sends.Add(nameof(eCancelTimerSuccess));
            this.sends.Add(nameof(eGlobalAbort));
            this.sends.Add(nameof(eGlobalCommit));
            this.sends.Add(nameof(eMonitor_AtomicityInitialize));
            this.sends.Add(nameof(eMonitor_LocalCommit));
            this.sends.Add(nameof(ePrepare));
            this.sends.Add(nameof(ePrepareFailed));
            this.sends.Add(nameof(ePrepareSuccess));
            this.sends.Add(nameof(eReadTransFailed));
            this.sends.Add(nameof(eReadTransSuccess));
            this.sends.Add(nameof(eReadTransaction));
            this.sends.Add(nameof(eStartTimer));
            this.sends.Add(nameof(eTimeOut));
            this.sends.Add(nameof(eWriteTransFailed));
            this.sends.Add(nameof(eWriteTransSuccess));
            this.sends.Add(nameof(eWriteTransaction));
            this.sends.Add(nameof(PHalt));
            this.sends.Add(nameof(local_event));
            this.receives.Add(nameof(eCancelTimer));
            this.receives.Add(nameof(eCancelTimerFailed));
            this.receives.Add(nameof(eCancelTimerSuccess));
            this.receives.Add(nameof(eGlobalAbort));
            this.receives.Add(nameof(eGlobalCommit));
            this.receives.Add(nameof(eMonitor_AtomicityInitialize));
            this.receives.Add(nameof(eMonitor_LocalCommit));
            this.receives.Add(nameof(ePrepare));
            this.receives.Add(nameof(ePrepareFailed));
            this.receives.Add(nameof(ePrepareSuccess));
            this.receives.Add(nameof(eReadTransFailed));
            this.receives.Add(nameof(eReadTransSuccess));
            this.receives.Add(nameof(eReadTransaction));
            this.receives.Add(nameof(eStartTimer));
            this.receives.Add(nameof(eTimeOut));
            this.receives.Add(nameof(eWriteTransFailed));
            this.receives.Add(nameof(eWriteTransSuccess));
            this.receives.Add(nameof(eWriteTransaction));
            this.receives.Add(nameof(PHalt));
            this.receives.Add(nameof(local_event));
            this.creates.Add(nameof(I_Timer));
        }
        
        public void Anon_6()
        {
            Coordinator currentMachine = this;
            PrtSeq payload_2 = (PrtSeq)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt i = ((PrtInt)0);
            PMachineValue TMP_tmp0_7 = null;
            PMachineValue TMP_tmp1_6 = null;
            PrtInt TMP_tmp2_3 = ((PrtInt)0);
            participants = (PrtSeq)(((PrtSeq)((IPrtValue)payload_2)?.Clone()));
            i = (PrtInt)(((PrtInt)0));
            currTransId = (PrtInt)(((PrtInt)0));
            TMP_tmp0_7 = (PMachineValue)(currentMachine.self);
            TMP_tmp1_6 = (PMachineValue)(GlobalFunctions.CreateTimer(TMP_tmp0_7, this));
            timer_2 = TMP_tmp1_6;
            TMP_tmp2_3 = (PrtInt)(((PrtInt)(payload_2).Count));
            currentMachine.Announce((Event)new eMonitor_AtomicityInitialize(((PrtInt)0)), TMP_tmp2_3);
            currentMachine.GotoState<Coordinator.WaitForTransactions>();
            throw new PUnreachableCodeException();
        }
        public void Anon_7()
        {
            Coordinator currentMachine = this;
            PrtNamedTuple wTrans = (PrtNamedTuple)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt TMP_tmp0_8 = ((PrtInt)0);
            PEvent TMP_tmp1_7 = null;
            PMachineValue TMP_tmp2_4 = null;
            PrtInt TMP_tmp3_2 = ((PrtInt)0);
            PrtInt TMP_tmp4_1 = ((PrtInt)0);
            PrtInt TMP_tmp5 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp6 = (new PrtNamedTuple(new string[]{"coordinator","transId","key","val"},null, ((PrtInt)0), ((PrtInt)0), ((PrtInt)0)));
            PMachineValue TMP_tmp7 = null;
            PrtInt TMP_tmp8 = ((PrtInt)0);
            PEvent TMP_tmp9 = null;
            pendingWrTrans = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)wTrans)?.Clone()));
            TMP_tmp0_8 = (PrtInt)((currTransId) + (((PrtInt)1)));
            currTransId = TMP_tmp0_8;
            TMP_tmp1_7 = (PEvent)(new ePrepare((new PrtNamedTuple(new string[]{"coordinator","transId","key","val"},null, ((PrtInt)0), ((PrtInt)0), ((PrtInt)0)))));
            TMP_tmp2_4 = (PMachineValue)(currentMachine.self);
            TMP_tmp3_2 = (PrtInt)(((PrtInt)((IPrtValue)currTransId)?.Clone()));
            TMP_tmp4_1 = (PrtInt)(((PrtNamedTuple)pendingWrTrans)["key"]);
            TMP_tmp5 = (PrtInt)(((PrtNamedTuple)pendingWrTrans)["val"]);
            TMP_tmp6 = (PrtNamedTuple)((new PrtNamedTuple(new string[]{"coordinator","transId","key","val"}, TMP_tmp2_4, TMP_tmp3_2, TMP_tmp4_1, TMP_tmp5)));
            SendToAllParticipants(TMP_tmp1_7, TMP_tmp6);
            TMP_tmp7 = (PMachineValue)(((PMachineValue)((IPrtValue)timer_2)?.Clone()));
            TMP_tmp8 = (PrtInt)(((PrtInt)100));
            GlobalFunctions.StartTimer(TMP_tmp7, TMP_tmp8, this);
            TMP_tmp9 = (PEvent)(new local_event(null));
            currentMachine.RaiseEvent((Event)TMP_tmp9);
            throw new PUnreachableCodeException();
        }
        public void Anon_8()
        {
            Coordinator currentMachine = this;
            PrtNamedTuple rTrans = (PrtNamedTuple)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtBool TMP_tmp0_9 = ((PrtBool)false);
            PMachineValue TMP_tmp1_8 = null;
            PMachineValue TMP_tmp2_5 = null;
            PEvent TMP_tmp3_3 = null;
            PrtNamedTuple TMP_tmp4_2 = (new PrtNamedTuple(new string[]{"client","key"},null, ((PrtInt)0)));
            PrtInt TMP_tmp5_1 = ((PrtInt)0);
            PrtInt TMP_tmp6_1 = ((PrtInt)0);
            PMachineValue TMP_tmp7_1 = null;
            PMachineValue TMP_tmp8_1 = null;
            PEvent TMP_tmp9_1 = null;
            PrtNamedTuple TMP_tmp10 = (new PrtNamedTuple(new string[]{"client","key"},null, ((PrtInt)0)));
            TMP_tmp0_9 = (PrtBool)(((PrtBool)currentMachine.Random()));
            if (TMP_tmp0_9)
            {
                TMP_tmp1_8 = (PMachineValue)(((PrtSeq)participants)[((PrtInt)0)]);
                TMP_tmp2_5 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp1_8)?.Clone()));
                TMP_tmp3_3 = (PEvent)(new eReadTransaction((new PrtNamedTuple(new string[]{"client","key"},null, ((PrtInt)0)))));
                TMP_tmp4_2 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)rTrans)?.Clone()));
                currentMachine.SendEvent(TMP_tmp2_5, (Event)TMP_tmp3_3, TMP_tmp4_2);
            }
            else
            {
                TMP_tmp5_1 = (PrtInt)(((PrtInt)(participants).Count));
                TMP_tmp6_1 = (PrtInt)((TMP_tmp5_1) - (((PrtInt)1)));
                TMP_tmp7_1 = (PMachineValue)(((PrtSeq)participants)[TMP_tmp6_1]);
                TMP_tmp8_1 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp7_1)?.Clone()));
                TMP_tmp9_1 = (PEvent)(new eReadTransaction((new PrtNamedTuple(new string[]{"client","key"},null, ((PrtInt)0)))));
                TMP_tmp10 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)rTrans)?.Clone()));
                currentMachine.SendEvent(TMP_tmp8_1, (Event)TMP_tmp9_1, TMP_tmp10);
            }
        }
        public void Anon_9()
        {
            Coordinator currentMachine = this;
            countPrepareResponses = (PrtInt)(((PrtInt)0));
        }
        public async Task Anon_10()
        {
            Coordinator currentMachine = this;
            PrtInt transId = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtBool TMP_tmp0_10 = ((PrtBool)false);
            PrtInt TMP_tmp1_9 = ((PrtInt)0);
            PrtInt TMP_tmp2_6 = ((PrtInt)0);
            PrtBool TMP_tmp3_4 = ((PrtBool)false);
            PEvent TMP_tmp4_3 = null;
            PrtInt TMP_tmp5_2 = ((PrtInt)0);
            PMachineValue TMP_tmp6_2 = null;
            PMachineValue TMP_tmp7_2 = null;
            PEvent TMP_tmp8_2 = null;
            PMachineValue TMP_tmp9_2 = null;
            TMP_tmp0_10 = (PrtBool)((PrtValues.SafeEquals(currTransId,transId)));
            if (TMP_tmp0_10)
            {
                TMP_tmp1_9 = (PrtInt)((countPrepareResponses) + (((PrtInt)1)));
                countPrepareResponses = TMP_tmp1_9;
                TMP_tmp2_6 = (PrtInt)(((PrtInt)(participants).Count));
                TMP_tmp3_4 = (PrtBool)((PrtValues.SafeEquals(countPrepareResponses,TMP_tmp2_6)));
                if (TMP_tmp3_4)
                {
                    TMP_tmp4_3 = (PEvent)(new eGlobalCommit(((PrtInt)0)));
                    TMP_tmp5_2 = (PrtInt)(((PrtInt)((IPrtValue)currTransId)?.Clone()));
                    SendToAllParticipants(TMP_tmp4_3, TMP_tmp5_2);
                    TMP_tmp6_2 = (PMachineValue)(((PrtNamedTuple)pendingWrTrans)["client"]);
                    TMP_tmp7_2 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp6_2)?.Clone()));
                    TMP_tmp8_2 = (PEvent)(new eWriteTransSuccess(null));
                    currentMachine.SendEvent(TMP_tmp7_2, (Event)TMP_tmp8_2);
                    TMP_tmp9_2 = (PMachineValue)(((PMachineValue)((IPrtValue)timer_2)?.Clone()));
                    await GlobalFunctions.CancelTimer(TMP_tmp9_2, this);
                    currentMachine.PopState();
                    throw new PUnreachableCodeException();
                }
            }
        }
        public async Task Anon_11()
        {
            Coordinator currentMachine = this;
            PrtInt transId_1 = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtBool TMP_tmp0_11 = ((PrtBool)false);
            PMachineValue TMP_tmp1_10 = null;
            TMP_tmp0_11 = (PrtBool)((PrtValues.SafeEquals(currTransId,transId_1)));
            if (TMP_tmp0_11)
            {
                DoGlobalAbort();
                TMP_tmp1_10 = (PMachineValue)(((PMachineValue)((IPrtValue)timer_2)?.Clone()));
                await GlobalFunctions.CancelTimer(TMP_tmp1_10, this);
                currentMachine.PopState();
                throw new PUnreachableCodeException();
            }
        }
        public void Anon_12()
        {
            Coordinator currentMachine = this;
            DoGlobalAbort();
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        public void Anon_13()
        {
            Coordinator currentMachine = this;
            PModule.runtime.Logger.WriteLine("Going back to WaitForTransactions");
        }
        public void DoGlobalAbort()
        {
            Coordinator currentMachine = this;
            PEvent TMP_tmp0_12 = null;
            PrtInt TMP_tmp1_11 = ((PrtInt)0);
            PMachineValue TMP_tmp2_7 = null;
            PMachineValue TMP_tmp3_5 = null;
            PEvent TMP_tmp4_4 = null;
            TMP_tmp0_12 = (PEvent)(new eGlobalAbort(((PrtInt)0)));
            TMP_tmp1_11 = (PrtInt)(((PrtInt)((IPrtValue)currTransId)?.Clone()));
            SendToAllParticipants(TMP_tmp0_12, TMP_tmp1_11);
            TMP_tmp2_7 = (PMachineValue)(((PrtNamedTuple)pendingWrTrans)["client"]);
            TMP_tmp3_5 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp2_7)?.Clone()));
            TMP_tmp4_4 = (PEvent)(new eWriteTransFailed(null));
            currentMachine.SendEvent(TMP_tmp3_5, (Event)TMP_tmp4_4);
        }
        public void SendToAllParticipants(PEvent message, IPrtValue payload_3)
        {
            Coordinator currentMachine = this;
            PrtInt i_1 = ((PrtInt)0);
            PrtInt TMP_tmp0_13 = ((PrtInt)0);
            PrtBool TMP_tmp1_12 = ((PrtBool)false);
            PrtBool TMP_tmp2_8 = ((PrtBool)false);
            PMachineValue TMP_tmp3_6 = null;
            PMachineValue TMP_tmp4_5 = null;
            PEvent TMP_tmp5_3 = null;
            IPrtValue TMP_tmp6_3 = null;
            PrtInt TMP_tmp7_3 = ((PrtInt)0);
            i_1 = (PrtInt)(((PrtInt)0));
            TMP_tmp0_13 = (PrtInt)(((PrtInt)(participants).Count));
            TMP_tmp1_12 = (PrtBool)((i_1) < (TMP_tmp0_13));
            TMP_tmp2_8 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp1_12)?.Clone()));
            while (TMP_tmp2_8)
            {
                TMP_tmp3_6 = (PMachineValue)(((PrtSeq)participants)[i_1]);
                TMP_tmp4_5 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp3_6)?.Clone()));
                TMP_tmp5_3 = (PEvent)(((PEvent)((IPrtValue)message)?.Clone()));
                TMP_tmp6_3 = (IPrtValue)(((IPrtValue)((IPrtValue)payload_3)?.Clone()));
                currentMachine.SendEvent(TMP_tmp4_5, (Event)TMP_tmp5_3, TMP_tmp6_3);
                TMP_tmp7_3 = (PrtInt)((i_1) + (((PrtInt)1)));
                i_1 = TMP_tmp7_3;
                TMP_tmp0_13 = (PrtInt)(((PrtInt)(participants).Count));
                TMP_tmp1_12 = (PrtBool)((i_1) < (TMP_tmp0_13));
                TMP_tmp2_8 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp1_12)?.Clone()));
            }
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_1))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_6))]
        class Init_1 : MachineState
        {
        }
        [OnEventDoAction(typeof(eWriteTransaction), nameof(Anon_7))]
        [OnEventDoAction(typeof(eReadTransaction), nameof(Anon_8))]
        [OnEventPushState(typeof(local_event), typeof(WaitForPrepareResponses))]
        [IgnoreEvents(typeof(ePrepareSuccess), typeof(ePrepareFailed))]
        class WaitForTransactions : MachineState
        {
        }
        [OnEntry(nameof(Anon_9))]
        [OnEventDoAction(typeof(ePrepareSuccess), nameof(Anon_10))]
        [OnEventDoAction(typeof(ePrepareFailed), nameof(Anon_11))]
        [OnEventDoAction(typeof(eTimeOut), nameof(Anon_12))]
        [DeferEvents(typeof(eWriteTransaction))]
        [OnExit(nameof(Anon_13))]
        class WaitForPrepareResponses : MachineState
        {
        }
    }
    internal partial class Participant : PMachine
    {
        private PrtMap kvStore = new PrtMap();
        private PrtNamedTuple pendingWrTrans_1 = (new PrtNamedTuple(new string[]{"coordinator","transId","key","val"},null, ((PrtInt)0), ((PrtInt)0), ((PrtInt)0)));
        private PrtInt lastTransId = ((PrtInt)0);
        public class ConstructorEvent : PEvent{public ConstructorEvent(IPrtValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Participant() {
            this.sends.Add(nameof(eCancelTimer));
            this.sends.Add(nameof(eCancelTimerFailed));
            this.sends.Add(nameof(eCancelTimerSuccess));
            this.sends.Add(nameof(eGlobalAbort));
            this.sends.Add(nameof(eGlobalCommit));
            this.sends.Add(nameof(eMonitor_AtomicityInitialize));
            this.sends.Add(nameof(eMonitor_LocalCommit));
            this.sends.Add(nameof(ePrepare));
            this.sends.Add(nameof(ePrepareFailed));
            this.sends.Add(nameof(ePrepareSuccess));
            this.sends.Add(nameof(eReadTransFailed));
            this.sends.Add(nameof(eReadTransSuccess));
            this.sends.Add(nameof(eReadTransaction));
            this.sends.Add(nameof(eStartTimer));
            this.sends.Add(nameof(eTimeOut));
            this.sends.Add(nameof(eWriteTransFailed));
            this.sends.Add(nameof(eWriteTransSuccess));
            this.sends.Add(nameof(eWriteTransaction));
            this.sends.Add(nameof(PHalt));
            this.sends.Add(nameof(local_event));
            this.receives.Add(nameof(eCancelTimer));
            this.receives.Add(nameof(eCancelTimerFailed));
            this.receives.Add(nameof(eCancelTimerSuccess));
            this.receives.Add(nameof(eGlobalAbort));
            this.receives.Add(nameof(eGlobalCommit));
            this.receives.Add(nameof(eMonitor_AtomicityInitialize));
            this.receives.Add(nameof(eMonitor_LocalCommit));
            this.receives.Add(nameof(ePrepare));
            this.receives.Add(nameof(ePrepareFailed));
            this.receives.Add(nameof(ePrepareSuccess));
            this.receives.Add(nameof(eReadTransFailed));
            this.receives.Add(nameof(eReadTransSuccess));
            this.receives.Add(nameof(eReadTransaction));
            this.receives.Add(nameof(eStartTimer));
            this.receives.Add(nameof(eTimeOut));
            this.receives.Add(nameof(eWriteTransFailed));
            this.receives.Add(nameof(eWriteTransSuccess));
            this.receives.Add(nameof(eWriteTransaction));
            this.receives.Add(nameof(PHalt));
            this.receives.Add(nameof(local_event));
        }
        
        public void Anon_14()
        {
            Participant currentMachine = this;
            lastTransId = (PrtInt)(((PrtInt)0));
            currentMachine.GotoState<Participant.WaitForRequests>();
            throw new PUnreachableCodeException();
        }
        public void Anon_15()
        {
            Participant currentMachine = this;
            PrtInt transId_2 = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt TMP_tmp0_14 = ((PrtInt)0);
            PrtBool TMP_tmp1_13 = ((PrtBool)false);
            PrtInt TMP_tmp2_9 = ((PrtInt)0);
            PrtBool TMP_tmp3_7 = ((PrtBool)false);
            TMP_tmp0_14 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
            TMP_tmp1_13 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_14,transId_2)));
            currentMachine.Assert(TMP_tmp1_13,"");
            TMP_tmp2_9 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
            TMP_tmp3_7 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp2_9,transId_2)));
            if (TMP_tmp3_7)
            {
                lastTransId = (PrtInt)(((PrtInt)((IPrtValue)transId_2)?.Clone()));
            }
        }
        public void Anon_16()
        {
            Participant currentMachine = this;
            PrtInt transId_3 = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt TMP_tmp0_15 = ((PrtInt)0);
            PrtBool TMP_tmp1_14 = ((PrtBool)false);
            PrtInt TMP_tmp2_10 = ((PrtInt)0);
            PrtBool TMP_tmp3_8 = ((PrtBool)false);
            PrtInt TMP_tmp4_6 = ((PrtInt)0);
            PrtInt TMP_tmp5_4 = ((PrtInt)0);
            TMP_tmp0_15 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
            TMP_tmp1_14 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_15,transId_3)));
            currentMachine.Assert(TMP_tmp1_14,"");
            TMP_tmp2_10 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
            TMP_tmp3_8 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp2_10,transId_3)));
            if (TMP_tmp3_8)
            {
                TMP_tmp4_6 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["key"]);
                TMP_tmp5_4 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["val"]);
                ((PrtMap)kvStore)[TMP_tmp4_6] = TMP_tmp5_4;
                lastTransId = (PrtInt)(((PrtInt)((IPrtValue)transId_3)?.Clone()));
            }
        }
        public void Anon_17()
        {
            Participant currentMachine = this;
            PrtNamedTuple prepareReq = (PrtNamedTuple)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt TMP_tmp0_16 = ((PrtInt)0);
            PrtBool TMP_tmp1_15 = ((PrtBool)false);
            PrtBool TMP_tmp2_11 = ((PrtBool)false);
            PMachineValue TMP_tmp3_9 = null;
            PrtInt TMP_tmp4_7 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp5_5 = (new PrtNamedTuple(new string[]{"participant","transId"},null, ((PrtInt)0)));
            PMachineValue TMP_tmp6_4 = null;
            PMachineValue TMP_tmp7_4 = null;
            PEvent TMP_tmp8_3 = null;
            PrtInt TMP_tmp9_3 = ((PrtInt)0);
            PMachineValue TMP_tmp10_1 = null;
            PMachineValue TMP_tmp11 = null;
            PEvent TMP_tmp12 = null;
            PrtInt TMP_tmp13 = ((PrtInt)0);
            pendingWrTrans_1 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)prepareReq)?.Clone()));
            TMP_tmp0_16 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
            TMP_tmp1_15 = (PrtBool)((TMP_tmp0_16) > (lastTransId));
            currentMachine.Assert(TMP_tmp1_15,"");
            TMP_tmp2_11 = (PrtBool)(((PrtBool)currentMachine.Random()));
            if (TMP_tmp2_11)
            {
                TMP_tmp3_9 = (PMachineValue)(currentMachine.self);
                TMP_tmp4_7 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
                TMP_tmp5_5 = (PrtNamedTuple)((new PrtNamedTuple(new string[]{"participant","transId"}, TMP_tmp3_9, TMP_tmp4_7)));
                currentMachine.Announce((Event)new eMonitor_LocalCommit((new PrtNamedTuple(new string[]{"participant","transId"},null, ((PrtInt)0)))), TMP_tmp5_5);
                TMP_tmp6_4 = (PMachineValue)(((PrtNamedTuple)prepareReq)["coordinator"]);
                TMP_tmp7_4 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp6_4)?.Clone()));
                TMP_tmp8_3 = (PEvent)(new ePrepareSuccess(((PrtInt)0)));
                TMP_tmp9_3 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
                currentMachine.SendEvent(TMP_tmp7_4, (Event)TMP_tmp8_3, TMP_tmp9_3);
            }
            else
            {
                TMP_tmp10_1 = (PMachineValue)(((PrtNamedTuple)prepareReq)["coordinator"]);
                TMP_tmp11 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp10_1)?.Clone()));
                TMP_tmp12 = (PEvent)(new ePrepareFailed(((PrtInt)0)));
                TMP_tmp13 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
                currentMachine.SendEvent(TMP_tmp11, (Event)TMP_tmp12, TMP_tmp13);
            }
        }
        public void Anon_18()
        {
            Participant currentMachine = this;
            PrtNamedTuple payload_4 = (PrtNamedTuple)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt TMP_tmp0_17 = ((PrtInt)0);
            PrtBool TMP_tmp1_16 = ((PrtBool)false);
            PMachineValue TMP_tmp2_12 = null;
            PMachineValue TMP_tmp3_10 = null;
            PEvent TMP_tmp4_8 = null;
            PrtInt TMP_tmp5_6 = ((PrtInt)0);
            PrtInt TMP_tmp6_5 = ((PrtInt)0);
            PMachineValue TMP_tmp7_5 = null;
            PMachineValue TMP_tmp8_4 = null;
            PEvent TMP_tmp9_4 = null;
            TMP_tmp0_17 = (PrtInt)(((PrtNamedTuple)payload_4)["key"]);
            TMP_tmp1_16 = (PrtBool)(((PrtBool)(((PrtMap)kvStore).ContainsKey(TMP_tmp0_17))));
            if (TMP_tmp1_16)
            {
                TMP_tmp2_12 = (PMachineValue)(((PrtNamedTuple)payload_4)["client"]);
                TMP_tmp3_10 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp2_12)?.Clone()));
                TMP_tmp4_8 = (PEvent)(new eReadTransSuccess(((PrtInt)0)));
                TMP_tmp5_6 = (PrtInt)(((PrtNamedTuple)payload_4)["key"]);
                TMP_tmp6_5 = (PrtInt)(((PrtMap)kvStore)[TMP_tmp5_6]);
                currentMachine.SendEvent(TMP_tmp3_10, (Event)TMP_tmp4_8, TMP_tmp6_5);
            }
            else
            {
                TMP_tmp7_5 = (PMachineValue)(((PrtNamedTuple)payload_4)["client"]);
                TMP_tmp8_4 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp7_5)?.Clone()));
                TMP_tmp9_4 = (PEvent)(new eReadTransFailed(null));
                currentMachine.SendEvent(TMP_tmp8_4, (Event)TMP_tmp9_4);
            }
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_2))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_14))]
        class Init_2 : MachineState
        {
        }
        [OnEventDoAction(typeof(eGlobalAbort), nameof(Anon_15))]
        [OnEventDoAction(typeof(eGlobalCommit), nameof(Anon_16))]
        [OnEventDoAction(typeof(ePrepare), nameof(Anon_17))]
        [OnEventDoAction(typeof(eReadTransaction), nameof(Anon_18))]
        class WaitForRequests : MachineState
        {
        }
    }
    internal partial class Atomicity : PMonitor
    {
        private PrtMap receivedLocalCommits = new PrtMap();
        private PrtInt numParticipants = ((PrtInt)0);
        static Atomicity() {
            observes.Add(nameof(eMonitor_AtomicityInitialize));
            observes.Add(nameof(eMonitor_LocalCommit));
            observes.Add(nameof(eWriteTransFailed));
            observes.Add(nameof(eWriteTransSuccess));
        }
        
        public void Anon_19()
        {
            Atomicity currentMachine = this;
            PrtInt n = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            numParticipants = (PrtInt)(((PrtInt)((IPrtValue)n)?.Clone()));
        }
        public void Anon_20()
        {
            Atomicity currentMachine = this;
            PrtNamedTuple payload_5 = (PrtNamedTuple)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PMachineValue TMP_tmp0_18 = null;
            PrtBool TMP_tmp1_17 = ((PrtBool)false);
            PrtBool TMP_tmp2_13 = ((PrtBool)false);
            PMachineValue TMP_tmp3_11 = null;
            PrtInt TMP_tmp4_9 = ((PrtInt)0);
            TMP_tmp0_18 = (PMachineValue)(((PrtNamedTuple)payload_5)["participant"]);
            TMP_tmp1_17 = (PrtBool)(((PrtBool)(((PrtMap)receivedLocalCommits).ContainsKey(TMP_tmp0_18))));
            TMP_tmp2_13 = (PrtBool)(!(TMP_tmp1_17));
            currentMachine.Assert(TMP_tmp2_13,"");
            TMP_tmp3_11 = (PMachineValue)(((PrtNamedTuple)payload_5)["participant"]);
            TMP_tmp4_9 = (PrtInt)(((PrtNamedTuple)payload_5)["transId"]);
            ((PrtMap)receivedLocalCommits)[TMP_tmp3_11] = TMP_tmp4_9;
        }
        public void Anon_21()
        {
            Atomicity currentMachine = this;
            PrtInt TMP_tmp0_19 = ((PrtInt)0);
            PrtBool TMP_tmp1_18 = ((PrtBool)false);
            PrtMap TMP_tmp2_14 = new PrtMap();
            TMP_tmp0_19 = (PrtInt)(((PrtInt)(receivedLocalCommits).Count));
            TMP_tmp1_18 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_19,numParticipants)));
            currentMachine.Assert(TMP_tmp1_18,"");
            TMP_tmp2_14 = (PrtMap)(new PrtMap());
            receivedLocalCommits = TMP_tmp2_14;
        }
        public void Anon_22()
        {
            Atomicity currentMachine = this;
            PrtMap TMP_tmp0_20 = new PrtMap();
            TMP_tmp0_20 = (PrtMap)(new PrtMap());
            receivedLocalCommits = TMP_tmp0_20;
        }
        [Start]
        [OnEventGotoState(typeof(eMonitor_AtomicityInitialize), typeof(WaitForEvents), nameof(Anon_19))]
        class Init_3 : MonitorState
        {
        }
        [OnEventDoAction(typeof(eMonitor_LocalCommit), nameof(Anon_20))]
        [OnEventDoAction(typeof(eWriteTransSuccess), nameof(Anon_21))]
        [OnEventDoAction(typeof(eWriteTransFailed), nameof(Anon_22))]
        class WaitForEvents : MonitorState
        {
        }
    }
    internal partial class Progress : PMonitor
    {
        static Progress() {
            observes.Add(nameof(eWriteTransFailed));
            observes.Add(nameof(eWriteTransSuccess));
            observes.Add(nameof(eWriteTransaction));
        }
        
        [Start]
        [OnEventGotoState(typeof(eWriteTransaction), typeof(WaitForOperationToFinish))]
        [IgnoreEvents(typeof(eWriteTransFailed), typeof(eWriteTransSuccess))]
        class Init_4 : MonitorState
        {
        }
        [Hot]
        [OnEventGotoState(typeof(eWriteTransSuccess), typeof(Init_4))]
        [OnEventGotoState(typeof(eWriteTransFailed), typeof(Init_4))]
        [IgnoreEvents(typeof(eWriteTransaction))]
        class WaitForOperationToFinish : MonitorState
        {
        }
    }
    internal partial class TestDriver0 : PMachine
    {
        public class ConstructorEvent : PEvent{public ConstructorEvent(IPrtValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public TestDriver0() {
            this.sends.Add(nameof(eCancelTimer));
            this.sends.Add(nameof(eCancelTimerFailed));
            this.sends.Add(nameof(eCancelTimerSuccess));
            this.sends.Add(nameof(eGlobalAbort));
            this.sends.Add(nameof(eGlobalCommit));
            this.sends.Add(nameof(eMonitor_AtomicityInitialize));
            this.sends.Add(nameof(eMonitor_LocalCommit));
            this.sends.Add(nameof(ePrepare));
            this.sends.Add(nameof(ePrepareFailed));
            this.sends.Add(nameof(ePrepareSuccess));
            this.sends.Add(nameof(eReadTransFailed));
            this.sends.Add(nameof(eReadTransSuccess));
            this.sends.Add(nameof(eReadTransaction));
            this.sends.Add(nameof(eStartTimer));
            this.sends.Add(nameof(eTimeOut));
            this.sends.Add(nameof(eWriteTransFailed));
            this.sends.Add(nameof(eWriteTransSuccess));
            this.sends.Add(nameof(eWriteTransaction));
            this.sends.Add(nameof(PHalt));
            this.sends.Add(nameof(local_event));
            this.receives.Add(nameof(eCancelTimer));
            this.receives.Add(nameof(eCancelTimerFailed));
            this.receives.Add(nameof(eCancelTimerSuccess));
            this.receives.Add(nameof(eGlobalAbort));
            this.receives.Add(nameof(eGlobalCommit));
            this.receives.Add(nameof(eMonitor_AtomicityInitialize));
            this.receives.Add(nameof(eMonitor_LocalCommit));
            this.receives.Add(nameof(ePrepare));
            this.receives.Add(nameof(ePrepareFailed));
            this.receives.Add(nameof(ePrepareSuccess));
            this.receives.Add(nameof(eReadTransFailed));
            this.receives.Add(nameof(eReadTransSuccess));
            this.receives.Add(nameof(eReadTransaction));
            this.receives.Add(nameof(eStartTimer));
            this.receives.Add(nameof(eTimeOut));
            this.receives.Add(nameof(eWriteTransFailed));
            this.receives.Add(nameof(eWriteTransSuccess));
            this.receives.Add(nameof(eWriteTransaction));
            this.receives.Add(nameof(PHalt));
            this.receives.Add(nameof(local_event));
            this.creates.Add(nameof(I_Client));
            this.creates.Add(nameof(I_Coordinator));
            this.creates.Add(nameof(I_Participant));
        }
        
        public void Anon_23()
        {
            TestDriver0 currentMachine = this;
            PMachineValue coord = null;
            PrtSeq participants_1 = new PrtSeq();
            PrtInt i_2 = ((PrtInt)0);
            PrtBool TMP_tmp0_21 = ((PrtBool)false);
            PrtBool TMP_tmp1_19 = ((PrtBool)false);
            PMachineValue TMP_tmp2_15 = null;
            PrtInt TMP_tmp3_12 = ((PrtInt)0);
            PrtSeq TMP_tmp4_10 = new PrtSeq();
            PMachineValue TMP_tmp5_7 = null;
            PMachineValue TMP_tmp6_6 = null;
            PMachineValue TMP_tmp7_6 = null;
            TMP_tmp0_21 = (PrtBool)((i_2) < (((PrtInt)2)));
            TMP_tmp1_19 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp0_21)?.Clone()));
            while (TMP_tmp1_19)
            {
                TMP_tmp2_15 = (PMachineValue)(currentMachine.CreateInterface<I_Participant>( currentMachine));
                ((PrtSeq)participants_1).Insert(i_2, TMP_tmp2_15);
                TMP_tmp3_12 = (PrtInt)((i_2) + (((PrtInt)1)));
                i_2 = TMP_tmp3_12;
                TMP_tmp0_21 = (PrtBool)((i_2) < (((PrtInt)2)));
                TMP_tmp1_19 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp0_21)?.Clone()));
            }
            TMP_tmp4_10 = (PrtSeq)(((PrtSeq)((IPrtValue)participants_1)?.Clone()));
            TMP_tmp5_7 = (PMachineValue)(currentMachine.CreateInterface<I_Coordinator>( currentMachine, TMP_tmp4_10));
            coord = (PMachineValue)TMP_tmp5_7;
            TMP_tmp6_6 = (PMachineValue)(((PMachineValue)((IPrtValue)coord)?.Clone()));
            currentMachine.CreateInterface<I_Client>(currentMachine, TMP_tmp6_6);
            TMP_tmp7_6 = (PMachineValue)(((PMachineValue)((IPrtValue)coord)?.Clone()));
            currentMachine.CreateInterface<I_Client>(currentMachine, TMP_tmp7_6);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_5))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_23))]
        class Init_5 : MachineState
        {
        }
    }
    internal partial class TestDriver1 : PMachine
    {
        public class ConstructorEvent : PEvent{public ConstructorEvent(IPrtValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public TestDriver1() {
            this.sends.Add(nameof(eCancelTimer));
            this.sends.Add(nameof(eCancelTimerFailed));
            this.sends.Add(nameof(eCancelTimerSuccess));
            this.sends.Add(nameof(eGlobalAbort));
            this.sends.Add(nameof(eGlobalCommit));
            this.sends.Add(nameof(eMonitor_AtomicityInitialize));
            this.sends.Add(nameof(eMonitor_LocalCommit));
            this.sends.Add(nameof(ePrepare));
            this.sends.Add(nameof(ePrepareFailed));
            this.sends.Add(nameof(ePrepareSuccess));
            this.sends.Add(nameof(eReadTransFailed));
            this.sends.Add(nameof(eReadTransSuccess));
            this.sends.Add(nameof(eReadTransaction));
            this.sends.Add(nameof(eStartTimer));
            this.sends.Add(nameof(eTimeOut));
            this.sends.Add(nameof(eWriteTransFailed));
            this.sends.Add(nameof(eWriteTransSuccess));
            this.sends.Add(nameof(eWriteTransaction));
            this.sends.Add(nameof(PHalt));
            this.sends.Add(nameof(local_event));
            this.receives.Add(nameof(eCancelTimer));
            this.receives.Add(nameof(eCancelTimerFailed));
            this.receives.Add(nameof(eCancelTimerSuccess));
            this.receives.Add(nameof(eGlobalAbort));
            this.receives.Add(nameof(eGlobalCommit));
            this.receives.Add(nameof(eMonitor_AtomicityInitialize));
            this.receives.Add(nameof(eMonitor_LocalCommit));
            this.receives.Add(nameof(ePrepare));
            this.receives.Add(nameof(ePrepareFailed));
            this.receives.Add(nameof(ePrepareSuccess));
            this.receives.Add(nameof(eReadTransFailed));
            this.receives.Add(nameof(eReadTransSuccess));
            this.receives.Add(nameof(eReadTransaction));
            this.receives.Add(nameof(eStartTimer));
            this.receives.Add(nameof(eTimeOut));
            this.receives.Add(nameof(eWriteTransFailed));
            this.receives.Add(nameof(eWriteTransSuccess));
            this.receives.Add(nameof(eWriteTransaction));
            this.receives.Add(nameof(PHalt));
            this.receives.Add(nameof(local_event));
            this.creates.Add(nameof(I_Client));
            this.creates.Add(nameof(I_Coordinator));
            this.creates.Add(nameof(I_FailureInjector));
            this.creates.Add(nameof(I_Participant));
        }
        
        public void Anon_24()
        {
            TestDriver1 currentMachine = this;
            PMachineValue coord_1 = null;
            PrtSeq participants_2 = new PrtSeq();
            PrtInt i_3 = ((PrtInt)0);
            PrtBool TMP_tmp0_22 = ((PrtBool)false);
            PrtBool TMP_tmp1_20 = ((PrtBool)false);
            PMachineValue TMP_tmp2_16 = null;
            PrtInt TMP_tmp3_13 = ((PrtInt)0);
            PrtSeq TMP_tmp4_11 = new PrtSeq();
            PMachineValue TMP_tmp5_8 = null;
            PrtSeq TMP_tmp6_7 = new PrtSeq();
            PMachineValue TMP_tmp7_7 = null;
            PMachineValue TMP_tmp8_5 = null;
            TMP_tmp0_22 = (PrtBool)((i_3) < (((PrtInt)2)));
            TMP_tmp1_20 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp0_22)?.Clone()));
            while (TMP_tmp1_20)
            {
                TMP_tmp2_16 = (PMachineValue)(currentMachine.CreateInterface<I_Participant>( currentMachine));
                ((PrtSeq)participants_2).Insert(i_3, TMP_tmp2_16);
                TMP_tmp3_13 = (PrtInt)((i_3) + (((PrtInt)1)));
                i_3 = TMP_tmp3_13;
                TMP_tmp0_22 = (PrtBool)((i_3) < (((PrtInt)2)));
                TMP_tmp1_20 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp0_22)?.Clone()));
            }
            TMP_tmp4_11 = (PrtSeq)(((PrtSeq)((IPrtValue)participants_2)?.Clone()));
            TMP_tmp5_8 = (PMachineValue)(currentMachine.CreateInterface<I_Coordinator>( currentMachine, TMP_tmp4_11));
            coord_1 = (PMachineValue)TMP_tmp5_8;
            TMP_tmp6_7 = (PrtSeq)(((PrtSeq)((IPrtValue)participants_2)?.Clone()));
            currentMachine.CreateInterface<I_FailureInjector>(currentMachine, TMP_tmp6_7);
            TMP_tmp7_7 = (PMachineValue)(((PMachineValue)((IPrtValue)coord_1)?.Clone()));
            currentMachine.CreateInterface<I_Client>(currentMachine, TMP_tmp7_7);
            TMP_tmp8_5 = (PMachineValue)(((PMachineValue)((IPrtValue)coord_1)?.Clone()));
            currentMachine.CreateInterface<I_Client>(currentMachine, TMP_tmp8_5);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_6))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_24))]
        class Init_6 : MachineState
        {
        }
    }
    internal partial class FailureInjector : PMachine
    {
        public class ConstructorEvent : PEvent{public ConstructorEvent(PrtSeq val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PrtSeq)value); }
        public FailureInjector() {
            this.sends.Add(nameof(eCancelTimer));
            this.sends.Add(nameof(eCancelTimerFailed));
            this.sends.Add(nameof(eCancelTimerSuccess));
            this.sends.Add(nameof(eGlobalAbort));
            this.sends.Add(nameof(eGlobalCommit));
            this.sends.Add(nameof(eMonitor_AtomicityInitialize));
            this.sends.Add(nameof(eMonitor_LocalCommit));
            this.sends.Add(nameof(ePrepare));
            this.sends.Add(nameof(ePrepareFailed));
            this.sends.Add(nameof(ePrepareSuccess));
            this.sends.Add(nameof(eReadTransFailed));
            this.sends.Add(nameof(eReadTransSuccess));
            this.sends.Add(nameof(eReadTransaction));
            this.sends.Add(nameof(eStartTimer));
            this.sends.Add(nameof(eTimeOut));
            this.sends.Add(nameof(eWriteTransFailed));
            this.sends.Add(nameof(eWriteTransSuccess));
            this.sends.Add(nameof(eWriteTransaction));
            this.sends.Add(nameof(PHalt));
            this.sends.Add(nameof(local_event));
            this.receives.Add(nameof(eCancelTimer));
            this.receives.Add(nameof(eCancelTimerFailed));
            this.receives.Add(nameof(eCancelTimerSuccess));
            this.receives.Add(nameof(eGlobalAbort));
            this.receives.Add(nameof(eGlobalCommit));
            this.receives.Add(nameof(eMonitor_AtomicityInitialize));
            this.receives.Add(nameof(eMonitor_LocalCommit));
            this.receives.Add(nameof(ePrepare));
            this.receives.Add(nameof(ePrepareFailed));
            this.receives.Add(nameof(ePrepareSuccess));
            this.receives.Add(nameof(eReadTransFailed));
            this.receives.Add(nameof(eReadTransSuccess));
            this.receives.Add(nameof(eReadTransaction));
            this.receives.Add(nameof(eStartTimer));
            this.receives.Add(nameof(eTimeOut));
            this.receives.Add(nameof(eWriteTransFailed));
            this.receives.Add(nameof(eWriteTransSuccess));
            this.receives.Add(nameof(eWriteTransaction));
            this.receives.Add(nameof(PHalt));
            this.receives.Add(nameof(local_event));
        }
        
        public void Anon_25()
        {
            FailureInjector currentMachine = this;
            PrtSeq participants_3 = (PrtSeq)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt i_4 = ((PrtInt)0);
            PrtInt TMP_tmp0_23 = ((PrtInt)0);
            PrtBool TMP_tmp1_21 = ((PrtBool)false);
            PrtBool TMP_tmp2_17 = ((PrtBool)false);
            PrtBool TMP_tmp3_14 = ((PrtBool)false);
            PMachineValue TMP_tmp4_12 = null;
            PMachineValue TMP_tmp5_9 = null;
            PEvent TMP_tmp6_8 = null;
            PrtInt TMP_tmp7_8 = ((PrtInt)0);
            i_4 = (PrtInt)(((PrtInt)0));
            TMP_tmp0_23 = (PrtInt)(((PrtInt)(participants_3).Count));
            TMP_tmp1_21 = (PrtBool)((i_4) < (TMP_tmp0_23));
            TMP_tmp2_17 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp1_21)?.Clone()));
            while (TMP_tmp2_17)
            {
                TMP_tmp3_14 = (PrtBool)(((PrtBool)currentMachine.Random()));
                if (TMP_tmp3_14)
                {
                    TMP_tmp4_12 = (PMachineValue)(((PrtSeq)participants_3)[i_4]);
                    TMP_tmp5_9 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp4_12)?.Clone()));
                    TMP_tmp6_8 = (PEvent)(new PHalt(null));
                    currentMachine.SendEvent(TMP_tmp5_9, (Event)TMP_tmp6_8);
                }
                TMP_tmp7_8 = (PrtInt)((i_4) + (((PrtInt)1)));
                i_4 = TMP_tmp7_8;
                TMP_tmp0_23 = (PrtInt)(((PrtInt)(participants_3).Count));
                TMP_tmp1_21 = (PrtBool)((i_4) < (TMP_tmp0_23));
                TMP_tmp2_17 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp1_21)?.Clone()));
            }
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_7))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_25))]
        class Init_7 : MachineState
        {
        }
    }
    internal partial class Timer : PMachine
    {
        private PMachineValue target = null;
        public class ConstructorEvent : PEvent{public ConstructorEvent(PMachineValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PMachineValue)value); }
        public Timer() {
            this.sends.Add(nameof(eCancelTimerFailed));
            this.sends.Add(nameof(eCancelTimerSuccess));
            this.sends.Add(nameof(eTimeOut));
            this.receives.Add(nameof(eCancelTimer));
            this.receives.Add(nameof(eStartTimer));
        }
        
        public void Anon_26()
        {
            Timer currentMachine = this;
            PMachineValue payload_6 = (PMachineValue)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            target = (PMachineValue)(((PMachineValue)((IPrtValue)payload_6)?.Clone()));
            currentMachine.GotoState<Timer.WaitForStartTimer>();
            throw new PUnreachableCodeException();
        }
        public void Anon_27()
        {
            Timer currentMachine = this;
            PMachineValue TMP_tmp0_24 = null;
            PEvent TMP_tmp1_22 = null;
            TMP_tmp0_24 = (PMachineValue)(((PMachineValue)((IPrtValue)target)?.Clone()));
            TMP_tmp1_22 = (PEvent)(new eCancelTimerFailed(null));
            currentMachine.SendEvent(TMP_tmp0_24, (Event)TMP_tmp1_22);
        }
        public void Anon_28()
        {
            Timer currentMachine = this;
            PrtInt payload_7 = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtBool TMP_tmp0_25 = ((PrtBool)false);
            PMachineValue TMP_tmp1_23 = null;
            PEvent TMP_tmp2_18 = null;
            TMP_tmp0_25 = (PrtBool)(((PrtBool)currentMachine.Random()));
            if (TMP_tmp0_25)
            {
                TMP_tmp1_23 = (PMachineValue)(((PMachineValue)((IPrtValue)target)?.Clone()));
                TMP_tmp2_18 = (PEvent)(new eTimeOut(null));
                currentMachine.SendEvent(TMP_tmp1_23, (Event)TMP_tmp2_18);
                currentMachine.GotoState<Timer.WaitForStartTimer>();
                throw new PUnreachableCodeException();
            }
        }
        public void Anon_29()
        {
            Timer currentMachine = this;
            PrtBool TMP_tmp0_26 = ((PrtBool)false);
            PMachineValue TMP_tmp1_24 = null;
            PEvent TMP_tmp2_19 = null;
            PMachineValue TMP_tmp3_15 = null;
            PEvent TMP_tmp4_13 = null;
            PMachineValue TMP_tmp5_10 = null;
            PEvent TMP_tmp6_9 = null;
            TMP_tmp0_26 = (PrtBool)(((PrtBool)currentMachine.Random()));
            if (TMP_tmp0_26)
            {
                TMP_tmp1_24 = (PMachineValue)(((PMachineValue)((IPrtValue)target)?.Clone()));
                TMP_tmp2_19 = (PEvent)(new eCancelTimerFailed(null));
                currentMachine.SendEvent(TMP_tmp1_24, (Event)TMP_tmp2_19);
                TMP_tmp3_15 = (PMachineValue)(((PMachineValue)((IPrtValue)target)?.Clone()));
                TMP_tmp4_13 = (PEvent)(new eTimeOut(null));
                currentMachine.SendEvent(TMP_tmp3_15, (Event)TMP_tmp4_13);
            }
            else
            {
                TMP_tmp5_10 = (PMachineValue)(((PMachineValue)((IPrtValue)target)?.Clone()));
                TMP_tmp6_9 = (PEvent)(new eCancelTimerSuccess(null));
                currentMachine.SendEvent(TMP_tmp5_10, (Event)TMP_tmp6_9);
            }
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_8))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_26))]
        class Init_8 : MachineState
        {
        }
        [OnEventGotoState(typeof(eStartTimer), typeof(TimerStarted))]
        [OnEventDoAction(typeof(eCancelTimer), nameof(Anon_27))]
        class WaitForStartTimer : MachineState
        {
        }
        [OnEntry(nameof(Anon_28))]
        [OnEventGotoState(typeof(eCancelTimer), typeof(WaitForStartTimer), nameof(Anon_29))]
        class TimerStarted : MachineState
        {
        }
    }
    public class Test0 {
        public static void InitializeLinkMap() {
            PModule.linkMap.Clear();
            PModule.linkMap[nameof(I_TestDriver0)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_TestDriver0)].Add(nameof(I_Client), nameof(I_Client));
            PModule.linkMap[nameof(I_TestDriver0)].Add(nameof(I_Coordinator), nameof(I_Coordinator));
            PModule.linkMap[nameof(I_TestDriver0)].Add(nameof(I_Participant), nameof(I_Participant));
            PModule.linkMap[nameof(I_Coordinator)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Coordinator)].Add(nameof(I_Timer), nameof(I_Timer));
            PModule.linkMap[nameof(I_Participant)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Timer)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Client)] = new Dictionary<string, string>();
        }
        
        public static void InitializeInterfaceDefMap() {
            PModule.interfaceDefinitionMap.Clear();
            PModule.interfaceDefinitionMap.Add(nameof(I_TestDriver0), typeof(TestDriver0));
            PModule.interfaceDefinitionMap.Add(nameof(I_Coordinator), typeof(Coordinator));
            PModule.interfaceDefinitionMap.Add(nameof(I_Participant), typeof(Participant));
            PModule.interfaceDefinitionMap.Add(nameof(I_Timer), typeof(Timer));
            PModule.interfaceDefinitionMap.Add(nameof(I_Client), typeof(Client));
        }
        
        public static void InitializeMonitorObserves() {
            PModule.monitorObserves.Clear();
        }
        
        public static void InitializeMonitorMap(IMachineRuntime runtime) {
            PModule.monitorMap.Clear();
        }
        
        
        [Microsoft.PSharp.Test]
        public static void Execute(IMachineRuntime runtime) {
            runtime.SetLogWriter(new PLogger());
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            PHelper.InitializeEnums();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof(TestDriver0)));
        }
    }
    public class Test1 {
        public static void InitializeLinkMap() {
            PModule.linkMap.Clear();
            PModule.linkMap[nameof(I_TestDriver1)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_TestDriver1)].Add(nameof(I_Client), nameof(I_Client));
            PModule.linkMap[nameof(I_TestDriver1)].Add(nameof(I_Coordinator), nameof(I_Coordinator));
            PModule.linkMap[nameof(I_TestDriver1)].Add(nameof(I_FailureInjector), nameof(I_FailureInjector));
            PModule.linkMap[nameof(I_TestDriver1)].Add(nameof(I_Participant), nameof(I_Participant));
            PModule.linkMap[nameof(I_Coordinator)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Coordinator)].Add(nameof(I_Timer), nameof(I_Timer));
            PModule.linkMap[nameof(I_Participant)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Timer)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Client)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_FailureInjector)] = new Dictionary<string, string>();
        }
        
        public static void InitializeInterfaceDefMap() {
            PModule.interfaceDefinitionMap.Clear();
            PModule.interfaceDefinitionMap.Add(nameof(I_TestDriver1), typeof(TestDriver1));
            PModule.interfaceDefinitionMap.Add(nameof(I_Coordinator), typeof(Coordinator));
            PModule.interfaceDefinitionMap.Add(nameof(I_Participant), typeof(Participant));
            PModule.interfaceDefinitionMap.Add(nameof(I_Timer), typeof(Timer));
            PModule.interfaceDefinitionMap.Add(nameof(I_Client), typeof(Client));
            PModule.interfaceDefinitionMap.Add(nameof(I_FailureInjector), typeof(FailureInjector));
        }
        
        public static void InitializeMonitorObserves() {
            PModule.monitorObserves.Clear();
            PModule.monitorObserves[nameof(Progress)] = new List<string>();
            PModule.monitorObserves[nameof(Progress)].Add(nameof(eWriteTransFailed));
            PModule.monitorObserves[nameof(Progress)].Add(nameof(eWriteTransSuccess));
            PModule.monitorObserves[nameof(Progress)].Add(nameof(eWriteTransaction));
        }
        
        public static void InitializeMonitorMap(IMachineRuntime runtime) {
            PModule.monitorMap.Clear();
            PModule.monitorMap[nameof(I_TestDriver1)] = new List<Type>();
            PModule.monitorMap[nameof(I_TestDriver1)].Add(typeof(Progress));
            PModule.monitorMap[nameof(I_Coordinator)] = new List<Type>();
            PModule.monitorMap[nameof(I_Coordinator)].Add(typeof(Progress));
            PModule.monitorMap[nameof(I_Participant)] = new List<Type>();
            PModule.monitorMap[nameof(I_Participant)].Add(typeof(Progress));
            PModule.monitorMap[nameof(I_Timer)] = new List<Type>();
            PModule.monitorMap[nameof(I_Timer)].Add(typeof(Progress));
            PModule.monitorMap[nameof(I_Client)] = new List<Type>();
            PModule.monitorMap[nameof(I_Client)].Add(typeof(Progress));
            PModule.monitorMap[nameof(I_FailureInjector)] = new List<Type>();
            PModule.monitorMap[nameof(I_FailureInjector)].Add(typeof(Progress));
            runtime.RegisterMonitor(typeof(Progress));
        }
        
        
        [Microsoft.PSharp.Test]
        public static void Execute(IMachineRuntime runtime) {
            runtime.SetLogWriter(new PLogger());
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            PHelper.InitializeEnums();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof(TestDriver1)));
        }
    }
    public class Test2 {
        public static void InitializeLinkMap() {
            PModule.linkMap.Clear();
            PModule.linkMap[nameof(I_TestDriver0)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_TestDriver0)].Add(nameof(I_Client), nameof(I_Client));
            PModule.linkMap[nameof(I_TestDriver0)].Add(nameof(I_Coordinator), nameof(I_Coordinator));
            PModule.linkMap[nameof(I_TestDriver0)].Add(nameof(I_Participant), nameof(I_Participant));
            PModule.linkMap[nameof(I_Coordinator)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Coordinator)].Add(nameof(I_Timer), nameof(I_Timer));
            PModule.linkMap[nameof(I_Participant)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Timer)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Client)] = new Dictionary<string, string>();
        }
        
        public static void InitializeInterfaceDefMap() {
            PModule.interfaceDefinitionMap.Clear();
            PModule.interfaceDefinitionMap.Add(nameof(I_TestDriver0), typeof(TestDriver0));
            PModule.interfaceDefinitionMap.Add(nameof(I_Coordinator), typeof(Coordinator));
            PModule.interfaceDefinitionMap.Add(nameof(I_Participant), typeof(Participant));
            PModule.interfaceDefinitionMap.Add(nameof(I_Timer), typeof(Timer));
            PModule.interfaceDefinitionMap.Add(nameof(I_Client), typeof(Client));
        }
        
        public static void InitializeMonitorObserves() {
            PModule.monitorObserves.Clear();
            PModule.monitorObserves[nameof(Atomicity)] = new List<string>();
            PModule.monitorObserves[nameof(Atomicity)].Add(nameof(eMonitor_AtomicityInitialize));
            PModule.monitorObserves[nameof(Atomicity)].Add(nameof(eMonitor_LocalCommit));
            PModule.monitorObserves[nameof(Atomicity)].Add(nameof(eWriteTransFailed));
            PModule.monitorObserves[nameof(Atomicity)].Add(nameof(eWriteTransSuccess));
        }
        
        public static void InitializeMonitorMap(IMachineRuntime runtime) {
            PModule.monitorMap.Clear();
            PModule.monitorMap[nameof(I_TestDriver0)] = new List<Type>();
            PModule.monitorMap[nameof(I_TestDriver0)].Add(typeof(Atomicity));
            PModule.monitorMap[nameof(I_Coordinator)] = new List<Type>();
            PModule.monitorMap[nameof(I_Coordinator)].Add(typeof(Atomicity));
            PModule.monitorMap[nameof(I_Participant)] = new List<Type>();
            PModule.monitorMap[nameof(I_Participant)].Add(typeof(Atomicity));
            PModule.monitorMap[nameof(I_Timer)] = new List<Type>();
            PModule.monitorMap[nameof(I_Timer)].Add(typeof(Atomicity));
            PModule.monitorMap[nameof(I_Client)] = new List<Type>();
            PModule.monitorMap[nameof(I_Client)].Add(typeof(Atomicity));
            runtime.RegisterMonitor(typeof(Atomicity));
        }
        
        
        [Microsoft.PSharp.Test]
        public static void Execute(IMachineRuntime runtime) {
            runtime.SetLogWriter(new PLogger());
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            PHelper.InitializeEnums();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof(TestDriver0)));
        }
    }
    public class I_Client : PMachineValue {
        public I_Client (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_Coordinator : PMachineValue {
        public I_Coordinator (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_Participant : PMachineValue {
        public I_Participant (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_TestDriver0 : PMachineValue {
        public I_TestDriver0 (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_TestDriver1 : PMachineValue {
        public I_TestDriver1 (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_FailureInjector : PMachineValue {
        public I_FailureInjector (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_Timer : PMachineValue {
        public I_Timer (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public partial class PHelper {
        public static void InitializeInterfaces() {
            PInterfaces.Clear();
            PInterfaces.AddInterface(nameof(I_Client), nameof(eCancelTimer), nameof(eCancelTimerFailed), nameof(eCancelTimerSuccess), nameof(eGlobalAbort), nameof(eGlobalCommit), nameof(eMonitor_AtomicityInitialize), nameof(eMonitor_LocalCommit), nameof(ePrepare), nameof(ePrepareFailed), nameof(ePrepareSuccess), nameof(eReadTransFailed), nameof(eReadTransSuccess), nameof(eReadTransaction), nameof(eStartTimer), nameof(eTimeOut), nameof(eWriteTransFailed), nameof(eWriteTransSuccess), nameof(eWriteTransaction), nameof(PHalt), nameof(local_event));
            PInterfaces.AddInterface(nameof(I_Coordinator), nameof(eCancelTimer), nameof(eCancelTimerFailed), nameof(eCancelTimerSuccess), nameof(eGlobalAbort), nameof(eGlobalCommit), nameof(eMonitor_AtomicityInitialize), nameof(eMonitor_LocalCommit), nameof(ePrepare), nameof(ePrepareFailed), nameof(ePrepareSuccess), nameof(eReadTransFailed), nameof(eReadTransSuccess), nameof(eReadTransaction), nameof(eStartTimer), nameof(eTimeOut), nameof(eWriteTransFailed), nameof(eWriteTransSuccess), nameof(eWriteTransaction), nameof(PHalt), nameof(local_event));
            PInterfaces.AddInterface(nameof(I_Participant), nameof(eCancelTimer), nameof(eCancelTimerFailed), nameof(eCancelTimerSuccess), nameof(eGlobalAbort), nameof(eGlobalCommit), nameof(eMonitor_AtomicityInitialize), nameof(eMonitor_LocalCommit), nameof(ePrepare), nameof(ePrepareFailed), nameof(ePrepareSuccess), nameof(eReadTransFailed), nameof(eReadTransSuccess), nameof(eReadTransaction), nameof(eStartTimer), nameof(eTimeOut), nameof(eWriteTransFailed), nameof(eWriteTransSuccess), nameof(eWriteTransaction), nameof(PHalt), nameof(local_event));
            PInterfaces.AddInterface(nameof(I_TestDriver0), nameof(eCancelTimer), nameof(eCancelTimerFailed), nameof(eCancelTimerSuccess), nameof(eGlobalAbort), nameof(eGlobalCommit), nameof(eMonitor_AtomicityInitialize), nameof(eMonitor_LocalCommit), nameof(ePrepare), nameof(ePrepareFailed), nameof(ePrepareSuccess), nameof(eReadTransFailed), nameof(eReadTransSuccess), nameof(eReadTransaction), nameof(eStartTimer), nameof(eTimeOut), nameof(eWriteTransFailed), nameof(eWriteTransSuccess), nameof(eWriteTransaction), nameof(PHalt), nameof(local_event));
            PInterfaces.AddInterface(nameof(I_TestDriver1), nameof(eCancelTimer), nameof(eCancelTimerFailed), nameof(eCancelTimerSuccess), nameof(eGlobalAbort), nameof(eGlobalCommit), nameof(eMonitor_AtomicityInitialize), nameof(eMonitor_LocalCommit), nameof(ePrepare), nameof(ePrepareFailed), nameof(ePrepareSuccess), nameof(eReadTransFailed), nameof(eReadTransSuccess), nameof(eReadTransaction), nameof(eStartTimer), nameof(eTimeOut), nameof(eWriteTransFailed), nameof(eWriteTransSuccess), nameof(eWriteTransaction), nameof(PHalt), nameof(local_event));
            PInterfaces.AddInterface(nameof(I_FailureInjector), nameof(eCancelTimer), nameof(eCancelTimerFailed), nameof(eCancelTimerSuccess), nameof(eGlobalAbort), nameof(eGlobalCommit), nameof(eMonitor_AtomicityInitialize), nameof(eMonitor_LocalCommit), nameof(ePrepare), nameof(ePrepareFailed), nameof(ePrepareSuccess), nameof(eReadTransFailed), nameof(eReadTransSuccess), nameof(eReadTransaction), nameof(eStartTimer), nameof(eTimeOut), nameof(eWriteTransFailed), nameof(eWriteTransSuccess), nameof(eWriteTransaction), nameof(PHalt), nameof(local_event));
            PInterfaces.AddInterface(nameof(I_Timer), nameof(eCancelTimer), nameof(eStartTimer));
        }
    }
    
    public partial class PHelper {
        public static void InitializeEnums() {
            PrtEnum.Clear();
        }
    }
    
}
#pragma warning restore 162, 219, 414
