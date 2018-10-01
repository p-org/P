using Microsoft.PSharp;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PrtSharp;
using PrtSharp.PValues;

namespace receive11
{
    public static partial class GlobalFunctions_receive11 { }
    internal class E : PEvent<int>
    {
        static E() { AssertVal = -1; AssumeVal = -1; }
        public E() : base() { }
        public E(int payload) : base(payload) { }
    }
    internal class F : PEvent<object>
    {
        static F() { AssertVal = -1; AssumeVal = -1; }
        public F() : base() { }
        public F(object payload) : base(payload) { }
    }
    internal class G : PEvent<object>
    {
        static G() { AssertVal = -1; AssumeVal = -1; }
        public G() : base() { }
        public G(object payload) : base(payload) { }
    }
    internal class Unit : PEvent<object>
    {
        static Unit() { AssertVal = -1; AssumeVal = -1; }
        public Unit() : base() { }
        public Unit(object payload) : base(payload) { }
    }
    internal class M : PMonitor
    {
        public void Anon()
        {
            int payload = (this.ReceivedEvent as PEvent<int>).Payload;
            bool TMP_tmp0 = false;
            TMP_tmp0 = (payload) == (10);
            this.Assert(TMP_tmp0, "");
        }
        public void Anon_1()
        {
        }
        [Start]
        [OnEventGotoState(typeof(E), typeof(Next), nameof(Anon))]
        class Init : MonitorState
        {
        }
        [OnEventGotoState(typeof(F), typeof(Next), nameof(Anon_1))]
        class Next : MonitorState
        {
        }
    }
    internal class A : PMachine
    {
        public A()
        {
            this.sends.Add("E");
            this.sends.Add("F");
            this.sends.Add("G");
            this.sends.Add("Unit");
            this.receives.Add("E");
            this.receives.Add("F");
            this.receives.Add("G");
            this.receives.Add("Unit");
        }

        public void Anon_2()
        {
            this.Announce(new F(null));
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ContructorEvent), typeof(Init_1))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_2))]
        class Init_1 : MachineState
        {
        }
    }
    internal class Main : PMachine
    {
        private int x = 0;
        private PMachineValue m = null;
        public Main()
        {
            this.sends.Add("E");
            this.sends.Add("F");
            this.sends.Add("G");
            this.sends.Add("Unit");
            this.receives.Add("E");
            this.receives.Add("F");
            this.receives.Add("G");
            this.receives.Add("Unit");
            this.creates.Add("A");
        }

        public void Anon_3()
        {
            Event TMP_tmp0_1 = null;
            TMP_tmp0_1 = new Unit(null);
            this.RaiseEvent(this, TMP_tmp0_1);
        }
        public async Task Anon_4()
        {
            PMachineValue TMP_tmp0_2 = null;
            Event TMP_tmp1 = null;
            PMachineValue TMP_tmp2 = null;
            Event TMP_tmp3 = null;
            int TMP_tmp4 = 0;
            PMachineValue TMP_tmp5 = null;
            TMP_tmp0_2 = this.self;
            TMP_tmp1 = new G(null);
            this.SendEvent(this, TMP_tmp0_2, TMP_tmp1);
            var PGEN_recvEvent = await this.Receive(typeof(G));
            switch (PGEN_recvEvent)
            {
                case G PGEN_evt:
                    {
                    }
                    break;
            }
        }
        public void Anon_5()
        {
            int payload_1 = (this.ReceivedEvent as PEvent<int>).Payload;
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ContructorEvent), typeof(Init_2))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_3))]
        [OnEventGotoState(typeof(Unit), typeof(X))]
        class Init_2 : MachineState
        {
        }
        [OnEntry(nameof(Anon_4))]
        [OnEventDoAction(typeof(E), nameof(Anon_5))]
        class X : MachineState
        {
        }
    }
    public class impl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap["Main"] = new Dictionary<string, string>();
            PModule.linkMap["Main"].Add("A", "A");
            PModule.linkMap["A"] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add("Main", typeof(Main));
            PModule.interfaceDefinitionMap.Add("A", typeof(A));
        }

        public static void InitializeMonitorObserves()
        {
            PModule.monitorObserves["M"] = new List<string>();
            PModule.monitorObserves["M"].Add("E");
            PModule.monitorObserves["M"].Add("F");
        }

        public static void InitializeMonitorMap(PSharpRuntime runtime)
        {
            PModule.monitorMap["Main"] = new List<Type>();
            PModule.monitorMap["Main"].Add(typeof(M));
            PModule.monitorMap["A"] = new List<Type>();
            PModule.monitorMap["A"].Add(typeof(M));
            runtime.RegisterMonitor(typeof(M));
        }

        public static void Main(string[] args)
        {
            // Optional: increases verbosity level to see the P# runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled(2);

            // Creates a new P# runtime instance, and passes an optional configuration.
            var runtime = PSharpRuntime.Create(configuration);

            // Executes the P# program.
            Execute(runtime);

            // The P# runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
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
    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.AddInterface("A", "E", "F", "G", "Unit");
            PInterfaces.AddInterface("Main", "E", "F", "G", "Unit");
        }
    }

}