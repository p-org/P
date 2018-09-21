using Microsoft.PSharp;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.IO;
using PSharpExtensions;

namespace Main
{
    public static partial class GlobalFunctions_Main { }
    internal class Ping : PEvent<PMachineId>
    {
        static Ping() { AssertVal = 1; AssumeVal = -1; }
        public Ping() : base(null) { }
        public Ping(PMachineId payload) : base(payload) { }
    }
    internal class Pong : PEvent<object>
    {
        static Pong() { AssertVal = 1; AssumeVal = -1; }
        public Pong() : base(null) { }
        public Pong(object payload) : base(payload) { }
    }
    internal class Success : PEvent<object>
    {
        static Success() { AssertVal = -1; AssumeVal = -1; }
        public Success() : base(null) { }
        public Success(object payload) : base(payload) { }
    }
    internal class Main : PMachine
    {
        private PMachineId pongId = null;
        public Main()
        {
            this.sends.Add("Ping");
            this.sends.Add("Pong");
            this.sends.Add("Success");
            this.receives.Add("Ping");
            this.receives.Add("Pong");
            this.receives.Add("Success");
            this.creates.Add("PONG");
        }

        public void Anon()
        {
            PMachineId TMP_tmp0 = null;
            Event TMP_tmp1 = null;
            TMP_tmp0 = CreateInterface(this, "PONG");
            pongId = TMP_tmp0;
            TMP_tmp1 = new Success(null);
            this.RaiseEvent(this, TMP_tmp1);
        }
        public void Anon_1()
        {
            PMachineId TMP_tmp0_1 = null;
            Event TMP_tmp1_1 = null;
            PMachineId TMP_tmp2 = null;
            Event TMP_tmp3 = null;
            TMP_tmp0_1 = pongId;
            TMP_tmp1_1 = new Ping(null);
            TMP_tmp2 = this.self;
            this.SendEvent(this, TMP_tmp0_1, TMP_tmp1_1, TMP_tmp2);
            TMP_tmp3 = new Success(null);
            this.RaiseEvent(this, TMP_tmp3);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ContructorEvent), typeof(Ping_Init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon))]
        [OnEventGotoState(typeof(Success), typeof(Ping_SendPing))]
        class Ping_Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_1))]
        [OnEventGotoState(typeof(Success), typeof(Ping_WaitPong))]
        class Ping_SendPing : MachineState
        {
        }
        [OnEventGotoState(typeof(Pong), typeof(Ping_SendPing))]
        class Ping_WaitPong : MachineState
        {
        }
        class Done : MachineState
        {
        }
    }
    internal class PONG : PMachine
    {
        public PONG()
        {
            this.sends.Add("Ping");
            this.sends.Add("Pong");
            this.sends.Add("Success");
            this.receives.Add("Ping");
            this.receives.Add("Pong");
            this.receives.Add("Success");
        }

        public void Anon_2()
        {
        }
        public void Anon_3()
        {
            PMachineId payload = (this.ReceivedEvent as PEvent<PMachineId>).Payload;
            PMachineId TMP_tmp0_2 = null;
            Event TMP_tmp1_2 = null;
            Event TMP_tmp2_1 = null;
            TMP_tmp0_2 = payload;
            TMP_tmp1_2 = new Pong(null);
            this.SendEvent(this, TMP_tmp0_2, TMP_tmp1_2);
            TMP_tmp2_1 = new Success(null);
            this.RaiseEvent(this, TMP_tmp2_1);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ContructorEvent), typeof(Pong_WaitPing))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_2))]
        [OnEventGotoState(typeof(Ping), typeof(Pong_SendPong))]
        class Pong_WaitPing : MachineState
        {
        }
        [OnEntry(nameof(Anon_3))]
        [OnEventGotoState(typeof(Success), typeof(Pong_WaitPing))]
        class Pong_SendPong : MachineState
        {
        }
    }
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap["Main"] = new Dictionary<string, string>();
            PModule.linkMap["Main"].Add("PONG", "PONG");
            PModule.linkMap["PONG"] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add("Main", typeof(Main));
            PModule.interfaceDefinitionMap.Add("PONG", typeof(PONG));
        }

        public static void InitializeMonitorMap(PSharpRuntime runtime)
        {
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
            PInterfaces.AddInterface("Main", "Ping", "Pong", "Success");
            PInterfaces.AddInterface("PONG", "Ping", "Pong", "Success");
        }
    }

}