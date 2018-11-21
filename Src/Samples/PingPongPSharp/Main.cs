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
    internal partial class XYZ : PEvent<PrtNamedTuple>
    {
        static XYZ() { AssertVal = -1; AssumeVal = -1; }
        public XYZ() : base() { }
        public XYZ(PrtNamedTuple payload) : base(payload) { }
        public override IPrtValue Clone() { return new XYZ(); }
    }
    internal partial class Main : PMachine
    {
        private PrtNamedTuple compVal2 = (new PrtNamedTuple(new string[] { "first", "second" }, (new PrtNamedTuple(new string[] { "first", "second" }, ((PrtInt)0), (new PrtTuple(((PrtInt)0), ((PrtBool)false))))), new PrtSeq()));
        private PrtNamedTuple compVal1 = (new PrtNamedTuple(new string[] { "first", "second" }, ((PrtInt)0), (new PrtTuple(((PrtInt)0), ((PrtBool)false)))));
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
            PrtNamedTuple TMP_tmp0 = (new PrtNamedTuple(new string[] { "first", "second" }, ((PrtInt)0), (new PrtTuple(((PrtInt)0), ((PrtBool)false)))));
            PMachineValue TMP_tmp1 = null;
            IEventWithPayload TMP_tmp2 = null;
            PrtNamedTuple TMP_tmp3 = (new PrtNamedTuple(new string[] { "first", "second" }, (new PrtNamedTuple(new string[] { "first", "second" }, ((PrtInt)0), (new PrtTuple(((PrtInt)0), ((PrtBool)false))))), new PrtSeq()));
            ((PrtNamedTuple)compVal1)["first"] = (PrtInt)(((PrtInt)1));
            ((PrtTuple)((PrtNamedTuple)compVal1)["second"])[0] = (PrtInt)(((PrtInt)100));
            ((PrtTuple)((PrtNamedTuple)compVal1)["second"])[1] = (PrtBool)(((PrtBool)false));
            ((PrtNamedTuple)compVal2)["first"] = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)compVal1)?.Clone()));
            TMP_tmp0 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)compVal1)?.Clone()));
            ((PrtSeq)((PrtNamedTuple)compVal2)["second"]).Insert(((PrtInt)0), TMP_tmp0);
            TMP_tmp1 = (PMachineValue)(currentMachine.self);
            TMP_tmp2 = (IEventWithPayload)(new XYZ((new PrtNamedTuple(new string[] { "first", "second" }, (new PrtNamedTuple(new string[] { "first", "second" }, ((PrtInt)0), (new PrtTuple(((PrtInt)0), ((PrtBool)false))))), new PrtSeq()))));
            TMP_tmp3 = (PrtNamedTuple)(((PrtNamedTuple)((IPrtValue)compVal2)?.Clone()));
            currentMachine.SendEvent(currentMachine, TMP_tmp1, (Event)TMP_tmp2, TMP_tmp3);
        }
        public void Anon_1()
        {
            Main currentMachine = this;
            PrtNamedTuple payload = this.gotoPayload == null ? ((PEvent<PrtNamedTuple>)currentMachine.ReceivedEvent).PayloadT : (PrtNamedTuple)this.gotoPayload;
            this.gotoPayload = null;
            PrtNamedTuple TMP_tmp0_1 = (new PrtNamedTuple(new string[] { "first", "second" }, ((PrtInt)0), (new PrtTuple(((PrtInt)0), ((PrtBool)false)))));
            PrtInt TMP_tmp1_1 = ((PrtInt)0);
            PrtBool TMP_tmp2_1 = ((PrtBool)false);
            PrtNamedTuple TMP_tmp3_1 = (new PrtNamedTuple(new string[] { "first", "second" }, ((PrtInt)0), (new PrtTuple(((PrtInt)0), ((PrtBool)false)))));
            PrtTuple TMP_tmp4 = (new PrtTuple(((PrtInt)0), ((PrtBool)false)));
            PrtInt TMP_tmp5 = ((PrtInt)0);
            PrtBool TMP_tmp6 = ((PrtBool)false);
            PrtNamedTuple TMP_tmp7 = (new PrtNamedTuple(new string[] { "first", "second" }, ((PrtInt)0), (new PrtTuple(((PrtInt)0), ((PrtBool)false)))));
            PrtTuple TMP_tmp8 = (new PrtTuple(((PrtInt)0), ((PrtBool)false)));
            PrtBool TMP_tmp9 = ((PrtBool)false);
            PrtBool TMP_tmp10 = ((PrtBool)false);
            PrtSeq TMP_tmp11 = new PrtSeq();
            PrtInt TMP_tmp12 = ((PrtInt)0);
            PrtBool TMP_tmp13 = ((PrtBool)false);
            TMP_tmp0_1 = (PrtNamedTuple)(((PrtNamedTuple)payload)["first"]);
            TMP_tmp1_1 = (PrtInt)(((PrtNamedTuple)TMP_tmp0_1)["first"]);
            TMP_tmp2_1 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp1_1, ((PrtInt)1))));
            currentMachine.Assert(TMP_tmp2_1, "");
            TMP_tmp3_1 = (PrtNamedTuple)(((PrtNamedTuple)payload)["first"]);
            TMP_tmp4 = (PrtTuple)(((PrtNamedTuple)TMP_tmp3_1)["second"]);
            TMP_tmp5 = (PrtInt)(((PrtTuple)TMP_tmp4)[0]);
            TMP_tmp6 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp5, ((PrtInt)100))));
            currentMachine.Assert(TMP_tmp6, "");
            TMP_tmp7 = (PrtNamedTuple)(((PrtNamedTuple)payload)["first"]);
            TMP_tmp8 = (PrtTuple)(((PrtNamedTuple)TMP_tmp7)["second"]);
            TMP_tmp9 = (PrtBool)(((PrtTuple)TMP_tmp8)[1]);
            TMP_tmp10 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp9, ((PrtBool)false))));
            currentMachine.Assert(TMP_tmp10, "");
            TMP_tmp11 = (PrtSeq)(((PrtNamedTuple)payload)["second"]);
            TMP_tmp12 = (PrtInt)(((PrtInt)(TMP_tmp11).Count));
            TMP_tmp13 = (PrtBool)((PrtValues.SafeEquals(TMP_tmp12, ((PrtInt)1))));
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
