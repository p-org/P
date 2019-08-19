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
    public static partial class GlobalFunctions {
        public static PrtNamedTuple ChooseTransaction(PMachine pMachine)
        {
            return (new PrtNamedTuple(new string[] { "client", "key", "val" }, ((PMachineValue)pMachine.self), ((PrtInt)10), ((PrtInt)1))); ;
        }
    }
    internal partial class local_event : PEvent
    {
        static local_event() { AssertVal = -1; AssumeVal = -1; }
        public local_event() : base() { }
        public local_event(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new local_event(); }
    }
    internal partial class ePrepare : PEvent
    {
        static ePrepare() { AssertVal = -1; AssumeVal = -1; }
        public ePrepare() : base() { }
        public ePrepare(PrtNamedTuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new ePrepare(); }
    }
    internal partial class ePrepareSuccess : PEvent
    {
        static ePrepareSuccess() { AssertVal = -1; AssumeVal = -1; }
        public ePrepareSuccess() : base() { }
        public ePrepareSuccess(PrtNamedTuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new ePrepareSuccess(); }
    }
    internal partial class ePrepareFailed : PEvent
    {
        static ePrepareFailed() { AssertVal = -1; AssumeVal = -1; }
        public ePrepareFailed() : base() { }
        public ePrepareFailed(PrtNamedTuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new ePrepareFailed(); }
    }
    internal partial class eGlobalAbort : PEvent
    {
        static eGlobalAbort() { AssertVal = -1; AssumeVal = -1; }
        public eGlobalAbort() : base() { }
        public eGlobalAbort(PrtNamedTuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new eGlobalAbort(); }
    }
    internal partial class eGlobalCommit : PEvent
    {
        static eGlobalCommit() { AssertVal = -1; AssumeVal = -1; }
        public eGlobalCommit() : base() { }
        public eGlobalCommit(PrtNamedTuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new eGlobalCommit(); }
    }
    internal partial class eWriteTransaction : PEvent
    {
        static eWriteTransaction() { AssertVal = -1; AssumeVal = -1; }
        public eWriteTransaction() : base() { }
        public eWriteTransaction(PrtNamedTuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new eWriteTransaction(); }
    }
    internal partial class eWriteTransFailed : PEvent
    {
        static eWriteTransFailed() { AssertVal = -1; AssumeVal = -1; }
        public eWriteTransFailed() : base() { }
        public eWriteTransFailed(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eWriteTransFailed(); }
    }
    internal partial class eWriteTransSuccess : PEvent
    {
        static eWriteTransSuccess() { AssertVal = -1; AssumeVal = -1; }
        public eWriteTransSuccess() : base() { }
        public eWriteTransSuccess(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eWriteTransSuccess(); }
    }
    internal partial class eReadTransaction : PEvent
    {
        static eReadTransaction() { AssertVal = -1; AssumeVal = -1; }
        public eReadTransaction() : base() { }
        public eReadTransaction(PrtNamedTuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new eReadTransaction(); }
    }
    internal partial class eReadTransFailed : PEvent
    {
        static eReadTransFailed() { AssertVal = -1; AssumeVal = -1; }
        public eReadTransFailed() : base() { }
        public eReadTransFailed(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eReadTransFailed(); }
    }
    internal partial class eReadTransUnAvailable : PEvent
    {
        static eReadTransUnAvailable() { AssertVal = -1; AssumeVal = -1; }
        public eReadTransUnAvailable() : base() { }
        public eReadTransUnAvailable(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eReadTransUnAvailable(); }
    }
    internal partial class eReadTransSuccess : PEvent
    {
        static eReadTransSuccess() { AssertVal = -1; AssumeVal = -1; }
        public eReadTransSuccess() : base() { }
        public eReadTransSuccess(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new eReadTransSuccess(); }
    }
    internal partial class eTimeOut : PEvent
    {
        static eTimeOut() { AssertVal = -1; AssumeVal = -1; }
        public eTimeOut() : base() { }
        public eTimeOut(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eTimeOut(); }
    }
    internal partial class eStartTimer : PEvent
    {
        static eStartTimer() { AssertVal = -1; AssumeVal = -1; }
        public eStartTimer() : base() { }
        public eStartTimer(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new eStartTimer(); }
    }
    internal partial class eCancelTimer : PEvent
    {
        static eCancelTimer() { AssertVal = -1; AssumeVal = -1; }
        public eCancelTimer() : base() { }
        public eCancelTimer(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eCancelTimer(); }
    }
    internal partial class eCancelTimerFailed : PEvent
    {
        static eCancelTimerFailed() { AssertVal = -1; AssumeVal = -1; }
        public eCancelTimerFailed() : base() { }
        public eCancelTimerFailed(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eCancelTimerFailed(); }
    }
    internal partial class eCancelTimerSuccess : PEvent
    {
        static eCancelTimerSuccess() { AssertVal = -1; AssumeVal = -1; }
        public eCancelTimerSuccess() : base() { }
        public eCancelTimerSuccess(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new eCancelTimerSuccess(); }
    }
    internal partial class eMonitor_LocalCommit : PEvent
    {
        static eMonitor_LocalCommit() { AssertVal = -1; AssumeVal = -1; }
        public eMonitor_LocalCommit() : base() { }
        public eMonitor_LocalCommit(PrtNamedTuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new eMonitor_LocalCommit(); }
    }
    internal partial class eMonitor_AtomicityInitialize : PEvent
    {
        static eMonitor_AtomicityInitialize() { AssertVal = -1; AssumeVal = -1; }
        public eMonitor_AtomicityInitialize() : base() { }
        public eMonitor_AtomicityInitialize(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new eMonitor_AtomicityInitialize(); }
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
            TMP_tmp1 = (PMachineValue)(currentMachine.CreateInterface<I_Timer>(currentMachine, TMP_tmp0));
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
            switch (PGEN_recvEvent)
            {
                case eCancelTimerSuccess PGEN_evt:
                    {
                        PModule.runtime.Logger.WriteLine("Timer Cancelled Successful");
                    }
                    break;
                case eCancelTimerFailed PGEN_evt_1:
                    {
                        var PGEN_recvEvent_1 = await currentMachine.ReceiveEvent(typeof(eTimeOut));
                        switch (PGEN_recvEvent_1)
                        {
                            case eTimeOut PGEN_evt_2:
                                {
                                    PModule.runtime.Logger.WriteLine("Timer Cancelled Successful");
                                }
                                break;
                        }
                    }
                    break;
            }
        }
    }
    internal partial class Client : PMachine
    {
        private PMachineValue coordinator = null;
        private PrtNamedTuple randomTransaction = (new PrtNamedTuple(new string[] { "client", "key", "val" }, null, ((PrtInt)0), ((PrtInt)0)));
        public class ConstructorEvent : PEvent { public ConstructorEvent(PMachineValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PMachineValue)value); }
        public Client()
        {
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
            this.sends.Add(nameof(eReadTransUnAvailable));
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
            this.receives.Add(nameof(eReadTransUnAvailable));
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
            PrtNamedTuple TMP_tmp0_3 = (new PrtNamedTuple(new string[] { "client", "key", "val" }, null, ((PrtInt)0), ((PrtInt)0)));
            PMachineValue TMP_tmp1_3 = null;
            PEvent TMP_tmp2_1 = null;
            PrtNamedTuple TMP_tmp3 = (new PrtNamedTuple(new string[] { "client", "key", "val" }, null, ((PrtInt)0), ((PrtInt)0)));
            TMP_tmp0_3 = (PrtNamedTuple)(GlobalFunctions.ChooseTransaction(this));
            randomTransaction = TMP_tmp0_3;
            TMP_tmp1_3 = (PMachineValue)(((PMachineValue)((IPrtValue)coordinator)?.Clone()));
            TMP_tmp2_1 = (PEvent)(new eWriteTransaction((new PrtNamedTuple(new string[] { "client", "key", "val" }, null, ((PrtInt)0), ((PrtInt)0)))));
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
            PrtNamedTuple TMP_tmp4 = (new PrtNamedTuple(new string[] { "client", "key" }, null, ((PrtInt)0)));
            TMP_tmp0_4 = (PMachineValue)(((PMachineValue)((IPrtValue)coordinator)?.Clone()));
            TMP_tmp1_4 = (PEvent)(new eReadTransaction((new PrtNamedTuple(new string[] { "client", "key" }, null, ((PrtInt)0)))));
            TMP_tmp2_2 = (PMachineValue)(currentMachine.self);
            TMP_tmp3_1 = (PrtInt)(((PrtNamedTuple)randomTransaction)["key"]);
            TMP_tmp4 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "client", "key" }, TMP_tmp2_2, TMP_tmp3_1)));
            currentMachine.SendEvent(TMP_tmp0_4, (Event)TMP_tmp1_4, TMP_tmp4);
        }
        public void Anon_3()
        {
            Client currentMachine = this;
            currentMachine.Assert(((PrtBool)false), "Read Failed after Write!!");
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
        [OnEventGotoState(typeof(eReadTransSuccess), typeof(End))]
        class ConfirmTransaction : MachineState
        {
        }
        class End : MachineState
        {
        }
    }
    internal partial class Coordinator : PMachine
    {
        private PrtSeq participants = new PrtSeq();
        private PrtNamedTuple pendingWrTrans = (new PrtNamedTuple(new string[] { "client", "key", "val" }, null, ((PrtInt)0), ((PrtInt)0)));
        private PrtInt currTransId = ((PrtInt)0);
        private PMachineValue timer_2 = null;
        private PrtInt countPrepareResponses = ((PrtInt)0);
        public class ConstructorEvent : PEvent { public ConstructorEvent(PrtInt val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PrtInt)value); }
        public Coordinator()
        {
            this.sends.Add(nameof(eGlobalAbort));
            this.sends.Add(nameof(eGlobalCommit));
            this.sends.Add(nameof(ePrepare));
            this.sends.Add(nameof(eWriteTransFailed));
            this.sends.Add(nameof(eWriteTransSuccess));
            this.receives.Add(nameof(ePrepareFailed));
            this.receives.Add(nameof(ePrepareSuccess));
            this.receives.Add(nameof(eReadTransaction));
            this.receives.Add(nameof(eTimeOut));
            this.receives.Add(nameof(eWriteTransaction));
            this.creates.Add(nameof(I_Participant));
            this.creates.Add(nameof(I_Timer));
        }

        public void Anon_4()
        {
            Coordinator currentMachine = this;
            PrtInt numParticipants = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt i = ((PrtInt)0);
            PMachineValue TMP_tmp0_5 = null;
            PMachineValue TMP_tmp1_5 = null;
            PrtBool TMP_tmp2_3 = ((PrtBool)false);
            PrtBool TMP_tmp3_2 = ((PrtBool)false);
            PrtBool TMP_tmp4_1 = ((PrtBool)false);
            PMachineValue TMP_tmp5 = null;
            PMachineValue TMP_tmp6 = null;
            PrtInt TMP_tmp7 = ((PrtInt)0);
            i = (PrtInt)(((PrtInt)0));
            currTransId = (PrtInt)(((PrtInt)0));
            TMP_tmp0_5 = (PMachineValue)(currentMachine.self);
            TMP_tmp1_5 = (PMachineValue)(GlobalFunctions.CreateTimer(TMP_tmp0_5, this));
            timer_2 = TMP_tmp1_5;
            TMP_tmp2_3 = (PrtBool)((numParticipants) > (((PrtInt)0)));
            currentMachine.Assert(TMP_tmp2_3, "");
            currentMachine.Announce((Event)new eMonitor_AtomicityInitialize(((PrtInt)0)), numParticipants);
            TMP_tmp3_2 = (PrtBool)((i) < (numParticipants));
            TMP_tmp4_1 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp3_2)?.Clone()));
            while (TMP_tmp4_1)
            {
                TMP_tmp5 = (PMachineValue)(currentMachine.self);
                TMP_tmp6 = (PMachineValue)(currentMachine.CreateInterface<I_Participant>(currentMachine, TMP_tmp5));
                ((PrtSeq)participants).Insert(i, TMP_tmp6);
                TMP_tmp7 = (PrtInt)((i) + (((PrtInt)1)));
                i = TMP_tmp7;
                TMP_tmp3_2 = (PrtBool)((i) < (numParticipants));
                TMP_tmp4_1 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp3_2)?.Clone()));
            }
            currentMachine.GotoState<Coordinator.WaitForTransactions>();
            throw new PUnreachableCodeException();
        }
        public void Anon_5()
        {
            Coordinator currentMachine = this;
            PrtNamedTuple wTrans = (PrtNamedTuple)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt TMP_tmp0_6 = ((PrtInt)0);
            PEvent TMP_tmp1_6 = null;
            PrtInt TMP_tmp2_4 = ((PrtInt)0);
            PrtInt TMP_tmp3_3 = ((PrtInt)0);
            PrtInt TMP_tmp4_2 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp5_1 = (new PrtNamedTuple(new string[] { "transId", "key", "val" }, ((PrtInt)0), ((PrtInt)0), ((PrtInt)0)));
            PMachineValue TMP_tmp6_1 = null;
            PrtInt TMP_tmp7_1 = ((PrtInt)0);
            PEvent TMP_tmp8 = null;
            pendingWrTrans = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)wTrans)?.Clone()));
            TMP_tmp0_6 = (PrtInt)((currTransId) + (((PrtInt)1)));
            currTransId = TMP_tmp0_6;
            TMP_tmp1_6 = (PEvent)(new ePrepare((new PrtNamedTuple(new string[] { "transId", "key", "val" }, ((PrtInt)0), ((PrtInt)0), ((PrtInt)0)))));
            TMP_tmp2_4 = (PrtInt)(((PrtInt)((IPrtValue)currTransId)?.Clone()));
            TMP_tmp3_3 = (PrtInt)(((PrtNamedTuple)pendingWrTrans)["key"]);
            TMP_tmp4_2 = (PrtInt)(((PrtNamedTuple)pendingWrTrans)["val"]);
            TMP_tmp5_1 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "transId", "key", "val" }, TMP_tmp2_4, TMP_tmp3_3, TMP_tmp4_2)));
            SendToAllParticipants(TMP_tmp1_6, TMP_tmp5_1);
            TMP_tmp6_1 = (PMachineValue)(((PMachineValue)((IPrtValue)timer_2)?.Clone()));
            TMP_tmp7_1 = (PrtInt)(((PrtInt)100));
            GlobalFunctions.StartTimer(TMP_tmp6_1, TMP_tmp7_1, this);
            TMP_tmp8 = (PEvent)(new local_event(null));
            currentMachine.RaiseEvent((Event)TMP_tmp8);
            throw new PUnreachableCodeException();
        }
        public void Anon_6()
        {
            Coordinator currentMachine = this;
            PrtNamedTuple rTrans = (PrtNamedTuple)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
        }
        public void DoGlobalAbort()
        {
            Coordinator currentMachine = this;
            PEvent TMP_tmp0_7 = null;
            PrtInt TMP_tmp1_7 = ((PrtInt)0);
            PMachineValue TMP_tmp2_5 = null;
            PMachineValue TMP_tmp3_4 = null;
            PEvent TMP_tmp4_3 = null;
            TMP_tmp0_7 = (PEvent)(new eGlobalAbort((new PrtNamedTuple(new string[] { "transId" }, ((PrtInt)0)))));
            TMP_tmp1_7 = (PrtInt)(((PrtInt)((IPrtValue)currTransId)?.Clone()));
            SendToAllParticipants(TMP_tmp0_7, TMP_tmp1_7);
            TMP_tmp2_5 = (PMachineValue)(((PrtNamedTuple)pendingWrTrans)["client"]);
            TMP_tmp3_4 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp2_5)?.Clone()));
            TMP_tmp4_3 = (PEvent)(new eWriteTransFailed(null));
            currentMachine.SendEvent(TMP_tmp3_4, (Event)TMP_tmp4_3);
        }
        public void Anon_7()
        {
            Coordinator currentMachine = this;
            countPrepareResponses = (PrtInt)(((PrtInt)0));
        }
        public async Task Anon_8()
        {
            Coordinator currentMachine = this;
            PrtInt transId = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtBool TMP_tmp0_8 = ((PrtBool)false);
            PrtInt TMP_tmp1_8 = ((PrtInt)0);
            PrtInt TMP_tmp2_6 = ((PrtInt)0);
            PrtBool TMP_tmp3_5 = ((PrtBool)false);
            PEvent TMP_tmp4_4 = null;
            PrtInt TMP_tmp5_2 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp6_2 = (new PrtNamedTuple(new string[] { "transId" }, ((PrtInt)0)));
            PMachineValue TMP_tmp7_2 = null;
            PMachineValue TMP_tmp8_1 = null;
            PEvent TMP_tmp9 = null;
            PMachineValue TMP_tmp10 = null;
            TMP_tmp0_8 = (PrtBool)((PrtValues.SafeEquals(currTransId, transId)));
            if (TMP_tmp0_8)
            {
                TMP_tmp1_8 = (PrtInt)((countPrepareResponses) + (((PrtInt)1)));
                countPrepareResponses = TMP_tmp1_8;
                TMP_tmp2_6 = (PrtInt)(((PrtInt)(participants).Count));
                TMP_tmp3_5 = (PrtBool)((PrtValues.SafeEquals(countPrepareResponses, TMP_tmp2_6)));
                if (TMP_tmp3_5)
                {
                    TMP_tmp4_4 = (PEvent)(new eGlobalCommit((new PrtNamedTuple(new string[] { "transId" }, ((PrtInt)0)))));
                    TMP_tmp5_2 = (PrtInt)(((PrtInt)((IPrtValue)currTransId)?.Clone()));
                    TMP_tmp6_2 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "transId" }, TMP_tmp5_2)));
                    SendToAllParticipants(TMP_tmp4_4, TMP_tmp6_2);
                    TMP_tmp7_2 = (PMachineValue)(((PrtNamedTuple)pendingWrTrans)["client"]);
                    TMP_tmp8_1 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp7_2)?.Clone()));
                    TMP_tmp9 = (PEvent)(new eWriteTransSuccess(null));
                    currentMachine.SendEvent(TMP_tmp8_1, (Event)TMP_tmp9);
                    TMP_tmp10 = (PMachineValue)(((PMachineValue)((IPrtValue)timer_2)?.Clone()));
                    await GlobalFunctions.CancelTimer(TMP_tmp10, this);
                    currentMachine.PopState();
                    throw new PUnreachableCodeException();
                }
            }
        }
        public async Task Anon_9()
        {
            Coordinator currentMachine = this;
            PrtInt transId_1 = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtBool TMP_tmp0_9 = ((PrtBool)false);
            PMachineValue TMP_tmp1_9 = null;
            TMP_tmp0_9 = (PrtBool)((PrtValues.SafeEquals(currTransId, transId_1)));
            if (TMP_tmp0_9)
            {
                DoGlobalAbort();
                TMP_tmp1_9 = (PMachineValue)(((PMachineValue)((IPrtValue)timer_2)?.Clone()));
                await GlobalFunctions.CancelTimer(TMP_tmp1_9, this);
            }
        }
        public void Anon_10()
        {
            Coordinator currentMachine = this;
            DoGlobalAbort();
            currentMachine.PopState();
            throw new PUnreachableCodeException();
        }
        public void Anon_11()
        {
            Coordinator currentMachine = this;
            PModule.runtime.Logger.WriteLine("Going back to WaitForTransactions");
        }
        public void SendToAllParticipants(PEvent message, IPrtValue payload_1)
        {
            Coordinator currentMachine = this;
            PrtInt i_1 = ((PrtInt)0);
            PrtInt TMP_tmp0_10 = ((PrtInt)0);
            PrtBool TMP_tmp1_10 = ((PrtBool)false);
            PrtBool TMP_tmp2_7 = ((PrtBool)false);
            PMachineValue TMP_tmp3_6 = null;
            PMachineValue TMP_tmp4_5 = null;
            PEvent TMP_tmp5_3 = null;
            IPrtValue TMP_tmp6_3 = null;
            PrtInt TMP_tmp7_3 = ((PrtInt)0);
            i_1 = (PrtInt)(((PrtInt)0));
            TMP_tmp0_10 = (PrtInt)(((PrtInt)(participants).Count));
            TMP_tmp1_10 = (PrtBool)((i_1) < (TMP_tmp0_10));
            TMP_tmp2_7 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp1_10)?.Clone()));
            while (TMP_tmp2_7)
            {
                TMP_tmp3_6 = (PMachineValue)(((PrtSeq)participants)[i_1]);
                TMP_tmp4_5 = (PMachineValue)(((PMachineValue)((IPrtValue)TMP_tmp3_6)?.Clone()));
                TMP_tmp5_3 = (PEvent)(((PEvent)((IPrtValue)message)?.Clone()));
                TMP_tmp6_3 = (IPrtValue)(((IPrtValue)((IPrtValue)payload_1)?.Clone()));
                currentMachine.SendEvent(TMP_tmp4_5, (Event)TMP_tmp5_3, TMP_tmp6_3);
                TMP_tmp7_3 = (PrtInt)((i_1) + (((PrtInt)1)));
                i_1 = TMP_tmp7_3;
                TMP_tmp0_10 = (PrtInt)(((PrtInt)(participants).Count));
                TMP_tmp1_10 = (PrtBool)((i_1) < (TMP_tmp0_10));
                TMP_tmp2_7 = (PrtBool)(((PrtBool)((IPrtValue)TMP_tmp1_10)?.Clone()));
            }
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_1))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_4))]
        class Init_1 : MachineState
        {
        }
        [OnEventDoAction(typeof(eWriteTransaction), nameof(Anon_5))]
        [OnEventDoAction(typeof(eReadTransaction), nameof(Anon_6))]
        [OnEventPushState(typeof(local_event), typeof(WaitForPrepareResponses))]
        [IgnoreEvents(typeof(ePrepareSuccess), typeof(ePrepareFailed))]
        class WaitForTransactions : MachineState
        {
        }
        [OnEntry(nameof(Anon_7))]
        [OnEventDoAction(typeof(ePrepareSuccess), nameof(Anon_8))]
        [OnEventDoAction(typeof(ePrepareFailed), nameof(Anon_9))]
        [OnEventDoAction(typeof(eTimeOut), nameof(Anon_10))]
        [DeferEvents(typeof(eWriteTransaction))]
        [OnExit(nameof(Anon_11))]
        class WaitForPrepareResponses : MachineState
        {
        }
    }
    internal partial class Participant : PMachine
    {
        private PMachineValue coordinator_1 = null;
        private PrtMap kvStore = new PrtMap();
        private PrtNamedTuple pendingWrTrans_1 = (new PrtNamedTuple(new string[] { "transId", "key", "val" }, ((PrtInt)0), ((PrtInt)0), ((PrtInt)0)));
        private PrtInt lastTransId = ((PrtInt)0);
        public class ConstructorEvent : PEvent { public ConstructorEvent(PMachineValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PMachineValue)value); }
        public Participant()
        {
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
            this.sends.Add(nameof(eReadTransUnAvailable));
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
            this.receives.Add(nameof(eReadTransUnAvailable));
            this.receives.Add(nameof(eReadTransaction));
            this.receives.Add(nameof(eStartTimer));
            this.receives.Add(nameof(eTimeOut));
            this.receives.Add(nameof(eWriteTransFailed));
            this.receives.Add(nameof(eWriteTransSuccess));
            this.receives.Add(nameof(eWriteTransaction));
            this.receives.Add(nameof(PHalt));
            this.receives.Add(nameof(local_event));
        }

        public void Anon_12()
        {
            Participant currentMachine = this;
            PMachineValue payload_2 = (PMachineValue)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            coordinator_1 = (PMachineValue)(((PMachineValue)((IPrtValue)payload_2)?.Clone()));
            lastTransId = (PrtInt)(((PrtInt)0));
            currentMachine.GotoState<Participant.WaitForRequests>();
            throw new PUnreachableCodeException();
        }
        public void Anon_13()
        {
            Participant currentMachine = this;
            PrtInt transId_2 = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt TMP_tmp0_11 = ((PrtInt)0);
            PrtBool TMP_tmp1_11 = ((PrtBool)false);
            PrtInt TMP_tmp2_8 = ((PrtInt)0);
            PrtBool TMP_tmp3_7 = ((PrtBool)false);
            TMP_tmp0_11 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
            TMP_tmp1_11 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_11, transId_2)));
            currentMachine.Assert(TMP_tmp1_11, "");
            TMP_tmp2_8 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
            TMP_tmp3_7 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp2_8, transId_2)));
            if (TMP_tmp3_7)
            {
                lastTransId = (PrtInt)(((PrtInt)((IPrtValue)transId_2)?.Clone()));
            }
        }
        public void Anon_14()
        {
            Participant currentMachine = this;
            PrtInt transId_3 = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt TMP_tmp0_12 = ((PrtInt)0);
            PrtBool TMP_tmp1_12 = ((PrtBool)false);
            PrtInt TMP_tmp2_9 = ((PrtInt)0);
            PrtBool TMP_tmp3_8 = ((PrtBool)false);
            PrtInt TMP_tmp4_6 = ((PrtInt)0);
            PrtInt TMP_tmp5_4 = ((PrtInt)0);
            PMachineValue TMP_tmp6_4 = null;
            PrtInt TMP_tmp7_4 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp8_2 = (new PrtNamedTuple(new string[] { "participant", "transId" }, null, ((PrtInt)0)));
            TMP_tmp0_12 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
            TMP_tmp1_12 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_12, transId_3)));
            currentMachine.Assert(TMP_tmp1_12, "");
            TMP_tmp2_9 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
            TMP_tmp3_8 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp2_9, transId_3)));
            if (TMP_tmp3_8)
            {
                TMP_tmp4_6 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["key"]);
                TMP_tmp5_4 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["val"]);
                ((PrtMap)kvStore)[TMP_tmp4_6] = TMP_tmp5_4;
                TMP_tmp6_4 = (PMachineValue)(currentMachine.self);
                TMP_tmp7_4 = (PrtInt)(((PrtInt)((IPrtValue)transId_3)?.Clone()));
                TMP_tmp8_2 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "participant", "transId" }, TMP_tmp6_4, TMP_tmp7_4)));
                currentMachine.Announce((Event)new eMonitor_LocalCommit((new PrtNamedTuple(new string[] { "parcipant", "transId" }, null, ((PrtInt)0)))), TMP_tmp8_2);
                lastTransId = (PrtInt)(((PrtInt)((IPrtValue)transId_3)?.Clone()));
            }
        }
        public void Anon_15()
        {
            Participant currentMachine = this;
            PrtNamedTuple prepareReq = (PrtNamedTuple)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtInt TMP_tmp0_13 = ((PrtInt)0);
            PrtBool TMP_tmp1_13 = ((PrtBool)false);
            PrtBool TMP_tmp2_10 = ((PrtBool)false);
            PMachineValue TMP_tmp3_9 = null;
            PEvent TMP_tmp4_7 = null;
            PrtInt TMP_tmp5_5 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp6_5 = (new PrtNamedTuple(new string[] { "transId" }, ((PrtInt)0)));
            PMachineValue TMP_tmp7_5 = null;
            PEvent TMP_tmp8_3 = null;
            PrtInt TMP_tmp9_1 = ((PrtInt)0);
            PrtNamedTuple TMP_tmp10_1 = (new PrtNamedTuple(new string[] { "transId" }, ((PrtInt)0)));
            pendingWrTrans_1 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)prepareReq)?.Clone()));
            TMP_tmp0_13 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
            TMP_tmp1_13 = (PrtBool)((TMP_tmp0_13) > (lastTransId));
            currentMachine.Assert(TMP_tmp1_13, "");
            TMP_tmp2_10 = (PrtBool)(((PrtBool)currentMachine.Random()));
            if (TMP_tmp2_10)
            {
                TMP_tmp3_9 = (PMachineValue)(((PMachineValue)((IPrtValue)coordinator_1)?.Clone()));
                TMP_tmp4_7 = (PEvent)(new ePrepareSuccess((new PrtNamedTuple(new string[] { "transId" }, ((PrtInt)0)))));
                TMP_tmp5_5 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
                TMP_tmp6_5 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "transId" }, TMP_tmp5_5)));
                currentMachine.SendEvent(TMP_tmp3_9, (Event)TMP_tmp4_7, TMP_tmp6_5);
            }
            else
            {
                TMP_tmp7_5 = (PMachineValue)(((PMachineValue)((IPrtValue)coordinator_1)?.Clone()));
                TMP_tmp8_3 = (PEvent)(new ePrepareFailed((new PrtNamedTuple(new string[] { "transId" }, ((PrtInt)0)))));
                TMP_tmp9_1 = (PrtInt)(((PrtNamedTuple)pendingWrTrans_1)["transId"]);
                TMP_tmp10_1 = (PrtNamedTuple)((new PrtNamedTuple(new string[] { "transId" }, TMP_tmp9_1)));
                currentMachine.SendEvent(TMP_tmp7_5, (Event)TMP_tmp8_3, TMP_tmp10_1);
            }
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_2))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_12))]
        class Init_2 : MachineState
        {
        }
        [OnEventDoAction(typeof(eGlobalAbort), nameof(Anon_13))]
        [OnEventDoAction(typeof(eGlobalCommit), nameof(Anon_14))]
        [OnEventDoAction(typeof(ePrepare), nameof(Anon_15))]
        class WaitForRequests : MachineState
        {
        }
    }
    internal partial class Main : PMachine
    {
        public class ConstructorEvent : PEvent { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Main()
        {
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
            this.sends.Add(nameof(eReadTransUnAvailable));
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
            this.receives.Add(nameof(eReadTransUnAvailable));
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
        }

        public void Anon_16()
        {
            Main currentMachine = this;
            PMachineValue coord = null;
            PrtInt TMP_tmp0_14 = ((PrtInt)0);
            PMachineValue TMP_tmp1_14 = null;
            PMachineValue TMP_tmp2_11 = null;
            PMachineValue TMP_tmp3_10 = null;
            TMP_tmp0_14 = (PrtInt)(((PrtInt)2));
            TMP_tmp1_14 = (PMachineValue)(currentMachine.CreateInterface<I_Coordinator>(currentMachine, TMP_tmp0_14));
            coord = (PMachineValue)TMP_tmp1_14;
            TMP_tmp2_11 = (PMachineValue)(((PMachineValue)((IPrtValue)coord)?.Clone()));
            currentMachine.CreateInterface<I_Client>(currentMachine, TMP_tmp2_11);
            TMP_tmp3_10 = (PMachineValue)(((PMachineValue)((IPrtValue)coord)?.Clone()));
            currentMachine.CreateInterface<I_Client>(currentMachine, TMP_tmp3_10);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_3))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_16))]
        class Init_3 : MachineState
        {
        }
    }
    internal partial class Timer : PMachine
    {
        private PMachineValue target = null;
        public class ConstructorEvent : PEvent { public ConstructorEvent(PMachineValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PMachineValue)value); }
        public Timer()
        {
            this.sends.Add(nameof(eCancelTimerFailed));
            this.sends.Add(nameof(eCancelTimerSuccess));
            this.sends.Add(nameof(eTimeOut));
            this.receives.Add(nameof(eCancelTimer));
            this.receives.Add(nameof(eStartTimer));
        }

        public void Anon_17()
        {
            Timer currentMachine = this;
            PMachineValue payload_3 = (PMachineValue)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            target = (PMachineValue)(((PMachineValue)((IPrtValue)payload_3)?.Clone()));
            currentMachine.GotoState<Timer.WaitForStartTimer>();
            throw new PUnreachableCodeException();
        }
        public void Anon_18()
        {
            Timer currentMachine = this;
            PrtInt payload_4 = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PrtBool TMP_tmp0_15 = ((PrtBool)false);
            PMachineValue TMP_tmp1_15 = null;
            PEvent TMP_tmp2_12 = null;
            TMP_tmp0_15 = (PrtBool)(((PrtBool)currentMachine.Random()));
            if (TMP_tmp0_15)
            {
                TMP_tmp1_15 = (PMachineValue)(((PMachineValue)((IPrtValue)target)?.Clone()));
                TMP_tmp2_12 = (PEvent)(new eTimeOut(null));
                currentMachine.SendEvent(TMP_tmp1_15, (Event)TMP_tmp2_12);
                currentMachine.GotoState<Timer.WaitForStartTimer>();
                throw new PUnreachableCodeException();
            }
        }
        public void Anon_19()
        {
            Timer currentMachine = this;
            PrtBool TMP_tmp0_16 = ((PrtBool)false);
            PMachineValue TMP_tmp1_16 = null;
            PEvent TMP_tmp2_13 = null;
            PMachineValue TMP_tmp3_11 = null;
            PEvent TMP_tmp4_8 = null;
            PMachineValue TMP_tmp5_6 = null;
            PEvent TMP_tmp6_6 = null;
            TMP_tmp0_16 = (PrtBool)(((PrtBool)currentMachine.Random()));
            if (TMP_tmp0_16)
            {
                TMP_tmp1_16 = (PMachineValue)(((PMachineValue)((IPrtValue)target)?.Clone()));
                TMP_tmp2_13 = (PEvent)(new eCancelTimerFailed(null));
                currentMachine.SendEvent(TMP_tmp1_16, (Event)TMP_tmp2_13);
                TMP_tmp3_11 = (PMachineValue)(((PMachineValue)((IPrtValue)target)?.Clone()));
                TMP_tmp4_8 = (PEvent)(new eTimeOut(null));
                currentMachine.SendEvent(TMP_tmp3_11, (Event)TMP_tmp4_8);
            }
            else
            {
                TMP_tmp5_6 = (PMachineValue)(((PMachineValue)((IPrtValue)target)?.Clone()));
                TMP_tmp6_6 = (PEvent)(new eCancelTimerSuccess(null));
                currentMachine.SendEvent(TMP_tmp5_6, (Event)TMP_tmp6_6);
            }
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_4))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_17))]
        class Init_4 : MachineState
        {
        }
        [OnEventGotoState(typeof(eStartTimer), typeof(TimerStarted))]
        [IgnoreEvents(typeof(eCancelTimer))]
        class WaitForStartTimer : MachineState
        {
        }
        [OnEntry(nameof(Anon_18))]
        [OnEventGotoState(typeof(eCancelTimer), typeof(WaitForStartTimer), nameof(Anon_19))]
        class TimerStarted : MachineState
        {
        }
    }
    internal partial class Atomicity : PMonitor
    {
        private PrtMap receivedLocalCommits = new PrtMap();
        private PrtInt numParticipants_1 = ((PrtInt)0);
        static Atomicity()
        {
            observes.Add(nameof(eMonitor_AtomicityInitialize));
            observes.Add(nameof(eMonitor_LocalCommit));
            observes.Add(nameof(eWriteTransSuccess));
        }

        public void Anon_20()
        {
            Atomicity currentMachine = this;
            PrtInt n = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            numParticipants_1 = (PrtInt)(((PrtInt)((IPrtValue)n)?.Clone()));
        }
        public void Anon_21()
        {
            Atomicity currentMachine = this;
            PrtNamedTuple payload_5 = (PrtNamedTuple)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PMachineValue TMP_tmp0_17 = null;
            PrtBool TMP_tmp1_17 = ((PrtBool)false);
            PrtBool TMP_tmp2_14 = ((PrtBool)false);
            PMachineValue TMP_tmp3_12 = null;
            PrtInt TMP_tmp4_9 = ((PrtInt)0);
            TMP_tmp0_17 = (PMachineValue)(((PrtNamedTuple)payload_5)["parcipant"]);
            TMP_tmp1_17 = (PrtBool)(((PrtBool)(receivedLocalCommits).ContainsKey(TMP_tmp0_17)));
            TMP_tmp2_14 = (PrtBool)(!(TMP_tmp1_17));
            currentMachine.Assert(TMP_tmp2_14, "");
            TMP_tmp3_12 = (PMachineValue)(((PrtNamedTuple)payload_5)["parcipant"]);
            TMP_tmp4_9 = (PrtInt)(((PrtNamedTuple)payload_5)["transId"]);
            ((PrtMap)receivedLocalCommits)[TMP_tmp3_12] = TMP_tmp4_9;
        }
        public void Anon_22()
        {
            Atomicity currentMachine = this;
            PrtInt TMP_tmp0_18 = ((PrtInt)0);
            PrtBool TMP_tmp1_18 = ((PrtBool)false);
            PrtMap TMP_tmp2_15 = new PrtMap();
            TMP_tmp0_18 = (PrtInt)(((PrtInt)(receivedLocalCommits).Count));
            TMP_tmp1_18 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp0_18, numParticipants_1)));
            currentMachine.Assert(TMP_tmp1_18, "");
            TMP_tmp2_15 = (PrtMap)(new PrtMap());
            receivedLocalCommits = TMP_tmp2_15;
        }
        [Start]
        [OnEventGotoState(typeof(eMonitor_AtomicityInitialize), typeof(WaitForEvents), nameof(Anon_20))]
        class Init_5 : MonitorState
        {
        }
        [OnEventDoAction(typeof(eMonitor_LocalCommit), nameof(Anon_21))]
        [OnEventDoAction(typeof(eWriteTransSuccess), nameof(Anon_22))]
        class WaitForEvents : MonitorState
        {
        }
    }
    internal partial class Progress : PMonitor
    {
        static Progress()
        {
            observes.Add(nameof(eWriteTransFailed));
            observes.Add(nameof(eWriteTransSuccess));
            observes.Add(nameof(eWriteTransaction));
        }

        [Start]
        [OnEventGotoState(typeof(eWriteTransaction), typeof(WaitForOperationToFinish))]
        class Init_6 : MonitorState
        {
        }
        [Hot]
        [OnEventGotoState(typeof(eWriteTransSuccess), typeof(Init_6))]
        [OnEventGotoState(typeof(eWriteTransFailed), typeof(Init_6))]
        class WaitForOperationToFinish : MonitorState
        {
        }
    }
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap.Clear();
            PModule.linkMap[nameof(I_Client)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Coordinator)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Coordinator)].Add(nameof(I_Participant), nameof(I_Participant));
            PModule.linkMap[nameof(I_Coordinator)].Add(nameof(I_Timer), nameof(I_Timer));
            PModule.linkMap[nameof(I_Participant)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_Client), nameof(I_Client));
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_Coordinator), nameof(I_Coordinator));
            PModule.linkMap[nameof(I_Timer)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Clear();
            PModule.interfaceDefinitionMap.Add(nameof(I_Client), typeof(Client));
            PModule.interfaceDefinitionMap.Add(nameof(I_Coordinator), typeof(Coordinator));
            PModule.interfaceDefinitionMap.Add(nameof(I_Participant), typeof(Participant));
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_Timer), typeof(Timer));
        }

        public static void InitializeMonitorObserves()
        {
            PModule.monitorObserves.Clear();
        }

        public static void InitializeMonitorMap(IMachineRuntime runtime)
        {
            PModule.monitorMap.Clear();
        }


        [Microsoft.PSharp.Test]
        public static void Execute(IMachineRuntime runtime)
        {
            //runtime.SetLogger(new PLogger());
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            PHelper.InitializeEnums();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof(Main)));
        }
    }
    public class I_Client : PMachineValue
    {
        public I_Client(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_Coordinator : PMachineValue
    {
        public I_Coordinator(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_Participant : PMachineValue
    {
        public I_Participant(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_Main : PMachineValue
    {
        public I_Main(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_Timer : PMachineValue
    {
        public I_Timer(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.Clear();
            PInterfaces.AddInterface(nameof(I_Client), nameof(eCancelTimer), nameof(eCancelTimerFailed), nameof(eCancelTimerSuccess), nameof(eGlobalAbort), nameof(eGlobalCommit), nameof(eMonitor_AtomicityInitialize), nameof(eMonitor_LocalCommit), nameof(ePrepare), nameof(ePrepareFailed), nameof(ePrepareSuccess), nameof(eReadTransFailed), nameof(eReadTransSuccess), nameof(eReadTransUnAvailable), nameof(eReadTransaction), nameof(eStartTimer), nameof(eTimeOut), nameof(eWriteTransFailed), nameof(eWriteTransSuccess), nameof(eWriteTransaction), nameof(PHalt), nameof(local_event));
            PInterfaces.AddInterface(nameof(I_Coordinator), nameof(ePrepareFailed), nameof(ePrepareSuccess), nameof(eReadTransaction), nameof(eTimeOut), nameof(eWriteTransaction));
            PInterfaces.AddInterface(nameof(I_Participant), nameof(eCancelTimer), nameof(eCancelTimerFailed), nameof(eCancelTimerSuccess), nameof(eGlobalAbort), nameof(eGlobalCommit), nameof(eMonitor_AtomicityInitialize), nameof(eMonitor_LocalCommit), nameof(ePrepare), nameof(ePrepareFailed), nameof(ePrepareSuccess), nameof(eReadTransFailed), nameof(eReadTransSuccess), nameof(eReadTransUnAvailable), nameof(eReadTransaction), nameof(eStartTimer), nameof(eTimeOut), nameof(eWriteTransFailed), nameof(eWriteTransSuccess), nameof(eWriteTransaction), nameof(PHalt), nameof(local_event));
            PInterfaces.AddInterface(nameof(I_Main), nameof(eCancelTimer), nameof(eCancelTimerFailed), nameof(eCancelTimerSuccess), nameof(eGlobalAbort), nameof(eGlobalCommit), nameof(eMonitor_AtomicityInitialize), nameof(eMonitor_LocalCommit), nameof(ePrepare), nameof(ePrepareFailed), nameof(ePrepareSuccess), nameof(eReadTransFailed), nameof(eReadTransSuccess), nameof(eReadTransUnAvailable), nameof(eReadTransaction), nameof(eStartTimer), nameof(eTimeOut), nameof(eWriteTransFailed), nameof(eWriteTransSuccess), nameof(eWriteTransaction), nameof(PHalt), nameof(local_event));
            PInterfaces.AddInterface(nameof(I_Timer), nameof(eCancelTimer), nameof(eStartTimer));
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
