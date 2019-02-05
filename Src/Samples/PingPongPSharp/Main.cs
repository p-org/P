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
namespace pingpongmonitor
{
    public static partial class GlobalFunctions { }
    internal partial class Ping : PEvent
    {
        static Ping() { AssertVal = 1; AssumeVal = -1; }
        public Ping() : base() { }
        public Ping(PMachineValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new Ping(); }
    }
    internal partial class Pong : PEvent
    {
        static Pong() { AssertVal = 1; AssumeVal = -1; }
        public Pong() : base() { }
        public Pong(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new Pong(); }
    }
    internal partial class Success : PEvent
    {
        static Success() { AssertVal = -1; AssumeVal = -1; }
        public Success() : base() { }
        public Success(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new Success(); }
    }
    internal partial class Main : PMachine
    {
        private PMachineValue pongId = null;
        public class ConstructorEvent : PEvent { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Main()
        {
            this.sends.Add(nameof(Ping));
            this.sends.Add(nameof(Pong));
            this.sends.Add(nameof(Success));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(Ping));
            this.receives.Add(nameof(Pong));
            this.receives.Add(nameof(Success));
            this.receives.Add(nameof(PHalt));
            this.creates.Add(nameof(I_PONG));
        }

        public void Anon()
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0 = null;
            PEvent TMP_tmp1 = null;
            TMP_tmp0 = (PMachineValue)(currentMachine.CreateInterface<I_PONG>(currentMachine));
            pongId = (PMachineValue)TMP_tmp0;
            TMP_tmp1 = (PEvent)(new Success(null));
            currentMachine.RaiseEvent((Event)TMP_tmp1);
            throw new PUnreachableCodeException();
        }
        public void Anon_1()
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0_1 = null;
            PEvent TMP_tmp1_1 = null;
            PMachineValue TMP_tmp2 = null;
            PEvent TMP_tmp3 = null;
            TMP_tmp0_1 = (PMachineValue)(((PMachineValue)((IPrtValue)pongId)?.Clone()));
            TMP_tmp1_1 = (PEvent)(new Ping(null));
            TMP_tmp2 = (PMachineValue)(currentMachine.self);
            currentMachine.SendEvent(TMP_tmp0_1, (Event)TMP_tmp1_1, TMP_tmp2);
            TMP_tmp3 = (PEvent)(new Success(null));
            currentMachine.RaiseEvent((Event)TMP_tmp3);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Ping_Init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon))]
        [OnEventGotoState(typeof(Success), typeof(Ping_SendPing))]
        class Ping_Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_1))]
        [OnEventGotoState(typeof(Success), typeof(Ping_WaitPong))]
        class Ping_SendPing : MachineState
        {
        }
        [OnEventGotoState(typeof(Pong), typeof(Ping_SendPing))]
        class Ping_WaitPong : MachineState
        {
        }
        class Done : MachineState
        {
        }
    }
    internal partial class PONG : PMachine
    {
        public class ConstructorEvent : PEvent { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public PONG()
        {
            this.sends.Add(nameof(Ping));
            this.sends.Add(nameof(Pong));
            this.sends.Add(nameof(Success));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(Ping));
            this.receives.Add(nameof(Pong));
            this.receives.Add(nameof(Success));
            this.receives.Add(nameof(PHalt));
        }

        public void Anon_2()
        {
            PONG currentMachine = this;
        }
        public void Anon_3()
        {
            PONG currentMachine = this;
            PMachineValue payload = (PMachineValue)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PMachineValue TMP_tmp0_2 = null;
            PEvent TMP_tmp1_2 = null;
            PEvent TMP_tmp2_1 = null;
            TMP_tmp0_2 = (PMachineValue)(((PMachineValue)((IPrtValue)payload)?.Clone()));
            TMP_tmp1_2 = (PEvent)(new Pong(null));
            currentMachine.SendEvent(TMP_tmp0_2, (Event)TMP_tmp1_2);
            TMP_tmp2_1 = (PEvent)(new Success(null));
            currentMachine.RaiseEvent((Event)TMP_tmp2_1);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Pong_WaitPing))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_2))]
        [OnEventGotoState(typeof(Ping), typeof(Pong_SendPong))]
        class Pong_WaitPing : MachineState
        {
        }
        [OnEntry(nameof(Anon_3))]
        [OnEventGotoState(typeof(Success), typeof(Pong_WaitPing))]
        class Pong_SendPong : MachineState
        {
        }
    }
    internal partial class M : PMonitor
    {
        static M()
        {
            observes.Add(nameof(Ping));
            observes.Add(nameof(Pong));
        }

        [Start]
        [Cold]
        [OnEventGotoState(typeof(Ping), typeof(ExpectPong))]
        class ExpectPing : MonitorState
        {
        }
        [Hot]
        [OnEventGotoState(typeof(Pong), typeof(ExpectPing))]
        class ExpectPong : MonitorState
        {
        }
    }
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap.Clear();
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_PONG), nameof(I_PONG));
            PModule.linkMap[nameof(I_PONG)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Clear();
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_PONG), typeof(PONG));
        }

        public static void InitializeMonitorObserves()
        {
            PModule.monitorObserves.Clear();
        }

        public static void InitializeMonitorMap(PSharpRuntime runtime)
        {
            PModule.monitorMap.Clear();
        }


        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime)
        {
            runtime.SetLogger(new PLogger());
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
    public class I_Main : PMachineValue
    {
        public I_Main(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_PONG : PMachineValue
    {
        public I_PONG(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.Clear();
            PInterfaces.AddInterface(nameof(I_Main), nameof(Ping), nameof(Pong), nameof(Success), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_PONG), nameof(Ping), nameof(Pong), nameof(Success), nameof(PHalt));
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
