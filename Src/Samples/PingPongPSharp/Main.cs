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
    internal partial class E : PEvent
    {
        static E() { AssertVal = -1; AssumeVal = -1; }
        public E() : base() { }
        public E(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new E(); }
    }
    internal partial class F : PEvent
    {
        static F() { AssertVal = -1; AssumeVal = -1; }
        public F() : base() { }
        public F(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new F(); }
    }
    internal partial class G : PEvent
    {
        static G() { AssertVal = -1; AssumeVal = -1; }
        public G() : base() { }
        public G(PrtInt payload) : base(payload) { }
        public override IPrtValue Clone() { return new G(); }
    }
    internal partial class Main : PMachine
    {
        private PrtInt x = ((PrtInt)0);
        public class ConstructorEvent : PEvent { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Main()
        {
            this.sends.Add(nameof(E));
            this.sends.Add(nameof(F));
            this.sends.Add(nameof(G));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(E));
            this.receives.Add(nameof(F));
            this.receives.Add(nameof(G));
            this.receives.Add(nameof(PHalt));
            this.creates.Add(nameof(I_B));
        }

        public async Task Anon()
        {
            Main currentMachine = this;
            PMachineValue b = null;
            PMachineValue TMP_tmp0 = null;
            PMachineValue TMP_tmp1 = null;
            PrtInt TMP_tmp2 = ((PrtInt)0);
            PrtBool TMP_tmp3 = ((PrtBool)false);
            PMachineValue TMP_tmp4 = null;
            PrtInt TMP_tmp5 = ((PrtInt)0);
            PrtBool TMP_tmp6 = ((PrtBool)false);
            TMP_tmp0 = (PMachineValue)(currentMachine.self);
            TMP_tmp1 = (PMachineValue)(currentMachine.CreateInterface<I_B>(currentMachine, TMP_tmp0));
            b = (PMachineValue)TMP_tmp1;
            TMP_tmp2 = (PrtInt)((x) + (((PrtInt)1)));
            x = TMP_tmp2;
            TMP_tmp3 = (PrtBool)((PrtValues.SafeEquals(x, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp3, "");
            TMP_tmp4 = (PMachineValue)(((PMachineValue)((IPrtValue)b)?.Clone()));
            TMP_tmp5 = (PrtInt)(((PrtInt)0));
            await foo(TMP_tmp4, TMP_tmp5);
            TMP_tmp6 = (PrtBool)((PrtValues.SafeEquals(x, ((PrtInt)2))));
            currentMachine.Assert(TMP_tmp6, "");
        }
        public async Task foo(PMachineValue b_1, PrtInt p)
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0_1 = null;
            IEventWithPayload TMP_tmp1_1 = null;
            PrtInt TMP_tmp2_1 = ((PrtInt)0);
            PMachineValue TMP_tmp3_1 = null;
            IEventWithPayload TMP_tmp4_1 = null;
            PrtInt TMP_tmp5_1 = ((PrtInt)0);
            PrtInt TMP_tmp6_1 = ((PrtInt)0);
            PrtInt TMP_tmp7 = ((PrtInt)0);
            PrtInt TMP_tmp8 = ((PrtInt)0);
            PrtInt TMP_tmp9 = ((PrtInt)0);
            PrtInt TMP_tmp10 = ((PrtInt)0);
            PrtInt TMP_tmp11 = ((PrtInt)0);
            TMP_tmp0_1 = (PMachineValue)(((PMachineValue)((IPrtValue)b_1)?.Clone()));
            TMP_tmp1_1 = (IEventWithPayload)(new E(((PrtInt)0)));
            TMP_tmp2_1 = (PrtInt)(((PrtInt)0));
            currentMachine.SendEvent(currentMachine, TMP_tmp0_1, (Event)TMP_tmp1_1, TMP_tmp2_1);
            TMP_tmp3_1 = (PMachineValue)(((PMachineValue)((IPrtValue)b_1)?.Clone()));
            TMP_tmp4_1 = (IEventWithPayload)(new G(((PrtInt)0)));
            TMP_tmp5_1 = (PrtInt)(((PrtInt)1));
            currentMachine.SendEvent(currentMachine, TMP_tmp3_1, (Event)TMP_tmp4_1, TMP_tmp5_1);
            var PGEN_recvEvent = await currentMachine.ReceiveEvent(typeof(E), typeof(F), typeof(G));
            switch (PGEN_recvEvent)
            {
                case E PGEN_evt:
                    {
                        var payload = PGEN_evt.Payload;
                        TMP_tmp6_1 = (PrtInt)((x) + (p));
                        TMP_tmp7 = (PrtInt)((TMP_tmp6_1) + (((PrtInt)1)));
                        x = TMP_tmp7;
                    }
                    break;
                case F PGEN_evt_1:
                    {
                        TMP_tmp8 = (PrtInt)((x) + (p));
                        TMP_tmp9 = (PrtInt)((TMP_tmp8) + (((PrtInt)2)));
                        x = TMP_tmp9;
                    }
                    break;
                case G PGEN_evt_2:
                    {
                        var payload_1 = PGEN_evt_2.Payload;
                        TMP_tmp10 = (PrtInt)((x) + (p));
                        TMP_tmp11 = (PrtInt)((TMP_tmp10) + (payload_1));
                        x = TMP_tmp11;
                    }
                    break;
            }
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
    internal partial class B : PMachine
    {
        public class ConstructorEvent : PEvent { public ConstructorEvent(PMachineValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PMachineValue)value); }
        public B()
        {
            this.sends.Add(nameof(E));
            this.sends.Add(nameof(F));
            this.sends.Add(nameof(G));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(E));
            this.receives.Add(nameof(F));
            this.receives.Add(nameof(G));
            this.receives.Add(nameof(PHalt));
        }

        public async Task Anon_1()
        {
            B currentMachine = this;
            PMachineValue payload1 = (PMachineValue)(gotoPayload ?? ((PEvent)currentMachine.ReceivedEvent).Payload);
            this.gotoPayload = null;
            PMachineValue y = null;
            PrtInt z = ((PrtInt)0);
            PrtInt TMP_tmp0_2 = ((PrtInt)0);
            PrtBool TMP_tmp1_2 = ((PrtBool)false);
            PMachineValue TMP_tmp2_2 = null;
            IEventWithPayload TMP_tmp3_2 = null;
            PrtInt TMP_tmp4_2 = ((PrtInt)0);
            PrtInt TMP_tmp5_2 = ((PrtInt)0);
            PrtBool TMP_tmp6_2 = ((PrtBool)false);
            PrtBool TMP_tmp7_1 = ((PrtBool)false);
            PrtBool TMP_tmp8_1 = ((PrtBool)false);
            TMP_tmp0_2 = (PrtInt)((z) + (((PrtInt)1)));
            z = TMP_tmp0_2;
            y = (PMachineValue)(((PMachineValue)((IPrtValue)payload1)?.Clone()));
            var PGEN_recvEvent_1 = await currentMachine.ReceiveEvent(typeof(E));
            switch (PGEN_recvEvent_1)
            {
                case E PGEN_evt_3:
                    {
                        var payload2 = PGEN_evt_3.Payload;
                        TMP_tmp1_2 = (PrtBool)((PrtValues.SafeEquals((IPrtValue)payload2, ((PrtInt)0))));
                        currentMachine.Assert(TMP_tmp1_2, "");
                        var PGEN_recvEvent_2 = await currentMachine.ReceiveEvent(typeof(G));
                        switch (PGEN_recvEvent_2)
                        {
                            case G PGEN_evt_4:
                                {
                                    var payload3 = PGEN_evt_4.Payload;
                                    PrtInt x_1 = ((PrtInt)0);
                                    PrtInt a = ((PrtInt)0);
                                    PrtInt b_2 = ((PrtInt)0);
                                    IEventWithPayload c = null;
                                    x_1 = (PrtInt)(((PrtInt)((IPrtValue)payload3)?.Clone()));
                                    TMP_tmp2_2 = (PMachineValue)(((PMachineValue)((IPrtValue)y)?.Clone()));
                                    TMP_tmp3_2 = (IEventWithPayload)(new G(((PrtInt)0)));
                                    TMP_tmp4_2 = (PrtInt)(((PrtInt)((IPrtValue)x_1)?.Clone()));
                                    currentMachine.SendEvent(currentMachine, TMP_tmp2_2, (Event)TMP_tmp3_2, TMP_tmp4_2);
                                    a = (PrtInt)(((PrtInt)10));
                                    b_2 = (PrtInt)(((PrtInt)11));
                                    TMP_tmp5_2 = (PrtInt)((a) + (z));
                                    TMP_tmp6_2 = (PrtBool)((PrtValues.SafeEquals(b_2, TMP_tmp5_2)));
                                    currentMachine.Assert(TMP_tmp6_2, "");
                                }
                                break;
                        }
                        TMP_tmp7_1 = (PrtBool)((PrtValues.SafeEquals((IPrtValue)payload2, ((PrtInt)0))));
                        currentMachine.Assert(TMP_tmp7_1, "");
                    }
                    break;
            }
            TMP_tmp8_1 = (PrtBool)((PrtValues.SafeEquals(y, payload1)));
            currentMachine.Assert(TMP_tmp8_1, "");
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_1))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_1))]
        class Init_1 : MachineState
        {
        }
    }
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_B), nameof(I_B));
            PModule.linkMap[nameof(I_B)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_B), typeof(B));
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
    public class I_Main : PMachineValue
    {
        public I_Main(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public class I_B : PMachineValue
    {
        public I_B(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.AddInterface(nameof(I_Main), nameof(E), nameof(F), nameof(G), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_B), nameof(E), nameof(F), nameof(G), nameof(PHalt));
        }
    }

}
#pragma warning restore 162, 219, 414
