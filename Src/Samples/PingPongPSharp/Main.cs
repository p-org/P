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

#pragma warning disable 162, 414
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
        private PrtMap<PrtInt, PrtInt> part = new PrtMap<PrtInt, PrtInt>();
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
        }

        public async Task Anon()
        {
            Main currentMachine = this;
            PMachineValue container = null;
            PMachineValue TMP_tmp0 = null;
            PMachineValue TMP_tmp1 = null;
            PMachineValue TMP_tmp2 = null;
            PrtBool TMP_tmp3 = ((PrtBool)false);
            PrtInt TMP_tmp4 = ((PrtInt)0);
            PrtTuple<PMachineValue, PrtBool, PrtInt> TMP_tmp5 = (new PrtTuple<PMachineValue, PrtBool, PrtInt>(null, ((PrtBool)false), ((PrtInt)0)));
            PrtInt TMP_tmp6 = ((PrtInt)0);
            PrtInt TMP_tmp7 = ((PrtInt)0);
            PMachineValue TMP_tmp8 = null;
            PMachineValue TMP_tmp9 = null;
            PMachineValue TMP_tmp10 = null;
            PrtBool TMP_tmp11 = ((PrtBool)false);
            PrtInt TMP_tmp12 = ((PrtInt)0);
            PrtTuple<PMachineValue, PrtBool, PrtInt> TMP_tmp13 = (new PrtTuple<PMachineValue, PrtBool, PrtInt>(null, ((PrtBool)false), ((PrtInt)0)));
            PrtInt TMP_tmp14 = ((PrtInt)0);
            PrtInt TMP_tmp15 = ((PrtInt)0);
            TMP_tmp0 = CREATECONTAINER();
            container = TMP_tmp0;
            TMP_tmp1 = ((PMachineValue)((IPrtValue)container)?.Clone());
            TMP_tmp2 = currentMachine.self;
            TMP_tmp3 = ((PrtBool)false);
            TMP_tmp4 = ((PrtInt)0);
            TMP_tmp5 = new PrtTuple<PMachineValue, PrtBool, PrtInt>((PMachineValue)TMP_tmp2, (PrtBool)TMP_tmp3, (PrtInt)TMP_tmp4);
            CreateSMR(TMP_tmp1, TMP_tmp5);
            var PGEN_recvEvent = await currentMachine.ReceiveEvent(typeof(x));
            switch (PGEN_recvEvent)
            {
                case x PGEN_evt:
                    {
                        var payload = PGEN_evt.PayloadT;
                        TMP_tmp6 = payload.Item1;
                        TMP_tmp7 = payload.Item2;
                        (part)[TMP_tmp6] = TMP_tmp7;
                    }
                    break;
            }
            TMP_tmp8 = CREATECONTAINER();
            container = TMP_tmp8;
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
            PInterfaces.AddInterface(nameof(I_Main), nameof(a), nameof(PHalt), nameof(x), nameof(y));
        }
    }

}
#pragma warning restore 162, 414

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
