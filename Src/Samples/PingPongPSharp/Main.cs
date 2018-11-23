using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Plang.PrtSharp;
using Plang.PrtSharp.Values;

#pragma warning disable 162, 219, 414
namespace Main
{
    public static class GlobalFunctions
    {
    }

    internal class e1 : PEvent
    {
        static e1()
        {
            AssertVal = -1;
            AssumeVal = -1;
        }

        public e1()
        {
        }

        public e1(IPrtValue payload) : base(payload)
        {
        }

        public override IPrtValue Clone()
        {
            return new e1();
        }
    }

    internal class e2 : PEvent
    {
        static e2()
        {
            AssertVal = -1;
            AssumeVal = -1;
        }

        public e2()
        {
        }

        public e2(IPrtValue payload) : base(payload)
        {
        }

        public override IPrtValue Clone()
        {
            return new e2();
        }
    }

    internal class e3 : PEvent
    {
        static e3()
        {
            AssertVal = -1;
            AssumeVal = -1;
        }

        public e3()
        {
        }

        public e3(IPrtValue payload) : base(payload)
        {
        }

        public override IPrtValue Clone()
        {
            return new e3();
        }
    }

    internal class e4 : PEvent
    {
        static e4()
        {
            AssertVal = -1;
            AssumeVal = -1;
        }

        public e4()
        {
        }

        public e4(IPrtValue payload) : base(payload)
        {
        }

        public override IPrtValue Clone()
        {
            return new e4();
        }
    }

    internal class Main : PMachine
    {
        private PMachineValue m;

        public Main()
        {
            sends.Add(nameof(e1));
            sends.Add(nameof(e2));
            sends.Add(nameof(e3));
            sends.Add(nameof(e4));
            sends.Add(nameof(PHalt));
            receives.Add(nameof(e1));
            receives.Add(nameof(e2));
            receives.Add(nameof(e3));
            receives.Add(nameof(e4));
            receives.Add(nameof(PHalt));
            creates.Add(nameof(I_Receiver));
        }

        protected override Event GetConstructorEvent(IPrtValue value)
        {
            return new ConstructorEvent(value);
        }

        public void Anon()
        {
            var currentMachine = this;
            PMachineValue TMP_tmp0 = null;
            PMachineValue TMP_tmp1 = null;
            PEvent TMP_tmp2 = null;
            PMachineValue TMP_tmp3 = null;
            PEvent TMP_tmp4 = null;
            PMachineValue TMP_tmp5 = null;
            PEvent TMP_tmp6 = null;
            PMachineValue TMP_tmp7 = null;
            PEvent TMP_tmp8 = null;
            TMP_tmp0 = currentMachine.CreateInterface<I_Receiver>(currentMachine);
            m = TMP_tmp0;
            TMP_tmp1 = (PMachineValue) ((IPrtValue) m)?.Clone();
            TMP_tmp2 = new e1(null);
            currentMachine.SendEvent(TMP_tmp1, TMP_tmp2);
            TMP_tmp3 = (PMachineValue) ((IPrtValue) m)?.Clone();
            TMP_tmp4 = new e2(null);
            currentMachine.SendEvent(TMP_tmp3, TMP_tmp4);
            TMP_tmp5 = (PMachineValue) ((IPrtValue) m)?.Clone();
            TMP_tmp6 = new e3(null);
            currentMachine.SendEvent(TMP_tmp5, TMP_tmp6);
            TMP_tmp7 = (PMachineValue) ((IPrtValue) m)?.Clone();
            TMP_tmp8 = new e4(null);
            currentMachine.SendEvent(TMP_tmp7, TMP_tmp8);
        }

