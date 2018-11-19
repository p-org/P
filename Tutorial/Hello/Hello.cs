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
namespace Hello
{
    public static partial class GlobalFunctions_Hello {}
    internal class START : PEvent<PrtInt>
    {
        static START() { AssertVal = -1; AssumeVal = -1;}
        public START() : base() {}
        public START (PrtInt payload): base(payload){ }
        public override IPrtValue Clone() { return new START();}
    }
    internal class TIMEOUT : PEvent<PMachineValue>
    {
        static TIMEOUT() { AssertVal = -1; AssumeVal = -1;}
        public TIMEOUT() : base() {}
        public TIMEOUT (PMachineValue payload): base(payload){ }
        public override IPrtValue Clone() { return new TIMEOUT();}
    }
    public static partial class GlobalFunctions_Hello
    {
        public static PMachineValue CreateTimer(PMachineValue owner, PMachine currentMachine)
        {
            PMachineValue m = null;
            PMachineValue TMP_tmp0 = null;
            PMachineValue TMP_tmp1 = null;
            TMP_tmp0 = ((PMachineValue)((IPrtValue)owner)?.Clone());
            TMP_tmp1 = currentMachine.CreateInterface<I_Timer>( currentMachine, TMP_tmp0);
            m = (PMachineValue)TMP_tmp1;
            return ((PMachineValue)((IPrtValue)m)?.Clone());
        }
    }
    public static partial class GlobalFunctions_Hello
    {
        public static void StartTimer(PMachineValue timer, PrtInt time, PMachine currentMachine)
        {
            PMachineValue TMP_tmp0_1 = null;
            IEventWithPayload TMP_tmp1_1 = null;
            PrtInt TMP_tmp2 = ((PrtInt)0);
            TMP_tmp0_1 = ((PMachineValue)((IPrtValue)timer)?.Clone());
            TMP_tmp1_1 = new START(((PrtInt)0));
            TMP_tmp2 = ((PrtInt)((IPrtValue)time)?.Clone());
            currentMachine.SendEvent(currentMachine, TMP_tmp0_1, (Event)TMP_tmp1_1, TMP_tmp2);
        }
    }
    public static partial class GlobalFunctions_Hello
    {
        public static void StopProgram(PMachine currentMachine)
        {
        }
    }
    public static partial class GlobalFunctions_Hello
    {
        public static PrtBool Continue(PMachine currentMachine)
        {
            PrtBool TMP_tmp0_2 = ((PrtBool)false);
            TMP_tmp0_2 = ((PrtBool)currentMachine.Random());
            return ((PrtBool)((IPrtValue)TMP_tmp0_2)?.Clone());
        }
    }
    internal class Timer : PMachine
    {
        private PMachineValue client = null;
        public class ConstructorEvent : PEvent<PMachineValue>{public ConstructorEvent(PMachineValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PMachineValue)value); }
        public Timer() {
            this.sends.Add(nameof(TIMEOUT));
            this.receives.Add(nameof(START));
        }
        
