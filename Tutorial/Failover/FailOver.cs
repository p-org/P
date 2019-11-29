using Microsoft.Coyote;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PrtSharp;
using PrtSharp.Values;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 162, 219, 414
namespace FailOver
{
    public static partial class GlobalFunctions_FailOver {}
    // TODO: EnumElem State0
    // TODO: EnumElem State1
    public enum MyState : long
    {
        State0 = 0,
        State1 = 1,
    }
    internal class eDoOpI : PEvent<IPrtValue>
    {
        static eDoOpI() { AssertVal = -1; AssumeVal = -1;}
        public eDoOpI() : base() {}
        public eDoOpI (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eDoOpI();}
    }
    internal class eDoOpJ : PEvent<IPrtValue>
    {
        static eDoOpJ() { AssertVal = -1; AssumeVal = -1;}
        public eDoOpJ() : base() {}
        public eDoOpJ (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eDoOpJ();}
    }
    internal class eQueryState : PEvent<PMachineValue>
    {
        static eQueryState() { AssertVal = -1; AssumeVal = -1;}
        public eQueryState() : base() {}
        public eQueryState (PMachineValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eQueryState();}
    }
    internal class eQueryStateResponse : PEvent<MyState>
    {
        static eQueryStateResponse() { AssertVal = -1; AssumeVal = -1;}
        public eQueryStateResponse() : base() {}
        public eQueryStateResponse (MyState payload): base(payload){ }
        public override IPrtValue Clone() { return new eQueryStateResponse();}
    }
    internal class eUpdateToState0 : PEvent<IPrtValue>
    {
        static eUpdateToState0() { AssertVal = -1; AssumeVal = -1;}
        public eUpdateToState0() : base() {}
        public eUpdateToState0 (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eUpdateToState0();}
    }
    internal class eUpdateToState1 : PEvent<IPrtValue>
    {
        static eUpdateToState1() { AssertVal = -1; AssumeVal = -1;}
        public eUpdateToState1() : base() {}
        public eUpdateToState1 (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new eUpdateToState1();}
    }
    internal class TestDriver : PMachine
    {
        private PMachineValue reliableStorage = null;
        private PMachineValue service = null;
        public class ConstructorEvent : PEvent<IPrtValue>{public ConstructorEvent(IPrtValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public TestDriver() {
            this.sends.Add(nameof(eDoOpI));
            this.sends.Add(nameof(eDoOpJ));
            this.sends.Add(nameof(eQueryState));
            this.sends.Add(nameof(eQueryStateResponse));
            this.sends.Add(nameof(eUpdateToState0));
            this.sends.Add(nameof(eUpdateToState1));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(eDoOpI));
            this.receives.Add(nameof(eDoOpJ));
            this.receives.Add(nameof(eQueryState));
            this.receives.Add(nameof(eQueryStateResponse));
            this.receives.Add(nameof(eUpdateToState0));
            this.receives.Add(nameof(eUpdateToState1));
            this.receives.Add(nameof(PHalt));
            this.creates.Add(nameof(I_FaultTolerantMachine));
            this.creates.Add(nameof(I_ReliableStorageMachine));
            this.creates.Add(nameof(I_ServiceMachine));
        }
        
