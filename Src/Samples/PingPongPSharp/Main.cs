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

#pragma warning disable 162, 219, 414
namespace Main
{
    public static partial class GlobalFunctions { }
    internal partial class Ping : PEvent<PMachineValue>
    {
        static Ping() { AssertVal = 1; AssumeVal = -1; }
        public Ping() : base() { }
        public Ping(PMachineValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new Ping(); }
    }
    internal partial class Pong : PEvent<PMachineValue>
    {
        static Pong() { AssertVal = 2; AssumeVal = -1; }
        public Pong() : base() { }
        public Pong(PMachineValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new Pong(); }
    }
    internal partial class Success : PEvent<IPrtValue>
    {
        static Success() { AssertVal = -1; AssumeVal = -1; }
        public Success() : base() { }
        public Success(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new Success(); }
    }
    internal partial class M_Ping : PEvent<IPrtValue>
    {
        static M_Ping() { AssertVal = -1; AssumeVal = -1; }
        public M_Ping() : base() { }
        public M_Ping(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new M_Ping(); }
    }
    internal partial class M_Pong : PEvent<IPrtValue>
    {
        static M_Pong() { AssertVal = -1; AssumeVal = -1; }
        public M_Pong() : base() { }
        public M_Pong(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new M_Pong(); }
    }
    public static partial class GlobalFunctions
    {
        public static PMachineValue _CREATEMACHINE(PMachineValue cner, PrtInt typeOfMachine, IPrtValue param, PMachineValue newMachine, PMachine currentMachine)
        {
            PrtBool TMP_tmp0 = ((PrtBool)false);
            PrtTuple<PMachineValue, PMachineValue> TMP_tmp1 = (new PrtTuple<PMachineValue, PMachineValue>(null, null));
            PMachineValue TMP_tmp2 = null;
            PrtBool TMP_tmp3 = ((PrtBool)false);
            PMachineValue TMP_tmp4 = null;
            TMP_tmp0 = (PrtValues.SafeEquals(typeOfMachine, typeOfMachine));
            if (TMP_tmp0)
            {
                TMP_tmp1 = ((PrtTuple<PMachineValue, PMachineValue>)param);
                TMP_tmp2 = currentMachine.CreateInterface<I_PING>(currentMachine, TMP_tmp1);
                newMachine = (PMachineValue)TMP_tmp2;
            }
            else
            {
                TMP_tmp3 = (PrtValues.SafeEquals(typeOfMachine, typeOfMachine));
                if (TMP_tmp3)
                {
                    TMP_tmp4 = currentMachine.CreateInterface<I_PONG>(currentMachine);
                    newMachine = (PMachineValue)TMP_tmp4;
                }
                else
                {
                    currentMachine.Assert(((PrtBool)false), "");
                    throw new PUnreachableCodeException();
                }
            }
            return ((PMachineValue)((IPrtValue)newMachine)?.Clone());
        }
    }
    public static partial class GlobalFunctions
    {
        public static void _SEND(PMachineValue target, IEventWithPayload e, IPrtValue p, PMachine currentMachine)
        {
            PMachineValue TMP_tmp0_1 = null;
            IEventWithPayload TMP_tmp1_1 = null;
            IPrtValue TMP_tmp2_1 = null;
            TMP_tmp0_1 = ((PMachineValue)((IPrtValue)target)?.Clone());
            TMP_tmp1_1 = ((IEventWithPayload)((IPrtValue)e)?.Clone());
            TMP_tmp2_1 = ((IPrtValue)((IPrtValue)p)?.Clone());
            currentMachine.SendEvent(currentMachine, TMP_tmp0_1, (Event)TMP_tmp1_1, TMP_tmp2_1);
        }
    }
    public static partial class GlobalFunctions
    {
        public static PMachineValue _CREATECONTAINER(PMachine currentMachine)
        {
            PMachineValue retVal = null;
            PMachineValue TMP_tmp0_2 = null;
            TMP_tmp0_2 = currentMachine.CreateInterface<I_Container>(currentMachine);
            retVal = (PMachineValue)TMP_tmp0_2;
            return ((PMachineValue)((IPrtValue)retVal)?.Clone());
        }
    }
    internal partial class PING : PMachine
    {
        private PrtTuple<PMachineValue, PMachineValue> pongMachine = (new PrtTuple<PMachineValue, PMachineValue>(null, null));
        public class ConstructorEvent : PEvent<PrtTuple<PMachineValue, PMachineValue>> { public ConstructorEvent(PrtTuple<PMachineValue, PMachineValue> val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PrtTuple<PMachineValue, PMachineValue>)value); }
        public PING()
        {
            this.sends.Add(nameof(M_Ping));
            this.sends.Add(nameof(M_Pong));
            this.sends.Add(nameof(Ping));
            this.sends.Add(nameof(Pong));
            this.sends.Add(nameof(Success));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(M_Ping));
            this.receives.Add(nameof(M_Pong));
            this.receives.Add(nameof(Ping));
            this.receives.Add(nameof(Pong));
            this.receives.Add(nameof(Success));
            this.receives.Add(nameof(PHalt));
        }

        public void Anon()
        {
            PING currentMachine = this;
            PrtTuple<PMachineValue, PMachineValue> payload = this.gotoPayload == null ? ((PEvent<PrtTuple<PMachineValue, PMachineValue>>)currentMachine.ReceivedEvent).PayloadT : (PrtTuple<PMachineValue, PMachineValue>)this.gotoPayload;
            this.gotoPayload = null;
            IEventWithPayload TMP_tmp0_3 = null;
            pongMachine = ((PrtTuple<PMachineValue, PMachineValue>)((IPrtValue)payload)?.Clone());
            TMP_tmp0_3 = new Success(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp0_3);
            throw new PUnreachableCodeException();
        }
        public void Anon_1()
        {
            PING currentMachine = this;
            PMachineValue TMP_tmp0_4 = null;
            IEventWithPayload TMP_tmp1_2 = null;
            PMachineValue TMP_tmp2_2 = null;
            PMachineValue TMP_tmp3_1 = null;
            IEventWithPayload TMP_tmp4_1 = null;
            PMachineValue TMP_tmp5 = null;
            IEventWithPayload TMP_tmp6 = null;
            currentMachine.Announce((Event)new M_Ping(null));
            TMP_tmp0_4 = pongMachine.Item1;
            TMP_tmp1_2 = new Ping(null);
            TMP_tmp2_2 = currentMachine.self;
            GlobalFunctions._SEND(TMP_tmp0_4, TMP_tmp1_2, TMP_tmp2_2, this);
            TMP_tmp3_1 = pongMachine.Item2;
            TMP_tmp4_1 = new Ping(null);
            TMP_tmp5 = currentMachine.self;
            GlobalFunctions._SEND(TMP_tmp3_1, TMP_tmp4_1, TMP_tmp5, this);
            TMP_tmp6 = new Success(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp6);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon))]
        [OnEventGotoState(typeof(Success), typeof(SendPing))]
        class Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_1))]
        [OnEventGotoState(typeof(Success), typeof(WaitPong_1))]
        class SendPing : MachineState
        {
        }
        [OnEventGotoState(typeof(Pong), typeof(WaitPong_2))]
        class WaitPong_1 : MachineState
        {
        }
        [OnEventGotoState(typeof(Pong), typeof(Done))]
        class WaitPong_2 : MachineState
        {
        }
        class Done : MachineState
        {
        }
    }
    internal partial class PONG : PMachine
    {
        public class ConstructorEvent : PEvent<IPrtValue> { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public PONG()
        {
            this.sends.Add(nameof(M_Ping));
            this.sends.Add(nameof(M_Pong));
            this.sends.Add(nameof(Ping));
            this.sends.Add(nameof(Pong));
            this.sends.Add(nameof(Success));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(M_Ping));
            this.receives.Add(nameof(M_Pong));
            this.receives.Add(nameof(Ping));
            this.receives.Add(nameof(Pong));
            this.receives.Add(nameof(Success));
            this.receives.Add(nameof(PHalt));
        }

        public void Anon_2()
        {
            PONG currentMachine = this;
            PMachineValue payload_1 = this.gotoPayload == null ? ((PEvent<PMachineValue>)currentMachine.ReceivedEvent).PayloadT : (PMachineValue)this.gotoPayload;
            this.gotoPayload = null;
            PMachineValue TMP_tmp0_5 = null;
            IEventWithPayload TMP_tmp1_3 = null;
            PMachineValue TMP_tmp2_3 = null;
            IEventWithPayload TMP_tmp3_2 = null;
            currentMachine.Announce((Event)new M_Pong(null));
            TMP_tmp0_5 = ((PMachineValue)((IPrtValue)payload_1)?.Clone());
            TMP_tmp1_3 = new Pong(null);
            TMP_tmp2_3 = currentMachine.self;
            GlobalFunctions._SEND(TMP_tmp0_5, TMP_tmp1_3, TMP_tmp2_3, this);
            TMP_tmp3_2 = new Success(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp3_2);
            throw new PUnreachableCodeException();
        }
        public void Anon_3()
        {
            PONG currentMachine = this;
            IEventWithPayload TMP_tmp0_6 = null;
            TMP_tmp0_6 = new PHalt(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp0_6);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_1))]
        class __InitState__ : MachineState { }

        [OnEventGotoState(typeof(Ping), typeof(SendPong))]
        class Init_1 : MachineState
        {
        }
        [OnEntry(nameof(Anon_2))]
        [OnEventGotoState(typeof(Success), typeof(End))]
        class SendPong : MachineState
        {
        }
        [OnEntry(nameof(Anon_3))]
        class End : MachineState
        {
        }
    }
    internal partial class M : PMonitor
    {
        [Start]
        [OnEventGotoState(typeof(M_Ping), typeof(ExpectPong_1))]
        class ExpectPing : MonitorState
        {
        }
        [OnEventGotoState(typeof(M_Pong), typeof(ExpectPong_2))]
        class ExpectPong_1 : MonitorState
        {
        }
        [OnEventGotoState(typeof(M_Pong), typeof(ExpectPing))]
        class ExpectPong_2 : MonitorState
        {
        }
    }
    internal partial class Main : PMachine
    {
        private PMachineValue container = null;
        private PMachineValue pongMachine_1 = null;
        private PMachineValue pongMachine_2 = null;
        public class ConstructorEvent : PEvent<IPrtValue> { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Main()
        {
            this.sends.Add(nameof(M_Ping));
            this.sends.Add(nameof(M_Pong));
            this.sends.Add(nameof(Ping));
            this.sends.Add(nameof(Pong));
            this.sends.Add(nameof(Success));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(M_Ping));
            this.receives.Add(nameof(M_Pong));
            this.receives.Add(nameof(Ping));
            this.receives.Add(nameof(Pong));
            this.receives.Add(nameof(Success));
            this.receives.Add(nameof(PHalt));
            this.creates.Add(nameof(I_Container));
            this.creates.Add(nameof(I_PING));
            this.creates.Add(nameof(I_PONG));
        }

        public void Anon_4()
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0_7 = null;
            PMachineValue TMP_tmp1_4 = null;
            PrtInt TMP_tmp2_4 = ((PrtInt)0);
            IPrtValue TMP_tmp3_3 = null;
            PMachineValue TMP_tmp4_2 = null;
            PMachineValue TMP_tmp5_1 = null;
            PMachineValue TMP_tmp6_1 = null;
            PMachineValue TMP_tmp7 = null;
            PrtInt TMP_tmp8 = ((PrtInt)0);
            IPrtValue TMP_tmp9 = null;
            PMachineValue TMP_tmp10 = null;
            PMachineValue TMP_tmp11 = null;
            PMachineValue TMP_tmp12 = null;
            PMachineValue TMP_tmp13 = null;
            PrtInt TMP_tmp14 = ((PrtInt)0);
            PMachineValue TMP_tmp15 = null;
            PMachineValue TMP_tmp16 = null;
            PrtTuple<PMachineValue, PMachineValue> TMP_tmp17 = (new PrtTuple<PMachineValue, PMachineValue>(null, null));
            PMachineValue TMP_tmp18 = null;
            TMP_tmp0_7 = GlobalFunctions._CREATECONTAINER(this);
            container = TMP_tmp0_7;
            TMP_tmp1_4 = ((PMachineValue)((IPrtValue)container)?.Clone());
            TMP_tmp2_4 = ((PrtInt)2);
            TMP_tmp3_3 = null;
            TMP_tmp4_2 = ((PMachineValue)null);
            TMP_tmp5_1 = GlobalFunctions._CREATEMACHINE(TMP_tmp1_4, TMP_tmp2_4, TMP_tmp3_3, TMP_tmp4_2, this);
            pongMachine_1 = TMP_tmp5_1;
            TMP_tmp6_1 = GlobalFunctions._CREATECONTAINER(this);
            container = TMP_tmp6_1;
            TMP_tmp7 = ((PMachineValue)((IPrtValue)container)?.Clone());
            TMP_tmp8 = ((PrtInt)2);
            TMP_tmp9 = null;
            TMP_tmp10 = ((PMachineValue)null);
            TMP_tmp11 = GlobalFunctions._CREATEMACHINE(TMP_tmp7, TMP_tmp8, TMP_tmp9, TMP_tmp10, this);
            pongMachine_2 = TMP_tmp11;
            TMP_tmp12 = GlobalFunctions._CREATECONTAINER(this);
            container = TMP_tmp12;
            TMP_tmp13 = ((PMachineValue)((IPrtValue)container)?.Clone());
            TMP_tmp14 = ((PrtInt)1);
            TMP_tmp15 = ((PMachineValue)((IPrtValue)pongMachine_1)?.Clone());
            TMP_tmp16 = ((PMachineValue)((IPrtValue)pongMachine_2)?.Clone());
            TMP_tmp17 = new PrtTuple<PMachineValue, PMachineValue>((PMachineValue)TMP_tmp15, (PMachineValue)TMP_tmp16);
            TMP_tmp18 = ((PMachineValue)null);
            GlobalFunctions._CREATEMACHINE(TMP_tmp13, TMP_tmp14, TMP_tmp17, TMP_tmp18, this);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_2))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_4))]
        class Init_2 : MachineState
        {
        }
    }
    internal partial class Container : PMachine
    {
        public class ConstructorEvent : PEvent<IPrtValue> { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Container()
        {
            this.sends.Add(nameof(M_Ping));
            this.sends.Add(nameof(M_Pong));
            this.sends.Add(nameof(Ping));
            this.sends.Add(nameof(Pong));
            this.sends.Add(nameof(Success));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(M_Ping));
            this.receives.Add(nameof(M_Pong));
            this.receives.Add(nameof(Ping));
            this.receives.Add(nameof(Pong));
            this.receives.Add(nameof(Success));
            this.receives.Add(nameof(PHalt));
        }

        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_3))]
        class __InitState__ : MachineState { }

        class Init_3 : MachineState
        {
        }
    }
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap[nameof(I_PING)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_PONG)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_Container), nameof(I_Container));
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_PING), nameof(I_PING));
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_PONG), nameof(I_PONG));
            PModule.linkMap[nameof(I_Container)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add(nameof(I_PING), typeof(PING));
            PModule.interfaceDefinitionMap.Add(nameof(I_PONG), typeof(PONG));
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_Container), typeof(Container));
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
    public class I_PING : PMachineValue
    {
        public I_PING(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_PONG : PMachineValue
    {
        public I_PONG(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_Main : PMachineValue
    {
        public I_Main(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_Container : PMachineValue
    {
        public I_Container(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.AddInterface(nameof(I_PING), nameof(M_Ping), nameof(M_Pong), nameof(Ping), nameof(Pong), nameof(Success), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_PONG), nameof(M_Ping), nameof(M_Pong), nameof(Ping), nameof(Pong), nameof(Success), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_Main), nameof(M_Ping), nameof(M_Pong), nameof(Ping), nameof(Pong), nameof(Success), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_Container), nameof(M_Ping), nameof(M_Pong), nameof(Ping), nameof(Pong), nameof(Success), nameof(PHalt));
        }
    }

}
#pragma warning restore 162, 219, 414



namespace Main
{
    public class _TestRegression
    {
        public static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the P# runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled(2);

            // Creates a new P# runtime instance, and passes an optional configuration.
            var runtime = PSharpRuntime.Create(configuration);

            // Executes the P# program.
            DefaultImpl.Execute(runtime);

            // The P# runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }
    }
}
