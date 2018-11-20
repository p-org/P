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
    internal partial class unit : PEvent<IPrtValue>
    {
        static unit() { AssertVal = -1; AssumeVal = -1; }
        public unit() : base() { }
        public unit(IPrtValue payload) : base(payload) { }
        public override IPrtValue Clone() { return new unit(); }
    }
    internal partial class seqpayload : PEvent<PrtSeq>
    {
        static seqpayload() { AssertVal = -1; AssumeVal = -1; }
        public seqpayload() : base() { }
        public seqpayload(PrtSeq payload) : base(payload) { }
        public override IPrtValue Clone() { return new seqpayload(); }
    }
    internal partial class Main : PMachine
    {
        private PrtSeq l = new PrtSeq();
        private PrtInt i = ((PrtInt)0);
        private PMachineValue mac = null;
        private PrtTuple<PrtSeq, PrtInt> t = (new PrtTuple<PrtSeq, PrtInt>(new PrtSeq(), ((PrtInt)0)));
        public class ConstructorEvent : PEvent<IPrtValue> { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Main()
        {
            this.sends.Add(nameof(PHalt));
            this.sends.Add(nameof(seqpayload));
            this.sends.Add(nameof(unit));
            this.receives.Add(nameof(PHalt));
            this.receives.Add(nameof(seqpayload));
            this.receives.Add(nameof(unit));
            this.creates.Add(nameof(I_XYZ));
        }

        public void Anon()
        {
            Main currentMachine = this;
            PrtInt TMP_tmp0 = ((PrtInt)0);
            PrtInt TMP_tmp1 = ((PrtInt)0);
            PrtInt TMP_tmp2 = ((PrtInt)0);
            PrtInt TMP_tmp3 = ((PrtInt)0);
            PrtInt TMP_tmp4 = ((PrtInt)0);
            PrtInt TMP_tmp5 = ((PrtInt)0);
            PrtSeq TMP_tmp6 = new PrtSeq();
            PrtInt TMP_tmp7 = ((PrtInt)0);
            PMachineValue TMP_tmp8 = null;
            PMachineValue TMP_tmp9 = null;
            IEventWithPayload TMP_tmp10 = null;
            PrtSeq TMP_tmp11 = new PrtSeq();
            TMP_tmp0 = (PrtInt)((PrtInt)12);
            l.Insert(((PrtInt)0), TMP_tmp0);
            TMP_tmp1 = (PrtInt)((PrtInt)23);
            l.Insert(((PrtInt)0), TMP_tmp1);
            TMP_tmp2 = (PrtInt)((PrtInt)12);
            l.Insert(((PrtInt)0), TMP_tmp2);
            TMP_tmp3 = (PrtInt)((PrtInt)23);
            l.Insert(((PrtInt)0), TMP_tmp3);
            TMP_tmp4 = (PrtInt)((PrtInt)12);
            l.Insert(((PrtInt)0), TMP_tmp4);
            TMP_tmp5 = (PrtInt)((PrtInt)23);
            l.Insert(((PrtInt)0), TMP_tmp5);
            TMP_tmp6 = (PrtSeq)((PrtSeq)((IPrtValue)l)?.Clone());
            TMP_tmp7 = (PrtInt)((PrtInt)1);
            TMP_tmp8 = (PMachineValue)currentMachine.CreateInterface<I_XYZ>(currentMachine, new PrtTuple<PrtSeq, PrtInt>(TMP_tmp6, TMP_tmp7));
            mac = (PMachineValue)TMP_tmp8;
            TMP_tmp9 = (PMachineValue)((PMachineValue)((IPrtValue)mac)?.Clone());
            TMP_tmp10 = (IEventWithPayload)new seqpayload(new PrtSeq());
            TMP_tmp11 = (PrtSeq)((PrtSeq)((IPrtValue)l)?.Clone());
            currentMachine.SendEvent(currentMachine, TMP_tmp9, (Event)TMP_tmp10, TMP_tmp11);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon))]
        class init : MachineState
        {
        }
    }
    internal partial class XYZ : PMachine
    {
        private PrtSeq ii = new PrtSeq();
        private PrtSeq rec = new PrtSeq();
        private PrtInt i_1 = ((PrtInt)0);
        public class ConstructorEvent : PEvent<PrtTuple<PrtSeq, PrtInt>> { public ConstructorEvent(PrtTuple<PrtSeq, PrtInt> val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((PrtTuple<PrtSeq, PrtInt>)value); }
        public XYZ()
        {
            this.sends.Add(nameof(PHalt));
            this.sends.Add(nameof(seqpayload));
            this.sends.Add(nameof(unit));
            this.receives.Add(nameof(PHalt));
            this.receives.Add(nameof(seqpayload));
            this.receives.Add(nameof(unit));
        }

        public void Anon_1()
        {
            XYZ currentMachine = this;
            PrtTuple<PrtSeq, PrtInt> payload = this.gotoPayload == null ? ((PEvent<PrtTuple<PrtSeq, PrtInt>>)currentMachine.ReceivedEvent).PayloadT : (PrtTuple<PrtSeq, PrtInt>)this.gotoPayload;
            this.gotoPayload = null;
            PrtSeq TMP_tmp0_1 = new PrtSeq();
            PrtSeq TMP_tmp1_1 = new PrtSeq();
            PrtInt TMP_tmp2_1 = ((PrtInt)0);
            PrtBool TMP_tmp3_1 = ((PrtBool)false);
            PrtInt TMP_tmp4_1 = ((PrtInt)0);
            PrtBool TMP_tmp5_1 = ((PrtBool)false);
            TMP_tmp0_1 = (PrtSeq)payload.Item1;
            ii = TMP_tmp0_1;
            TMP_tmp1_1 = (PrtSeq)payload.Item1;
            TMP_tmp2_1 = (PrtInt)(TMP_tmp1_1)[((PrtInt)0)];
            TMP_tmp3_1 = (PrtBool)(PrtValues.SafeEquals(TMP_tmp2_1, ((PrtInt)23)));
            currentMachine.Assert(TMP_tmp3_1, "");
            TMP_tmp4_1 = (PrtInt)payload.Item2;
            TMP_tmp5_1 = (PrtBool)(PrtValues.SafeEquals(TMP_tmp4_1, ((PrtInt)1)));
            currentMachine.Assert(TMP_tmp5_1, "");
        }
        public void Anon_2()
        {
            XYZ currentMachine = this;
            PrtSeq payload_1 = this.gotoPayload == null ? ((PEvent<PrtSeq>)currentMachine.ReceivedEvent).PayloadT : (PrtSeq)this.gotoPayload;
            this.gotoPayload = null;
            PrtInt TMP_tmp0_2 = ((PrtInt)0);
            PrtInt TMP_tmp1_2 = ((PrtInt)0);
            PrtBool TMP_tmp2_2 = ((PrtBool)false);
            PrtBool TMP_tmp3_2 = ((PrtBool)false);
            PrtInt TMP_tmp4_2 = ((PrtInt)0);
            PrtInt TMP_tmp5_2 = ((PrtInt)0);
            PrtBool TMP_tmp6_1 = ((PrtBool)false);
            PrtInt TMP_tmp7_1 = ((PrtInt)0);
            rec = (PrtSeq)((PrtSeq)((IPrtValue)payload_1)?.Clone());
            TMP_tmp0_2 = (PrtInt)((PrtInt)(rec).Count);
            TMP_tmp1_2 = (PrtInt)(TMP_tmp0_2) - (((PrtInt)1));
            i_1 = TMP_tmp1_2;
            TMP_tmp2_2 = (PrtBool)(i_1) >= (((PrtInt)0));
            TMP_tmp3_2 = (PrtBool)((PrtBool)((IPrtValue)TMP_tmp2_2)?.Clone());
            while (TMP_tmp3_2)
            {
                TMP_tmp4_2 = (PrtInt)(rec)[i_1];
                TMP_tmp5_2 = (PrtInt)(ii)[i_1];
                TMP_tmp6_1 = (PrtBool)(PrtValues.SafeEquals(TMP_tmp4_2, TMP_tmp5_2));
                currentMachine.Assert(TMP_tmp6_1, "");
                TMP_tmp7_1 = (PrtInt)(i_1) - (((PrtInt)1));
                i_1 = TMP_tmp7_1;
                TMP_tmp2_2 = (PrtBool)(i_1) >= (((PrtInt)0));
                TMP_tmp3_2 = (PrtBool)((PrtBool)((IPrtValue)TMP_tmp2_2)?.Clone());
            }
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(init_1))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_1))]
        [OnEventGotoState(typeof(seqpayload), typeof(XYZitnow))]
        class init_1 : MachineState
        {
        }
        [OnEntry(nameof(Anon_2))]
        class XYZitnow : MachineState
        {
        }
    }
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_XYZ), nameof(I_XYZ));
            PModule.linkMap[nameof(I_XYZ)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_XYZ), typeof(XYZ));
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

    public class I_XYZ : PMachineValue
    {
        public I_XYZ(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.AddInterface(nameof(I_Main), nameof(PHalt), nameof(seqpayload), nameof(unit));
            PInterfaces.AddInterface(nameof(I_XYZ), nameof(PHalt), nameof(seqpayload), nameof(unit));
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