        public void Anon()
        {
            TestDriver currentMachine = this;
            PMachineValue m = null;
            PMachineValue TMP_tmp0 = null;
            PMachineValue TMP_tmp1 = null;
            PMachineValue TMP_tmp2 = null;
            PMachineValue TMP_tmp3 = null;
            PMachineValue TMP_tmp4 = null;
            PMachineValue TMP_tmp5 = null;
            IEventWithPayload TMP_tmp6 = null;
            PMachineValue TMP_tmp7 = null;
            PMachineValue TMP_tmp8 = null;
            PMachineValue TMP_tmp9 = null;
            TMP_tmp0 = currentMachine.CreateInterface<I_ReliableStorageMachine>( currentMachine);
            reliableStorage = TMP_tmp0;
            TMP_tmp1 = currentMachine.CreateInterface<I_ServiceMachine>( currentMachine);
            service = TMP_tmp1;
            TMP_tmp2 = ((PMachineValue)((IPrtValue)service)?.Clone());
            TMP_tmp3 = ((PMachineValue)((IPrtValue)reliableStorage)?.Clone());
            TMP_tmp4 = currentMachine.CreateInterface<I_FaultTolerantMachine>( currentMachine, TMP_tmp2);
            m = (PMachineValue)TMP_tmp4;
            TMP_tmp5 = ((PMachineValue)((IPrtValue)m)?.Clone());
            TMP_tmp6 = new PHalt(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp5, (Event)TMP_tmp6);
            TMP_tmp7 = ((PMachineValue)((IPrtValue)service)?.Clone());
            TMP_tmp8 = ((PMachineValue)((IPrtValue)reliableStorage)?.Clone());
            TMP_tmp9 = currentMachine.CreateInterface<I_FaultTolerantMachine>( currentMachine, TMP_tmp7);
            m = (PMachineValue)TMP_tmp9;
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon))]
        class Init : MachineState
        {
        }
    }
    internal class FaultTolerantMachine : PMachine
    {
        private PMachineValue service_1 = null;
        private PMachineValue reliableStorage_1 = null;
        public class ConstructorEvent : PEvent<PrtTuple<PMachineValue, PMachineValue>>{public ConstructorEvent(PrtTuple<PMachineValue, PMachineValue> val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PrtTuple<PMachineValue, PMachineValue>)value); }
        public FaultTolerantMachine() {
            this.sends.Add(nameof(eDoOpI));
            this.sends.Add(nameof(eDoOpJ));
            this.sends.Add(nameof(eQueryState));
            this.sends.Add(nameof(eQueryStateResponse));
            this.sends.Add(nameof(eUpdateToState0));
            this.sends.Add(nameof(eUpdateToState1));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(eQueryStateResponse));
            this.receives.Add(nameof(PHalt));
        }
        
        public async Task Anon_1(Event currentMachine_dequeuedEvent)
        {
            FaultTolerantMachine currentMachine = this;
            PrtTuple<PMachineValue, PMachineValue> arg = ((PEvent<PrtTuple<PMachineValue, PMachineValue>>)currentMachine_dequeuedEvent).PayloadT;
            PMachineValue TMP_tmp0_1 = null;
            PMachineValue TMP_tmp1_1 = null;
            PMachineValue TMP_tmp2_1 = null;
            IEventWithPayload TMP_tmp3_1 = null;
            PMachineValue TMP_tmp4_1 = null;
            PrtBool TMP_tmp5_1 = ((PrtBool)false);
            TMP_tmp0_1 = arg.Item1;
            service_1 = TMP_tmp0_1;
            TMP_tmp1_1 = arg.Item2;
            reliableStorage_1 = TMP_tmp1_1;
            TMP_tmp2_1 = ((PMachineValue)((IPrtValue)reliableStorage_1)?.Clone());
            TMP_tmp3_1 = new eQueryState(null);
            TMP_tmp4_1 = currentMachine.self;
            currentMachine.SendEvent(currentMachine, TMP_tmp2_1, (Event)TMP_tmp3_1, TMP_tmp4_1);
            var PGEN_recvEvent = await currentMachine.ReceiveEvent(typeof(eQueryStateResponse));
            switch (PGEN_recvEvent) {
                case eQueryStateResponse PGEN_evt: {
                    var s = PGEN_evt.PayloadT;
                    TMP_tmp5_1 = ((long)s) == ((long)((PrtInt)(long)MyState.State0));
                    if (TMP_tmp5_1)
                    {
                        currentMachine.GotoState<FaultTolerantMachine.State0>();
                        throw new PUnreachableCodeException();
                    }
                    else
                    {
                        currentMachine.GotoState<FaultTolerantMachine.State1>();
                        throw new PUnreachableCodeException();
                    }
                } break;
            }
        }
        public async Task Anon_2()
        {
            FaultTolerantMachine currentMachine = this;
            PMachineValue TMP_tmp0_2 = null;
            IEventWithPayload TMP_tmp1_2 = null;
            PMachineValue TMP_tmp2_2 = null;
            IEventWithPayload TMP_tmp3_2 = null;
            TMP_tmp0_2 = ((PMachineValue)((IPrtValue)service_1)?.Clone());
            TMP_tmp1_2 = new eDoOpI(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_2, (Event)TMP_tmp1_2);
            await PossiblyRaiseHalt();
            TMP_tmp2_2 = ((PMachineValue)((IPrtValue)reliableStorage_1)?.Clone());
            TMP_tmp3_2 = new eUpdateToState1(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp2_2, (Event)TMP_tmp3_2);
            currentMachine.GotoState<FaultTolerantMachine.State1>();
            throw new PUnreachableCodeException();
        }
        public async Task Anon_3()
        {
            FaultTolerantMachine currentMachine = this;
            PMachineValue TMP_tmp0_3 = null;
            IEventWithPayload TMP_tmp1_3 = null;
            PMachineValue TMP_tmp2_3 = null;
            IEventWithPayload TMP_tmp3_3 = null;
            TMP_tmp0_3 = ((PMachineValue)((IPrtValue)service_1)?.Clone());
            TMP_tmp1_3 = new eDoOpJ(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_3, (Event)TMP_tmp1_3);
            await PossiblyRaiseHalt();
            TMP_tmp2_3 = ((PMachineValue)((IPrtValue)reliableStorage_1)?.Clone());
            TMP_tmp3_3 = new eUpdateToState0(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp2_3, (Event)TMP_tmp3_3);
            currentMachine.GotoState<FaultTolerantMachine.State0>();
            throw new PUnreachableCodeException();
        }
        public async Task PossiblyRaiseHalt()
        {
            FaultTolerantMachine currentMachine = this;
            IEventWithPayload TMP_tmp0_4 = null;
            var PGEN_recvEvent_1 = await currentMachine.ReceiveEvent(typeof(PHalt), typeof(DefaultEvent));
            switch (PGEN_recvEvent_1) {
                case PHalt PGEN_evt_1: {
                    TMP_tmp0_4 = new PHalt(null);
                    currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp0_4);
                    throw new PUnreachableCodeException();
                } break;
                case DefaultEvent PGEN_evt_2: {
                } break;
            }
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_1))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_1))]
        class Init_1 : MachineState
        {
        }
        [OnEntry(nameof(Anon_2))]
        class State0_1 : MachineState
        {
        }
        [OnEntry(nameof(Anon_3))]
        class State1_1 : MachineState
        {
        }
    }
    internal class ServiceMachine : PMachine
    {
        private PrtInt i = ((PrtInt)0);
        private PrtInt j = ((PrtInt)0);
        private PrtBool donei = ((PrtBool)false);
        private PrtBool donej = ((PrtBool)false);
        public class ConstructorEvent : PEvent<IPrtValue>{public ConstructorEvent(IPrtValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public ServiceMachine() {
            this.sends.Add(nameof(eDoOpI));
            this.sends.Add(nameof(eDoOpJ));
            this.sends.Add(nameof(eQueryState));
            this.sends.Add(nameof(eQueryStateResponse));
            this.sends.Add(nameof(eUpdateToState0));
            this.sends.Add(nameof(eUpdateToState1));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(eDoOpI));
            this.receives.Add(nameof(eDoOpJ));
            this.receives.Add(nameof(eQueryState));
            this.receives.Add(nameof(eQueryStateResponse));
            this.receives.Add(nameof(eUpdateToState0));
            this.receives.Add(nameof(eUpdateToState1));
            this.receives.Add(nameof(PHalt));
        }
        
        public void Anon_4()
        {
            ServiceMachine currentMachine = this;
            PrtBool TMP_tmp0_5 = ((PrtBool)false);
            PrtInt TMP_tmp1_4 = ((PrtInt)0);
            PrtInt TMP_tmp2_4 = ((PrtInt)0);
            PrtBool TMP_tmp3_4 = ((PrtBool)false);
            TMP_tmp0_5 = !(donei);
            if (TMP_tmp0_5)
            {
                TMP_tmp1_4 = (i) + (((PrtInt)1));
                i = TMP_tmp1_4;
                donei = ((PrtBool)true);
            }
            donej = ((PrtBool)false);
            TMP_tmp2_4 = (j) + (((PrtInt)1));
            TMP_tmp3_4 = (i) == (TMP_tmp2_4);
            currentMachine.Assert(TMP_tmp3_4,"");
        }
        public void Anon_5()
        {
            ServiceMachine currentMachine = this;
            PrtBool TMP_tmp0_6 = ((PrtBool)false);
            PrtInt TMP_tmp1_5 = ((PrtInt)0);
            PrtBool TMP_tmp2_5 = ((PrtBool)false);
            TMP_tmp0_6 = !(donej);
            if (TMP_tmp0_6)
            {
                TMP_tmp1_5 = (j) + (((PrtInt)1));
                j = TMP_tmp1_5;
                donej = ((PrtBool)true);
            }
            donei = ((PrtBool)false);
            TMP_tmp2_5 = (i) == (j);
            currentMachine.Assert(TMP_tmp2_5,"");
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_2))]
        class __InitState__ : MachineState { }
        
        [OnEventDoAction(typeof(eDoOpI), nameof(Anon_4))]
        [OnEventDoAction(typeof(eDoOpJ), nameof(Anon_5))]
        class Init_2 : MachineState
        {
        }
    }
    internal class ReliableStorageMachine : PMachine
    {
        private MyState s_1 = (MyState)(0);
        public class ConstructorEvent : PEvent<IPrtValue>{public ConstructorEvent(IPrtValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public ReliableStorageMachine() {
            this.sends.Add(nameof(eDoOpI));
            this.sends.Add(nameof(eDoOpJ));
            this.sends.Add(nameof(eQueryState));
            this.sends.Add(nameof(eQueryStateResponse));
            this.sends.Add(nameof(eUpdateToState0));
            this.sends.Add(nameof(eUpdateToState1));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(eDoOpI));
            this.receives.Add(nameof(eDoOpJ));
            this.receives.Add(nameof(eQueryState));
            this.receives.Add(nameof(eQueryStateResponse));
            this.receives.Add(nameof(eUpdateToState0));
            this.receives.Add(nameof(eUpdateToState1));
            this.receives.Add(nameof(PHalt));
        }
        
        public void Anon_6()
        {
            ReliableStorageMachine currentMachine = this;
            s_1 = ((PrtInt)(long)MyState.State0);
        }
        public void Anon_7(Event currentMachine_dequeuedEvent)
        {
            ReliableStorageMachine currentMachine = this;
            PMachineValue m_1 = ((PEvent<PMachineValue>)currentMachine_dequeuedEvent).PayloadT;
            PMachineValue TMP_tmp0_7 = null;
            IEventWithPayload TMP_tmp1_6 = null;
            MyState TMP_tmp2_6 = (MyState)(0);
            TMP_tmp0_7 = ((PMachineValue)((IPrtValue)m_1)?.Clone());
            TMP_tmp1_6 = new eQueryStateResponse((MyState)(0));
            TMP_tmp2_6 = ((MyState)((IPrtValue)s_1)?.Clone());
            currentMachine.SendEvent(currentMachine, TMP_tmp0_7, (Event)TMP_tmp1_6, TMP_tmp2_6);
        }
        public void Anon_8()
        {
            ReliableStorageMachine currentMachine = this;
            s_1 = ((PrtInt)(long)MyState.State0);
        }
        public void Anon_9()
        {
            ReliableStorageMachine currentMachine = this;
            s_1 = ((PrtInt)(long)MyState.State1);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_3))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_6))]
        [OnEventDoAction(typeof(eQueryState), nameof(Anon_7))]
        [OnEventDoAction(typeof(eUpdateToState0), nameof(Anon_8))]
        [OnEventDoAction(typeof(eUpdateToState1), nameof(Anon_9))]
        class Init_3 : MachineState
        {
        }
    }
    // TODO: TypeDef Pair
    public class Test0 {
        public static void InitializeLinkMap() {
            PModule.linkMap[nameof(I_TestDriver)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_TestDriver)].Add(nameof(I_FaultTolerantMachine), nameof(I_FaultTolerantMachine));
            PModule.linkMap[nameof(I_TestDriver)].Add(nameof(I_ReliableStorageMachine), nameof(I_ReliableStorageMachine));
            PModule.linkMap[nameof(I_TestDriver)].Add(nameof(I_ServiceMachine), nameof(I_ServiceMachine));
            PModule.linkMap[nameof(I_FaultTolerantMachine)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_ReliableStorageMachine)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_ServiceMachine)] = new Dictionary<string, string>();
        }
        
        public static void InitializeInterfaceDefMap() {
            PModule.interfaceDefinitionMap.Add(nameof(I_TestDriver), typeof(TestDriver));
            PModule.interfaceDefinitionMap.Add(nameof(I_FaultTolerantMachine), typeof(FaultTolerantMachine));
            PModule.interfaceDefinitionMap.Add(nameof(I_ReliableStorageMachine), typeof(ReliableStorageMachine));
            PModule.interfaceDefinitionMap.Add(nameof(I_ServiceMachine), typeof(ServiceMachine));
        }
        
        public static void InitializeMonitorObserves() {
        }
        
        public static void InitializeMonitorMap(IActorRuntime runtime) {
        }
        
        
        [Microsoft.Coyote.TestingServices.Test]
        public static void Execute(IActorRuntime runtime) {
            runtime.SetLogFormatter(new PLogFormatter());
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateActor(typeof(_GodMachine), new _GodMachine.Config(typeof(TestDriver)));
        }
    }
    public class I_TestDriver : PMachineValue {
        public I_TestDriver (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_FaultTolerantMachine : PMachineValue {
        public I_FaultTolerantMachine (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_ServiceMachine : PMachineValue {
        public I_ServiceMachine (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_ReliableStorageMachine : PMachineValue {
        public I_ReliableStorageMachine (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public partial class PHelper {
        public static void InitializeInterfaces() {
            PInterfaces.AddInterface(nameof(I_TestDriver), nameof(eDoOpI), nameof(eDoOpJ), nameof(eQueryState), nameof(eQueryStateResponse), nameof(eUpdateToState0), nameof(eUpdateToState1), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_FaultTolerantMachine), nameof(eQueryStateResponse), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_ServiceMachine), nameof(eDoOpI), nameof(eDoOpJ), nameof(eQueryState), nameof(eQueryStateResponse), nameof(eUpdateToState0), nameof(eUpdateToState1), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_ReliableStorageMachine), nameof(eDoOpI), nameof(eDoOpJ), nameof(eQueryState), nameof(eQueryStateResponse), nameof(eUpdateToState0), nameof(eUpdateToState1), nameof(PHalt));
        }
    }
    
}
#pragma warning restore 162, 219, 414
