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
    public static partial class GlobalFunctions_Main { }
    internal class x : PEvent<PrtTuple<PrtInt, PrtInt>>
    {
        static x() { AssertVal = -1; AssumeVal = -1; }
        public x() : base() { }
        public x(PrtTuple<PrtInt, PrtInt> payload) : base(payload) { }
    }
    internal class a : PEvent<IPrtValue>
    {
        static a() { AssertVal = -1; AssumeVal = -1; }
        public a() : base() { }
        public a(IPrtValue payload) : base(payload) { }
    }
    internal class y : PEvent<IPrtValue>
    {
        static y() { AssertVal = -1; AssumeVal = -1; }
        public y() : base() { }
        public y(IPrtValue payload) : base(payload) { }
    }
    internal class Main : PMachine
    {
        private PMachineValue id = null;
        private PrtMap<PrtInt, PrtInt> part = new PrtMap<PrtInt, PrtInt>();
        public class ConstructorEvent : PEvent<IPrtValue> { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Main()
        {
            this.sends.Add(nameof(a));
            this.sends.Add(nameof(PHalt));
            this.sends.Add(nameof(x));
            this.sends.Add(nameof(y));
            this.receives.Add(nameof(a));
            this.receives.Add(nameof(PHalt));
            this.receives.Add(nameof(x));
            this.receives.Add(nameof(y));
            this.creates.Add(nameof(I_M2));
        }

        public async Task Anon()
        {
            Main currentMachine = this;
            PMachineValue container = null;
            PMachineValue TMP_tmp0 = null;
            PMachineValue TMP_tmp1 = null;
            PMachineValue TMP_tmp2 = null;
            PMachineValue TMP_tmp3 = null;
            PrtBool TMP_tmp4 = ((PrtBool)false);
            PrtInt TMP_tmp5 = ((PrtInt)0);
            PrtTuple<PMachineValue, PrtBool, PrtInt> TMP_tmp6 = (new PrtTuple<PMachineValue, PrtBool, PrtInt>(null, ((PrtBool)false), ((PrtInt)0)));
            PrtInt TMP_tmp7 = ((PrtInt)0);
            PrtInt TMP_tmp8 = ((PrtInt)0);
            PMachineValue TMP_tmp9 = null;
            PMachineValue TMP_tmp10 = null;
            PrtBool TMP_tmp11 = ((PrtBool)false);
            PrtInt TMP_tmp12 = ((PrtInt)0);
            PrtTuple<PMachineValue, PrtBool, PrtInt> TMP_tmp13 = (new PrtTuple<PMachineValue, PrtBool, PrtInt>(null, ((PrtBool)false), ((PrtInt)0)));
            PrtInt TMP_tmp14 = ((PrtInt)0);
            PrtInt TMP_tmp15 = ((PrtInt)0);
            TMP_tmp0 = CREATECONTAINER();
            container = TMP_tmp0;
            TMP_tmp1 = currentMachine.self;
            currentMachine.CreateInterface<I_M2>(currentMachine, TMP_tmp1);
            TMP_tmp2 = ((PMachineValue)((IPrtValue)container)?.Clone());
            TMP_tmp3 = currentMachine.self;
            TMP_tmp4 = ((PrtBool)false);
            TMP_tmp5 = ((PrtInt)0);
            TMP_tmp6 = new PrtTuple<PMachineValue, PrtBool, PrtInt>((PMachineValue)TMP_tmp3, (PrtBool)TMP_tmp4, (PrtInt)TMP_tmp5);
            CreateSMR(TMP_tmp2, TMP_tmp6);
            var PGEN_recvEvent = await currentMachine.ReceiveEvent(typeof(x));
            switch (PGEN_recvEvent)
            {
                case x PGEN_evt:
                    {
                        var payload = PGEN_evt.PayloadT;
                        TMP_tmp7 = payload.Item1;
                        TMP_tmp8 = payload.Item2;
                        (part)[TMP_tmp7] = TMP_tmp8;
                    }
                    break;
            }
            CREATECONTAINER();
            TMP_tmp9 = ((PMachineValue)((IPrtValue)container)?.Clone());
            TMP_tmp10 = currentMachine.self;
            TMP_tmp11 = ((PrtBool)false);
            TMP_tmp12 = ((PrtInt)1);
            TMP_tmp13 = new PrtTuple<PMachineValue, PrtBool, PrtInt>((PMachineValue)TMP_tmp10, (PrtBool)TMP_tmp11, (PrtInt)TMP_tmp12);
            CreateSMR(TMP_tmp9, TMP_tmp13);
            var PGEN_recvEvent_1 = await currentMachine.ReceiveEvent(typeof(x));
            switch (PGEN_recvEvent_1)
            {
                case x PGEN_evt_1:
                    {
                        var payload_1 = PGEN_evt_1.PayloadT;
                        TMP_tmp14 = payload_1.Item1;
                        TMP_tmp15 = payload_1.Item2;
                        (part)[TMP_tmp14] = TMP_tmp15;
                    }
                    break;
            }
        }
        public PMachineValue CREATECONTAINER()
        {
            Main currentMachine = this;
            return null;
        }
        public PMachineValue CreateSMR(PMachineValue cont, IPrtValue param)
        {
            Main currentMachine = this;
            return null;
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(S))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon))]
        class S : MachineState
        {
        }
    }
    internal class M2 : PMachine
    {
        private PMachineValue id_1 = null;
        private PrtMap<PrtInt, PrtInt> part_1 = new PrtMap<PrtInt, PrtInt>();
        public class ConstructorEvent : PEvent<PMachineValue> { public ConstructorEvent(PMachineValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PMachineValue)value.Clone()); }

        public M2()
        {
            this.sends.Add(nameof(a));
            this.sends.Add(nameof(PHalt));
            this.sends.Add(nameof(x));
            this.sends.Add(nameof(y));
            this.receives.Add(nameof(a));
            this.receives.Add(nameof(PHalt));
            this.receives.Add(nameof(x));
            this.receives.Add(nameof(y));
        }

        public void Anon_1()
        {
            M2 currentMachine = this;
            PMachineValue payload_2 = ((PEvent<PMachineValue>)currentMachine.ReceivedEvent).PayloadT;
            PMachineValue TMP_tmp0_1 = null;
            IEventWithPayload TMP_tmp1_1 = null;
            PrtInt TMP_tmp2_1 = ((PrtInt)0);
            PrtInt TMP_tmp3_1 = ((PrtInt)0);
            PrtTuple<PrtInt, PrtInt> TMP_tmp4_1 = (new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0)));
            TMP_tmp0_1 = ((PMachineValue)((IPrtValue)payload_2)?.Clone());
            TMP_tmp1_1 = new x((new PrtTuple<PrtInt, PrtInt>(((PrtInt)0), ((PrtInt)0))));
            TMP_tmp2_1 = ((PrtInt)0);
            TMP_tmp3_1 = ((PrtInt)0);
            TMP_tmp4_1 = new PrtTuple<PrtInt, PrtInt>((PrtInt)TMP_tmp2_1, (PrtInt)TMP_tmp3_1);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_1, (Event)TMP_tmp1_1, TMP_tmp4_1);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(S_1))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_1))]
        class S_1 : MachineState
        {
        }
    }
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_M2), nameof(I_M2));
            PModule.linkMap[nameof(I_M2)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_M2), typeof(M2));
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

    public class I_M2 : PMachineValue
    {
        public I_M2(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.AddInterface(nameof(I_Main), nameof(a), nameof(PHalt), nameof(x), nameof(y));
            PInterfaces.AddInterface(nameof(I_M2), nameof(a), nameof(PHalt), nameof(x), nameof(y));
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
