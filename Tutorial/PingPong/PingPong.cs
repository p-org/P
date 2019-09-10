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
namespace PingPong
{
    public static partial class GlobalFunctions {}
    internal partial class START : PEvent
    {
        static START() { AssertVal = -1; AssumeVal = -1;}
        public START() : base() {}
        public START (PrtInt payload): base(payload){ }
        public override IPrtValue Clone() { return new START();}
    }
    internal partial class TIMEOUT : PEvent
    {
        static TIMEOUT() { AssertVal = -1; AssumeVal = -1;}
        public TIMEOUT() : base() {}
        public TIMEOUT (PMachineValue payload): base(payload){ }
        public override IPrtValue Clone() { return new TIMEOUT();}
    }
    internal partial class PING : PEvent
    {
        static PING() { AssertVal = 1; AssumeVal = -1;}
        public PING() : base() {}
        public PING (PMachineValue payload): base(payload){ }
        public override IPrtValue Clone() { return new PING();}
    }
    internal partial class PONG : PEvent
    {
        static PONG() { AssertVal = 1; AssumeVal = -1;}
        public PONG() : base() {}
        public PONG (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new PONG();}
    }
    internal partial class SUCCESS : PEvent
    {
        static SUCCESS() { AssertVal = -1; AssumeVal = -1;}
        public SUCCESS() : base() {}
        public SUCCESS (IPrtValue payload): base(payload){ }
        public override IPrtValue Clone() { return new SUCCESS();}
    }
    public static partial class GlobalFunctions
    {
        public static PMachineValue CreateTimer(PMachineValue owner, PMachine currentMachine)
        {
            PMachineValue m = null;
            PMachineValue TMP_tmp0 = null;
            PMachineValue TMP_tmp1 = null;
            TMP_tmp0 = (PMachineValue)(((PMachineValue)((IPrtValue)owner)?.Clone()));
            TMP_tmp1 = (PMachineValue)(currentMachine.CreateInterface<I_Timer>( currentMachine, TMP_tmp0));
            m = (PMachineValue)TMP_tmp1;
            return ((PMachineValue)((IPrtValue)m)?.Clone());
        }
    }
    public static partial class GlobalFunctions
    {
        public static void StartTimer(PMachineValue timer, PrtInt time, PMachine currentMachine)
        {
            PMachineValue TMP_tmp0_1 = null;
            PEvent TMP_tmp1_1 = null;
            PrtInt TMP_tmp2 = ((PrtInt)0);
            TMP_tmp0_1 = (PMachineValue)(((PMachineValue)((IPrtValue)timer)?.Clone()));
            TMP_tmp1_1 = (PEvent)(new START(((PrtInt)0)));
            TMP_tmp2 = (PrtInt)(((PrtInt)((IPrtValue)time)?.Clone()));
            currentMachine.SendEvent(TMP_tmp0_1, (Event)TMP_tmp1_1, TMP_tmp2);
        }
    }
    public static partial class GlobalFunctions
    {
        public static void StopProgram(PMachine currentMachine)
        {
        }
    }
    internal partial class Timer : PMachine
    {
        private PMachineValue client = null;
        public class ConstructorEvent : PEvent{public ConstructorEvent(PMachineValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PMachineValue)value); }
        public Timer() {
            this.sends.Add(nameof(TIMEOUT));
            this.receives.Add(nameof(START));
        }
        
