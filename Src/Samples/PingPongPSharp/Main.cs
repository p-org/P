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
    internal class XYZ : PEvent<tuple>
    {
        static XYZ() { AssertVal = -1; AssumeVal = -1; }
        public XYZ() : base() { }
        public XYZ(tuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new XYZ(); }
    }
    internal class Main : PMachine
    {
        private tuple compVal2 = (new tuple((new tuple_1(((PrtInt)0), (new PrtTuple<PrtInt, PrtBool>(((PrtInt)0), ((PrtBool)false))))), new PrtSeq<tuple_1>()));
        private tuple_1 compVal1 = (new tuple_1(((PrtInt)0), (new PrtTuple<PrtInt, PrtBool>(((PrtInt)0), ((PrtBool)false)))));
        public class ConstructorEvent : PEvent<IPrtValue> { public ConstructorEvent(IPrtValue val) : base(val) { } }

        protected override Event GetConstructorEvent(IPrtValue value) { return new ConstructorEvent((IPrtValue)value); }
        public Main()
        {
            this.sends.Add(nameof(XYZ));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(XYZ));
            this.receives.Add(nameof(PHalt));
        }

        public void Anon()
        {
            Main currentMachine = this;
            tuple_1 TMP_tmp0 = (new tuple_1(((PrtInt)0), (new PrtTuple<PrtInt, PrtBool>(((PrtInt)0), ((PrtBool)false)))));
            PMachineValue TMP_tmp1 = null;
            IEventWithPayload TMP_tmp2 = null;
            tuple TMP_tmp3 = (new tuple((new tuple_1(((PrtInt)0), (new PrtTuple<PrtInt, PrtBool>(((PrtInt)0), ((PrtBool)false))))), new PrtSeq<tuple_1>()));
            (compVal1).first = ((PrtInt)1);
            (compVal1).second.Item1 = ((PrtInt)100);
            (compVal1).second.Item2 = ((PrtBool)false);
            (compVal2).first = ((tuple_1)((IPrtValue)compVal1)?.Clone());
            TMP_tmp0 = ((tuple_1)((IPrtValue)compVal1)?.Clone());
            (compVal2).second.Insert(((PrtInt)0), TMP_tmp0);
            TMP_tmp1 = currentMachine.self;
            TMP_tmp2 = new XYZ((new tuple((new tuple_1(((PrtInt)0), (new PrtTuple<PrtInt, PrtBool>(((PrtInt)0), ((PrtBool)false))))), new PrtSeq<tuple_1>())));
            TMP_tmp3 = ((tuple)((IPrtValue)compVal2)?.Clone());
            currentMachine.SendEvent(currentMachine, TMP_tmp1, (Event)TMP_tmp2, TMP_tmp3);
        }
        public void Anon_1()
        {
            Main currentMachine = this;
            tuple payload = ((PEvent<tuple>)currentMachine.ReceivedEvent).PayloadT;
            tuple_1 TMP_tmp0_1 = (new tuple_1(((PrtInt)0), (new PrtTuple<PrtInt, PrtBool>(((PrtInt)0), ((PrtBool)false)))));
            PrtInt TMP_tmp1_1 = ((PrtInt)0);
            PrtBool TMP_tmp2_1 = ((PrtBool)false);
            tuple_1 TMP_tmp3_1 = (new tuple_1(((PrtInt)0), (new PrtTuple<PrtInt, PrtBool>(((PrtInt)0), ((PrtBool)false)))));
            PrtTuple<PrtInt, PrtBool> TMP_tmp4 = (new PrtTuple<PrtInt, PrtBool>(((PrtInt)0), ((PrtBool)false)));
            PrtInt TMP_tmp5 = ((PrtInt)0);
            PrtBool TMP_tmp6 = ((PrtBool)false);
            tuple_1 TMP_tmp7 = (new tuple_1(((PrtInt)0), (new PrtTuple<PrtInt, PrtBool>(((PrtInt)0), ((PrtBool)false)))));
            PrtTuple<PrtInt, PrtBool> TMP_tmp8 = (new PrtTuple<PrtInt, PrtBool>(((PrtInt)0), ((PrtBool)false)));
            PrtBool TMP_tmp9 = ((PrtBool)false);
            PrtBool TMP_tmp10 = ((PrtBool)false);
            PrtSeq<tuple_1> TMP_tmp11 = new PrtSeq<tuple_1>();
            PrtInt TMP_tmp12 = ((PrtInt)0);
            PrtBool TMP_tmp13 = ((PrtBool)false);
            TMP_tmp0_1 = (payload).first;
            TMP_tmp1_1 = (TMP_tmp0_1).first;
            TMP_tmp2_1 = (TMP_tmp1_1) == (((PrtInt)1));
            currentMachine.Assert(TMP_tmp2_1, "");
            TMP_tmp3_1 = (payload).first;
            TMP_tmp4 = (TMP_tmp3_1).second;
            TMP_tmp5 = TMP_tmp4.Item1;
            TMP_tmp6 = (TMP_tmp5) == (((PrtInt)100));
            currentMachine.Assert(TMP_tmp6, "");
            TMP_tmp7 = (payload).first;
            TMP_tmp8 = (TMP_tmp7).second;
            TMP_tmp9 = TMP_tmp8.Item2;
            TMP_tmp10 = (TMP_tmp9) == (((PrtBool)false));
            currentMachine.Assert(TMP_tmp10, "");
            TMP_tmp11 = (payload).second;
            TMP_tmp12 = ((PrtInt)(TMP_tmp11).Count);
            TMP_tmp13 = (TMP_tmp12) == (((PrtInt)1));
            currentMachine.Assert(TMP_tmp13, "");
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(S1))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon))]
        [OnEventDoAction(typeof(XYZ), nameof(Anon_1))]
        class S1 : MachineState
        {
        }
    }
    // TODO: TypeDef compTup1
    // TODO: TypeDef compTup2
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
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

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.AddInterface(nameof(I_Main), nameof(XYZ), nameof(PHalt));
        }
    }

    public class tuple : PrtTuple<tuple_1, PrtSeq<tuple_1>>
    {
        public tuple(tuple_1 first, PrtSeq<tuple_1> second) : base(first, second) { }
        public tuple(IReadOnlyPrtTuple<tuple_1, PrtSeq<tuple_1>> other) : base(other) { }
        public tuple_1 first { get => Item1; set => Item1 = value; }
        public PrtSeq<tuple_1> second { get => Item2; set => Item2 = value; }
    }
    public class tuple_1 : PrtTuple<PrtInt, PrtTuple<PrtInt, PrtBool>>
    {
        public tuple_1(PrtInt first, PrtTuple<PrtInt, PrtBool> second) : base(first, second) { }
        public tuple_1(IReadOnlyPrtTuple<PrtInt, PrtTuple<PrtInt, PrtBool>> other) : base(other) { }
        public PrtInt first { get => Item1; set => Item1 = value; }
        public PrtTuple<PrtInt, PrtBool> second { get => Item2; set => Item2 = value; }
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