        public class ConstructorEvent : PEvent
        {
            public ConstructorEvent(IPrtValue val) : base(val)
            {
            }
        }

        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init))]
        private class __InitState__ : MachineState
        {
        }

        [OnEntry(nameof(Anon))]
        private class Init : MachineState
        {
        }
    }

    internal class Receiver : PMachine
    {
        private readonly PrtNamedTuple ts = new PrtNamedTuple(new[] {"a", "b"}, (PrtInt) 0, (PrtInt) 0);
        private PrtInt x = 0;
        private PEvent y;
        private PrtInt z = 0;

        public Receiver()
        {
            sends.Add(nameof(e1));
            sends.Add(nameof(e2));
            sends.Add(nameof(e3));
            sends.Add(nameof(e4));
            sends.Add(nameof(PHalt));
            receives.Add(nameof(e1));
            receives.Add(nameof(e2));
            receives.Add(nameof(e3));
            receives.Add(nameof(e4));
            receives.Add(nameof(PHalt));
        }

        protected override Event GetConstructorEvent(IPrtValue value)
        {
            return new ConstructorEvent(value);
        }

        public async Task Anon_1()
        {
            var currentMachine = this;
            var TMP_tmp0_1 = (PrtBool) false;
            var TMP_tmp1_1 = (PrtBool) false;
            var TMP_tmp2_1 = (PrtInt) 0;
            var TMP_tmp3_1 = (PrtInt) 0;
            var TMP_tmp4_1 = (PrtInt) 0;
            var TMP_tmp5_1 = (PrtBool) false;
            var TMP_tmp6_1 = (PrtInt) 0;
            var TMP_tmp7_1 = (PrtInt) 0;
            var TMP_tmp8_1 = (PrtBool) false;
            var TMP_tmp9 = (PrtInt) 0;
            var TMP_tmp10 = (PrtInt) 0;
            var TMP_tmp11 = (PrtBool) false;
            var TMP_tmp12 = (PrtBool) false;
            var TMP_tmp13 = (PrtInt) 0;
            var TMP_tmp14 = (PrtBool) false;
            var TMP_tmp15 = (PrtBool) false;
            var TMP_tmp16 = (PrtBool) false;
            var TMP_tmp17 = (PrtInt) 0;
            var TMP_tmp18 = (PrtBool) false;
            var TMP_tmp19 = (PrtBool) false;
            var TMP_tmp20 = (PrtBool) false;
            var TMP_tmp21 = (PrtInt) 0;
            var TMP_tmp22 = (PrtBool) false;
            var TMP_tmp23 = (PrtBool) false;
            var TMP_tmp24 = (PrtBool) false;
            var TMP_tmp25 = (PrtBool) false;
            var TMP_tmp26 = (PrtBool) false;
            var TMP_tmp27 = (PrtInt) 0;
            var TMP_tmp28 = (PrtBool) false;
            var TMP_tmp29 = (PrtInt) 0;
            var TMP_tmp30 = (PrtBool) false;
            var TMP_tmp31 = (PrtBool) false;
            var TMP_tmp32 = (PrtBool) false;
            var TMP_tmp33 = (PrtInt) 0;
            var TMP_tmp34 = (PrtBool) false;
            var TMP_tmp35 = (PrtInt) 0;
            var TMP_tmp36 = (PrtBool) false;
            var TMP_tmp37 = (PrtInt) 0;
            var TMP_tmp38 = (PrtBool) false;
            x = 10;
            y = new e1(null);
            var PGEN_recvEvent = await currentMachine.ReceiveEvent(typeof(e1));
            switch (PGEN_recvEvent)
            {
                case e1 PGEN_evt:
                {
                    var x_1 = (PrtInt) 0;
                    var y_1 = (PrtInt) 0;
                    var foo0 = (PrtInt) 0;
                    var Foo = (PrtInt) 0;
                    var a = (PrtInt) 0;
                    x_1 = 19;
                    TMP_tmp0_1 = PrtValues.SafeEquals(x_1, (PrtInt) 19);
                    currentMachine.Assert(TMP_tmp0_1, "");
                    y_1 = 1;
                    foo0 = 5;
                    TMP_tmp1_1 = PrtValues.SafeEquals(foo0, (PrtInt) 5);
                    currentMachine.Assert(TMP_tmp1_1, "");
                    TMP_tmp2_1 = 0;
                    TMP_tmp3_1 = TMP_tmp2_1;
                    Foo = TMP_tmp3_1;
                    TMP_tmp4_1 = PrtEnum.Get("bar0");
                    TMP_tmp5_1 = PrtValues.SafeEquals(Foo, TMP_tmp4_1);
                    currentMachine.Assert(TMP_tmp5_1, "");
                    TMP_tmp6_1 = (PrtInt) ((IPrtValue) y_1)?.Clone();
                    TMP_tmp7_1 = foo0_1(TMP_tmp6_1);
                    z = TMP_tmp7_1;
                    TMP_tmp8_1 = PrtValues.SafeEquals(z, (PrtInt) 1);
                    currentMachine.Assert(TMP_tmp8_1, "");
                    TMP_tmp9 = (PrtInt) ((IPrtValue) y_1)?.Clone();
                    TMP_tmp10 = foo1(TMP_tmp9);
                    z = TMP_tmp10;
                    TMP_tmp11 = PrtValues.SafeEquals(z, (PrtInt) 1);
                    currentMachine.Assert(TMP_tmp11, "");
                    a = 3;
                    ts["a"] = (PrtInt) 5;
                    TMP_tmp12 = PrtValues.SafeEquals(a, (PrtInt) 3);
                    currentMachine.Assert(TMP_tmp12, "");
                    TMP_tmp13 = (PrtInt) ts["a"];
                    TMP_tmp14 = PrtValues.SafeEquals(TMP_tmp13, (PrtInt) 5);
                    currentMachine.Assert(TMP_tmp14, "");
                    var PGEN_recvEvent_1 = await currentMachine.ReceiveEvent(typeof(e2));
                    switch (PGEN_recvEvent_1)
                    {
                        case e2 PGEN_evt_1:
                        {
                            TMP_tmp15 = PrtValues.SafeEquals(x_1, (PrtInt) 19);
                            currentMachine.Assert(TMP_tmp15, "");
                            TMP_tmp16 = PrtValues.SafeEquals(foo0, (PrtInt) 5);
                            currentMachine.Assert(TMP_tmp16, "");
                            TMP_tmp17 = PrtEnum.Get("bar0");
                            TMP_tmp18 = PrtValues.SafeEquals(Foo, TMP_tmp17);
                            currentMachine.Assert(TMP_tmp18, "");
                            TMP_tmp19 = PrtValues.SafeEquals(z, (PrtInt) 1);
                            currentMachine.Assert(TMP_tmp19, "");
                            TMP_tmp20 = PrtValues.SafeEquals(a, (PrtInt) 3);
                            currentMachine.Assert(TMP_tmp20, "");
                            TMP_tmp21 = (PrtInt) ts["a"];
                            TMP_tmp22 = PrtValues.SafeEquals(TMP_tmp21, (PrtInt) 5);
                            currentMachine.Assert(TMP_tmp22, "");
                            TMP_tmp23 = PrtValues.SafeEquals(y_1, (PrtInt) 1);
                            currentMachine.Assert(TMP_tmp23, "");
                            TMP_tmp24 = PrtValues.SafeEquals(foo0, (PrtInt) 5);
                            currentMachine.Assert(TMP_tmp24, "");
                        }
                            break;
                    }
                }
                    break;
            }

            var PGEN_recvEvent_2 = await currentMachine.ReceiveEvent(typeof(e3));
            switch (PGEN_recvEvent_2)
            {
                case e3 PGEN_evt_2:
                {
                    TMP_tmp25 = PrtValues.SafeEquals(x, (PrtInt) 10);
                    currentMachine.Assert(TMP_tmp25, "");
                    TMP_tmp26 = PrtValues.SafeEquals(y, new e1(null));
                    currentMachine.Assert(TMP_tmp26, "");
                    TMP_tmp27 = 0;
                    TMP_tmp28 = PrtValues.SafeEquals(PrtValues.Box((long) TMP_tmp27),
                        PrtValues.Box((long) PrtEnum.Get("foo0_2")));
                    currentMachine.Assert(TMP_tmp28, "");
                    TMP_tmp29 = (PrtInt) ts["a"];
                    TMP_tmp30 = PrtValues.SafeEquals(TMP_tmp29, (PrtInt) 5);
                    currentMachine.Assert(TMP_tmp30, "");
                }
                    break;
            }

            await bar();
            TMP_tmp31 = PrtValues.SafeEquals(x, (PrtInt) 10);
            currentMachine.Assert(TMP_tmp31, "");
            TMP_tmp32 = PrtValues.SafeEquals(y, new e1(null));
            currentMachine.Assert(TMP_tmp32, "");
            TMP_tmp33 = PrtEnum.Get("foo0_2");
            TMP_tmp34 = PrtValues.SafeEquals(TMP_tmp33, (PrtInt) 0);
            currentMachine.Assert(TMP_tmp34, "");
            TMP_tmp35 = 0;
            TMP_tmp36 = PrtValues.SafeEquals(PrtValues.Box((long) TMP_tmp35),
                PrtValues.Box((long) PrtEnum.Get("foo0_2")));
            currentMachine.Assert(TMP_tmp36, "");
            TMP_tmp37 = (PrtInt) ts["a"];
            TMP_tmp38 = PrtValues.SafeEquals(TMP_tmp37, (PrtInt) 5);
            currentMachine.Assert(TMP_tmp38, "");
        }

        public PrtInt foo0_1(PrtInt a_1)
        {
            var currentMachine = this;
            z = (PrtInt) ((IPrtValue) a_1)?.Clone();
            return (PrtInt) ((IPrtValue) a_1)?.Clone();
        }

        public PrtInt foo1(PrtInt a_2)
        {
            var currentMachine = this;
            z = (PrtInt) ((IPrtValue) a_2)?.Clone();
            return (PrtInt) ((IPrtValue) a_2)?.Clone();
        }

        public async Task bar()
        {
            var currentMachine = this;
            var x_2 = (PrtInt) 0;
            var PGEN_recvEvent_3 = await currentMachine.ReceiveEvent(typeof(e4));
            switch (PGEN_recvEvent_3)
            {
                case e4 PGEN_evt_3:
                {
                }
                    break;
            }
        }

        public class ConstructorEvent : PEvent
        {
            public ConstructorEvent(IPrtValue val) : base(val)
            {
            }
        }

        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Init_1))]
        private class __InitState__ : MachineState
        {
        }

        [OnEntry(nameof(Anon_1))]
        private class Init_1 : MachineState
        {
        }
    }

    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap.Clear();
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_Receiver), nameof(I_Receiver));
            PModule.linkMap[nameof(I_Receiver)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Clear();
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_Receiver), typeof(Receiver));
        }

        public static void InitializeMonitorObserves()
        {
            PModule.monitorObserves.Clear();
        }

        public static void InitializeMonitorMap(PSharpRuntime runtime)
        {
            PModule.monitorMap.Clear();
        }


        [Test]
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
        public I_Main(MachineId machine, List<string> permissions) : base(machine, permissions)
        {
        }
    }

    public class I_Receiver : PMachineValue
    {
        public I_Receiver(MachineId machine, List<string> permissions) : base(machine, permissions)
        {
        }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.Clear();
            PInterfaces.AddInterface(nameof(I_Main), nameof(e1), nameof(e2), nameof(e3), nameof(e4), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_Receiver), nameof(e1), nameof(e2), nameof(e3), nameof(e4), nameof(PHalt));
        }
    }

    public partial class PHelper
    {
        public static void InitializeEnums()
        {
            PrtEnum.Clear();
            PrtEnum.AddEnumElements(new[] {"foo0", "foo1", "foo2"}, new[] {0, 1, 2});
            PrtEnum.AddEnumElements(new[] {"bar0", "bar1"}, new[] {0, 1});
        }
    }
}
#pragma warning restore 162, 219, 414