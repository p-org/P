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

namespace Main
{
    public static partial class GlobalFunctions_Main { }
    internal class E : PEvent<IPrtValue>
    {
        static E() { AssertVal = 1; AssumeVal = -1; }
        public E() : base() { }
        public E(IPrtValue payload) : base(payload) { }
    }
    internal class Main : PMachine
    {
        private IPrtValue vAny = null;
        private IEventWithPayload vEvent = null;
        private PrtInt vInt = ((PrtInt)0);
        public Main()
        {
            this.sends.Add(nameof(E));
            this.sends.Add(nameof(PHalt));
            this.receives.Add(nameof(E));
            this.receives.Add(nameof(PHalt));
        }

        public void Anon()
        {
            Main currentMachine = this;
            PrtBool TMP_tmp0 = ((PrtBool)false);
            PrtBool TMP_tmp1 = ((PrtBool)false);
            IEventWithPayload TMP_tmp2 = null;
            vAny = ((PrtInt)1);
            vInt = ((PrtInt)1);
            TMP_tmp0 = (vAny) == (vInt);
            currentMachine.Assert(TMP_tmp0, "");
            vEvent = new E(null);
            vAny = new E(null);
            TMP_tmp1 = (vAny) != (vEvent);
            currentMachine.Assert(TMP_tmp1, "");
            vAny = ((PrtBool)true);
            vInt = ((PrtInt)1);
            vAny = null;
            TMP_tmp2 = null;
            vEvent = TMP_tmp2;
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
            PInterfaces.AddInterface(nameof(I_Main), nameof(E), nameof(PHalt));
        }
    }

}


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
