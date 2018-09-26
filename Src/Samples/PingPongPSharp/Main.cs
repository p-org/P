using Microsoft.PSharp;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PSharpExtensions;
using System.Threading;
using System.Threading.Tasks;

namespace Main
{
    public static partial class GlobalFunctions_Main { }
    internal class e1 : PEvent<int>
    {
        static e1() { AssertVal = -1; AssumeVal = -1; }
        public e1() : base() { }
        public e1(int payload) : base(payload) { }
    }
    internal class Main : PMachine
    {
        private PMachineId m = null;
        public Main()
        {
            this.sends.Add("e1");
            this.receives.Add("e1");
            this.creates.Add("Receiver");
        }

        public void Anon()
        {
            PMachineId TMP_tmp0 = null;
            PMachineId TMP_tmp1 = null;
            Event TMP_tmp2 = null;
            int TMP_tmp3 = 0;
            PMachineId TMP_tmp4 = null;
            Event TMP_tmp5 = null;
            int TMP_tmp6 = 0;
            TMP_tmp0 = CreateInterface(this, "Receiver");
            m = TMP_tmp0;
            TMP_tmp1 = m;
            TMP_tmp2 = new e1(0);
            TMP_tmp3 = 1;
            this.SendEvent(this, TMP_tmp1, TMP_tmp2, TMP_tmp3);
            TMP_tmp4 = m;
            TMP_tmp5 = new e1(0);
            TMP_tmp6 = 2;
            this.SendEvent(this, TMP_tmp4, TMP_tmp5, TMP_tmp6);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ContructorEvent), typeof(Init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon))]
        class Init : MachineState
        {
        }
    }
    internal class Receiver : PMachine
    {
        public Receiver()
        {
            this.sends.Add("e1");
            this.receives.Add("e1");
        }

        public async Task Anon_1()
        {
            int i = 0;
            int x = 0;
            bool TMP_tmp0_1 = false;
            bool TMP_tmp1_1 = false;
            bool TMP_tmp2_1 = false;
            int TMP_tmp3_1 = 0;
            TMP_tmp0_1 = (i) < (2);
            TMP_tmp1_1 = TMP_tmp0_1;
            while (TMP_tmp1_1)
            {
                var PGEN_recvEvent = await this.Receive(typeof(e1));
                switch (PGEN_recvEvent)
                {
                    case e1 PGEN_evt:
                        {
                        }
                        break;
                }
                PModule.runtime.Logger.WriteLine("x = {0}\n", x);
                TMP_tmp3_1 = (i) + (1);
                i = TMP_tmp3_1;
                TMP_tmp0_1 = (i) < (2);
                TMP_tmp1_1 = TMP_tmp0_1;
            }
            PModule.runtime.WriteLine("done!\n");
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ContructorEvent), typeof(Init_1))]
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
            PModule.linkMap["Main"] = new Dictionary<string, string>();
            PModule.linkMap["Main"].Add("Receiver", "Receiver");
            PModule.linkMap["Receiver"] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add("Main", typeof(Main));
            PModule.interfaceDefinitionMap.Add("Receiver", typeof(Receiver));
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
            runtime.CreateMachine(typeof(_GodMachine), new _GodMachine.Config(typeof(Main)));
        }
    }
    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.AddInterface("Main", "e1");
            PInterfaces.AddInterface("Receiver", "e1");
        }
    }

}