        public void Anon()
        {
            Timer currentMachine = this;
            PMachineValue payload = (PMachineValue)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            client = (PMachineValue)(((PMachineValue)((IPrtValue)payload)?.Clone()));
            currentMachine.GotoState<Timer.WaitForReq>();
            throw new PUnreachableCodeException();
        }
        public void Anon_1()
        {
            Timer currentMachine = this;
            PMachineValue TMP_tmp0_2 = null;
            PEvent TMP_tmp1_2 = null;
            PMachineValue TMP_tmp2_1 = null;
            TMP_tmp0_2 = (PMachineValue)(((PMachineValue)((IPrtValue)client)?.Clone()));
            TMP_tmp1_2 = (PEvent)(new TIMEOUT(null));
            TMP_tmp2_1 = (PMachineValue)(currentMachine.self);
            currentMachine.SendEvent(TMP_tmp0_2, (Event)TMP_tmp1_2, TMP_tmp2_1);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon))]
        class Init : MachineState
        {
        }
        [OnEventGotoState(typeof(START), typeof(WaitForTimeout))]
        class WaitForReq : MachineState
        {
        }
        [OnEventGotoState(typeof(Default), typeof(WaitForReq), nameof(Anon_1))]
        [IgnoreEvents(typeof(START))]
        class WaitForTimeout : MachineState
        {
        }
    }
    internal partial class Test_1_Machine : PMachine
    {
        private PMachineValue client_1 = null;
        public class ConstructorEvent : PEvent{public ConstructorEvent(IPrtValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Test_1_Machine() {
            this.creates.Add(nameof(I_ClientMachine));
        }
        
        public void Anon_2()
        {
            Test_1_Machine currentMachine = this;
            PrtInt TMP_tmp0_3 = ((PrtInt)0);
            PMachineValue TMP_tmp1_3 = null;
            TMP_tmp0_3 = (PrtInt)(((PrtInt)5));
            TMP_tmp1_3 = (PMachineValue)(currentMachine.CreateInterface<I_ClientMachine>( currentMachine, TMP_tmp0_3));
            client_1 = TMP_tmp1_3;
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_1))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_2))]
        class Init_1 : MachineState
        {
        }
    }
    internal partial class Test_2_Machine : PMachine
    {
        private PMachineValue client_2 = null;
        public class ConstructorEvent : PEvent{public ConstructorEvent(IPrtValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Test_2_Machine() {
            this.creates.Add(nameof(I_ClientMachine));
        }
        
        public void Anon_3()
        {
            Test_2_Machine currentMachine = this;
            PrtInt TMP_tmp0_4 = ((PrtInt)0);
            PMachineValue TMP_tmp1_4 = null;
            TMP_tmp0_4 = (PrtInt)(-(((PrtInt)1)));
            TMP_tmp1_4 = (PMachineValue)(currentMachine.CreateInterface<I_ClientMachine>( currentMachine, TMP_tmp0_4));
            client_2 = TMP_tmp1_4;
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_2))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_3))]
        class Init_2 : MachineState
        {
        }
    }
    internal partial class ClientMachine : PMachine
    {
        private PMachineValue server = null;
        private PrtInt numIterations = ((PrtInt)0);
        public class ConstructorEvent : PEvent{public ConstructorEvent(PrtInt val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PrtInt)value); }
        public ClientMachine() {
            this.sends.Add(nameof(PING));
            this.receives.Add(nameof(PONG));
            this.creates.Add(nameof(I_ServerMachine));
        }
        
        public void Anon_4()
        {
            ClientMachine currentMachine = this;
            PrtInt n = (PrtInt)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PMachineValue TMP_tmp0_5 = null;
            PEvent TMP_tmp1_5 = null;
            PModule.runtime.Logger.WriteLine("Client created\n");
            numIterations = (PrtInt)(((PrtInt)((IPrtValue)n)?.Clone()));
            TMP_tmp0_5 = (PMachineValue)(currentMachine.CreateInterface<I_ServerMachine>( currentMachine));
            server = TMP_tmp0_5;
            TMP_tmp1_5 = (PEvent)(new SUCCESS(null));
            currentMachine.RaiseEvent((Event)TMP_tmp1_5);
            throw new PUnreachableCodeException();
        }
        public void Anon_5()
        {
            ClientMachine currentMachine = this;
            PrtBool TMP_tmp0_6 = ((PrtBool)false);
            PrtBool TMP_tmp1_6 = ((PrtBool)false);
            PrtInt TMP_tmp2_2 = ((PrtInt)0);
            PMachineValue TMP_tmp3 = null;
            PEvent TMP_tmp4 = null;
            PMachineValue TMP_tmp5 = null;
            TMP_tmp0_6 = (PrtBool)((PrtValues.SafeEquals(numIterations,((PrtInt)0))));
            if (TMP_tmp0_6)
            {
                currentMachine.GotoState<ClientMachine.Stop>();
                throw new PUnreachableCodeException();
            }
            else
            {
                TMP_tmp1_6 = (PrtBool)((numIterations) > (((PrtInt)0)));
                if (TMP_tmp1_6)
                {
                    TMP_tmp2_2 = (PrtInt)((numIterations) - (((PrtInt)1)));
                    numIterations = TMP_tmp2_2;
                }
            }
            PModule.runtime.Logger.WriteLine("Client sending PING\n");
            TMP_tmp3 = (PMachineValue)(((PMachineValue)((IPrtValue)server)?.Clone()));
            TMP_tmp4 = (PEvent)(new PING(null));
            TMP_tmp5 = (PMachineValue)(currentMachine.self);
            currentMachine.SendEvent(TMP_tmp3, (Event)TMP_tmp4, TMP_tmp5);
        }
        public void Anon_6()
        {
            ClientMachine currentMachine = this;
            GlobalFunctions.StopProgram(this);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_3))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_4))]
        [OnEventGotoState(typeof(SUCCESS), typeof(SendPing))]
        class Init_3 : MachineState
        {
        }
        [OnEntry(nameof(Anon_5))]
        [OnEventGotoState(typeof(PONG), typeof(SendPing))]
        class SendPing : MachineState
        {
        }
        [OnEntry(nameof(Anon_6))]
        class Stop : MachineState
        {
        }
    }
    internal partial class ServerMachine : PMachine
    {
        private PMachineValue timer_1 = null;
        private PMachineValue client_3 = null;
        public class ConstructorEvent : PEvent{public ConstructorEvent(IPrtValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public ServerMachine() {
            this.sends.Add(nameof(PONG));
            this.sends.Add(nameof(START));
            this.receives.Add(nameof(PING));
            this.receives.Add(nameof(TIMEOUT));
            this.creates.Add(nameof(I_Timer));
        }
        
        public void Anon_7()
        {
            ServerMachine currentMachine = this;
            PMachineValue TMP_tmp0_7 = null;
            PMachineValue TMP_tmp1_7 = null;
            PModule.runtime.Logger.WriteLine("Server created\n");
            TMP_tmp0_7 = (PMachineValue)(currentMachine.self);
            TMP_tmp1_7 = (PMachineValue)(GlobalFunctions.CreateTimer(TMP_tmp0_7, this));
            timer_1 = TMP_tmp1_7;
            currentMachine.GotoState<ServerMachine.WaitPing>();
            throw new PUnreachableCodeException();
        }
        public void Anon_8()
        {
            ServerMachine currentMachine = this;
            PMachineValue m_1 = (PMachineValue)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PMachineValue TMP_tmp0_8 = null;
            PrtInt TMP_tmp1_8 = ((PrtInt)0);
            client_3 = (PMachineValue)(((PMachineValue)((IPrtValue)m_1)?.Clone()));
            TMP_tmp0_8 = (PMachineValue)(((PMachineValue)((IPrtValue)timer_1)?.Clone()));
            TMP_tmp1_8 = (PrtInt)(((PrtInt)1000));
            GlobalFunctions.StartTimer(TMP_tmp0_8, TMP_tmp1_8, this);
        }
        public void Anon_9()
        {
            ServerMachine currentMachine = this;
            PMachineValue TMP_tmp0_9 = null;
            PEvent TMP_tmp1_9 = null;
            PModule.runtime.Logger.WriteLine("Server sending PONG\n");
            TMP_tmp0_9 = (PMachineValue)(((PMachineValue)((IPrtValue)client_3)?.Clone()));
            TMP_tmp1_9 = (PEvent)(new PONG(null));
            currentMachine.SendEvent(TMP_tmp0_9, (Event)TMP_tmp1_9);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_4))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_7))]
        class Init_4 : MachineState
        {
        }
        [OnEventGotoState(typeof(PING), typeof(Sleep))]
        class WaitPing : MachineState
        {
        }
        [OnEntry(nameof(Anon_8))]
        [OnEventGotoState(typeof(TIMEOUT), typeof(WaitPing), nameof(Anon_9))]
        class Sleep : MachineState
        {
        }
    }
    internal partial class Safety : PMonitor
    {
        private PrtInt pending = ((PrtInt)0);
        static Safety() {
            observes.Add(nameof(PING));
            observes.Add(nameof(PONG));
        }
        
        public void Anon_10()
        {
            Safety currentMachine = this;
            PrtBool TMP_tmp0_10 = ((PrtBool)false);
            PrtInt TMP_tmp1_10 = ((PrtInt)0);
            TMP_tmp0_10 = (PrtBool)((PrtValues.SafeEquals(pending,((PrtInt)0))));
            currentMachine.Assert(TMP_tmp0_10,"");
            TMP_tmp1_10 = (PrtInt)((pending) + (((PrtInt)1)));
            pending = TMP_tmp1_10;
        }
        public void Anon_11()
        {
            Safety currentMachine = this;
            PrtBool TMP_tmp0_11 = ((PrtBool)false);
            PrtInt TMP_tmp1_11 = ((PrtInt)0);
            TMP_tmp0_11 = (PrtBool)((PrtValues.SafeEquals(pending,((PrtInt)1))));
            currentMachine.Assert(TMP_tmp0_11,"");
            TMP_tmp1_11 = (PrtInt)((pending) - (((PrtInt)1)));
            pending = TMP_tmp1_11;
        }
        [Start]
        [OnEventDoAction(typeof(PING), nameof(Anon_10))]
        [OnEventDoAction(typeof(PONG), nameof(Anon_11))]
        class Init_5 : MonitorState
        {
        }
    }
    internal partial class Liveness : PMonitor
    {
        static Liveness() {
            observes.Add(nameof(PING));
            observes.Add(nameof(PONG));
        }
        
        [Start]
        [Cold]
        [OnEventGotoState(typeof(PING), typeof(WaitPong))]
        class WaitPing_1 : MonitorState
        {
        }
        [Hot]
        [OnEventGotoState(typeof(PONG), typeof(WaitPing_1))]
        class WaitPong : MonitorState
        {
        }
    }
    public class Test0 {
        public static void InitializeLinkMap() {
            PModule.linkMap.Clear();
            PModule.linkMap[nameof(I_Test_1_Machine)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Test_1_Machine)].Add(nameof(I_ClientMachine), nameof(I_ClientMachine));
            PModule.linkMap[nameof(I_ClientMachine)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_ClientMachine)].Add(nameof(I_ServerMachine), nameof(I_ServerMachine));
            PModule.linkMap[nameof(I_ServerMachine)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_ServerMachine)].Add(nameof(I_Timer), nameof(I_Timer));
            PModule.linkMap[nameof(I_Timer)] = new Dictionary<string, string>();
        }
        
        public static void InitializeInterfaceDefMap() {
            PModule.interfaceDefinitionMap.Clear();
            PModule.interfaceDefinitionMap.Add(nameof(I_Test_1_Machine), typeof(Test_1_Machine));
            PModule.interfaceDefinitionMap.Add(nameof(I_ClientMachine), typeof(ClientMachine));
            PModule.interfaceDefinitionMap.Add(nameof(I_ServerMachine), typeof(ServerMachine));
            PModule.interfaceDefinitionMap.Add(nameof(I_Timer), typeof(Timer));
        }
        
        public static void InitializeMonitorObserves() {
            PModule.monitorObserves.Clear();
            PModule.monitorObserves[nameof(Safety)] = new List<string>();
            PModule.monitorObserves[nameof(Safety)].Add(nameof(PING));
            PModule.monitorObserves[nameof(Safety)].Add(nameof(PONG));
        }
        
        public static void InitializeMonitorMap(PSharpRuntime runtime) {
            PModule.monitorMap.Clear();
            PModule.monitorMap[nameof(I_Test_1_Machine)] = new List<Type>();
            PModule.monitorMap[nameof(I_Test_1_Machine)].Add(typeof(Safety));
            PModule.monitorMap[nameof(I_ClientMachine)] = new List<Type>();
            PModule.monitorMap[nameof(I_ClientMachine)].Add(typeof(Safety));
            PModule.monitorMap[nameof(I_ServerMachine)] = new List<Type>();
            PModule.monitorMap[nameof(I_ServerMachine)].Add(typeof(Safety));
            PModule.monitorMap[nameof(I_Timer)] = new List<Type>();
            PModule.monitorMap[nameof(I_Timer)].Add(typeof(Safety));
            runtime.RegisterMonitor(typeof(Safety));
        }
        
        
        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime) {
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            PHelper.InitializeEnums();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof(Test_1_Machine)));
        }
    }
    public class Test1 {
        public static void InitializeLinkMap() {
            PModule.linkMap.Clear();
            PModule.linkMap[nameof(I_Test_2_Machine)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Test_2_Machine)].Add(nameof(I_ClientMachine), nameof(I_ClientMachine));
            PModule.linkMap[nameof(I_ClientMachine)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_ClientMachine)].Add(nameof(I_ServerMachine), nameof(I_ServerMachine));
            PModule.linkMap[nameof(I_ServerMachine)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_ServerMachine)].Add(nameof(I_Timer), nameof(I_Timer));
            PModule.linkMap[nameof(I_Timer)] = new Dictionary<string, string>();
        }
        
        public static void InitializeInterfaceDefMap() {
            PModule.interfaceDefinitionMap.Clear();
            PModule.interfaceDefinitionMap.Add(nameof(I_Test_2_Machine), typeof(Test_2_Machine));
            PModule.interfaceDefinitionMap.Add(nameof(I_ClientMachine), typeof(ClientMachine));
            PModule.interfaceDefinitionMap.Add(nameof(I_ServerMachine), typeof(ServerMachine));
            PModule.interfaceDefinitionMap.Add(nameof(I_Timer), typeof(Timer));
        }
        
        public static void InitializeMonitorObserves() {
            PModule.monitorObserves.Clear();
            PModule.monitorObserves[nameof(Liveness)] = new List<string>();
            PModule.monitorObserves[nameof(Liveness)].Add(nameof(PING));
            PModule.monitorObserves[nameof(Liveness)].Add(nameof(PONG));
        }
        
        public static void InitializeMonitorMap(PSharpRuntime runtime) {
            PModule.monitorMap.Clear();
            PModule.monitorMap[nameof(I_Test_2_Machine)] = new List<Type>();
            PModule.monitorMap[nameof(I_Test_2_Machine)].Add(typeof(Liveness));
            PModule.monitorMap[nameof(I_ClientMachine)] = new List<Type>();
            PModule.monitorMap[nameof(I_ClientMachine)].Add(typeof(Liveness));
            PModule.monitorMap[nameof(I_ServerMachine)] = new List<Type>();
            PModule.monitorMap[nameof(I_ServerMachine)].Add(typeof(Liveness));
            PModule.monitorMap[nameof(I_Timer)] = new List<Type>();
            PModule.monitorMap[nameof(I_Timer)].Add(typeof(Liveness));
            runtime.RegisterMonitor(typeof(Liveness));
        }
        
        
        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime) {
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            PHelper.InitializeEnums();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof(Test_2_Machine)));
        }
    }
    // TODO: NamedModule System
    public class I_Timer : PMachineValue {
        public I_Timer (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_Test_1_Machine : PMachineValue {
        public I_Test_1_Machine (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_Test_2_Machine : PMachineValue {
        public I_Test_2_Machine (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_ClientMachine : PMachineValue {
        public I_ClientMachine (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_ServerMachine : PMachineValue {
        public I_ServerMachine (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public partial class PHelper {
        public static void InitializeInterfaces() {
            PInterfaces.Clear();
            PInterfaces.AddInterface(nameof(I_Timer), nameof(START));
            PInterfaces.AddInterface(nameof(I_Test_1_Machine));
            PInterfaces.AddInterface(nameof(I_Test_2_Machine));
            PInterfaces.AddInterface(nameof(I_ClientMachine), nameof(PONG));
            PInterfaces.AddInterface(nameof(I_ServerMachine), nameof(PING), nameof(TIMEOUT));
        }
    }
    
    public partial class PHelper {
        public static void InitializeEnums() {
            PrtEnum.Clear();
        }
    }
    
}
#pragma warning restore 162, 219, 414