        public void Anon()
        {
            Timer currentMachine = this;
            PMachineValue payload = ((PEvent<PMachineValue>)currentMachine.ReceivedEvent).PayloadT;
            client = ((PMachineValue)((IPrtValue)payload)?.Clone());
            currentMachine.GotoState<Timer.WaitForReq>();
            throw new PUnreachableCodeException();
        }
        public void Anon_1()
        {
            Timer currentMachine = this;
            PMachineValue TMP_tmp0_3 = null;
            IEventWithPayload TMP_tmp1_2 = null;
            PMachineValue TMP_tmp2_1 = null;
            TMP_tmp0_3 = ((PMachineValue)((IPrtValue)client)?.Clone());
            TMP_tmp1_2 = new TIMEOUT(null);
            TMP_tmp2_1 = currentMachine.self;
            currentMachine.SendEvent(currentMachine, TMP_tmp0_3, (Event)TMP_tmp1_2, TMP_tmp2_1);
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
    internal class Hello : PMachine
    {
        private PMachineValue timer_1 = null;
        public class ConstructorEvent : PEvent<IPrtValue>{public ConstructorEvent(IPrtValue val) : base(val) { }}
        
        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Hello() {
            this.sends.Add(nameof(START));
            this.sends.Add(nameof(TIMEOUT));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(START));
            this.receives.Add(nameof(TIMEOUT));
            this.receives.Add(nameof(PHalt));
            this.creates.Add(nameof(I_Timer));
        }
        
        public void Anon_2()
        {
            Hello currentMachine = this;
            PMachineValue TMP_tmp0_4 = null;
            PMachineValue TMP_tmp1_3 = null;
            TMP_tmp0_4 = currentMachine.self;
            TMP_tmp1_3 = GlobalFunctions_Hello.CreateTimer(TMP_tmp0_4, this);
            timer_1 = TMP_tmp1_3;
            currentMachine.GotoState<Hello.GetInput>();
            throw new PUnreachableCodeException();
        }
        public void Anon_3()
        {
            Hello currentMachine = this;
            PrtBool b = ((PrtBool)false);
            PrtBool TMP_tmp0_5 = ((PrtBool)false);
            TMP_tmp0_5 = GlobalFunctions_Hello.Continue(this);
            b = TMP_tmp0_5;
            if (b)
            {
                currentMachine.GotoState<Hello.PrintHello>();
                throw new PUnreachableCodeException();
            }
            else
            {
                currentMachine.GotoState<Hello.Stop>();
                throw new PUnreachableCodeException();
            }
        }
        public void Anon_4()
        {
            Hello currentMachine = this;
            PMachineValue TMP_tmp0_6 = null;
            PrtInt TMP_tmp1_4 = ((PrtInt)0);
            TMP_tmp0_6 = ((PMachineValue)((IPrtValue)timer_1)?.Clone());
            TMP_tmp1_4 = ((PrtInt)100);
            GlobalFunctions_Hello.StartTimer(TMP_tmp0_6, TMP_tmp1_4, this);
        }
        public void Anon_5()
        {
            Hello currentMachine = this;
            PModule.runtime.Logger.WriteLine("Hello\n");
        }
        public void Anon_6()
        {
            Hello currentMachine = this;
            GlobalFunctions_Hello.StopProgram(this);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_1))]
        class __InitState__ : MachineState { }
        
        [OnEntry(nameof(Anon_2))]
        class Init_1 : MachineState
        {
        }
        [OnEntry(nameof(Anon_3))]
        class GetInput : MachineState
        {
        }
        [OnEntry(nameof(Anon_4))]
        [OnEventGotoState(typeof(TIMEOUT), typeof(GetInput), nameof(Anon_5))]
        class PrintHello : MachineState
        {
        }
        [OnEntry(nameof(Anon_6))]
        class Stop : MachineState
        {
        }
    }
    // TODO: TypeDef TimerPtr
    public class Test0 {
        public static void InitializeLinkMap() {
            PModule.linkMap[nameof(I_Hello)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Hello)].Add(nameof(I_Timer), nameof(I_Timer));
            PModule.linkMap[nameof(I_Timer)] = new Dictionary<string, string>();
        }
        
        public static void InitializeInterfaceDefMap() {
            PModule.interfaceDefinitionMap.Add(nameof(I_Hello), typeof(Hello));
            PModule.interfaceDefinitionMap.Add(nameof(I_Timer), typeof(Timer));
        }
        
        public static void InitializeMonitorObserves() {
        }
        
        public static void InitializeMonitorMap(PSharpRuntime runtime) {
        }
        
        
        [Microsoft.PSharp.Test]
        public static void Execute(PSharpRuntime runtime) {
            runtime.SetLogger(new PLogger());
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof(Hello)));
        }
    }
    public class I_Timer : PMachineValue {
        public I_Timer (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public class I_Hello : PMachineValue {
        public I_Hello (MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }
    
    public partial class PHelper {
        public static void InitializeInterfaces() {
            PInterfaces.AddInterface(nameof(I_Timer), nameof(START));
            PInterfaces.AddInterface(nameof(I_Hello), nameof(START), nameof(TIMEOUT), nameof(PHalt));
        }
    }
    
}
#pragma warning restore 162, 219, 414
