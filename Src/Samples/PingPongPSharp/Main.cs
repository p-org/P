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
    internal class Ping : PEvent<PMachineValue>
    {
        static Ping() { AssertVal = 1; AssumeVal = -1; }
        public Ping() : base() { }
        public Ping(PMachineValue payload) : base(payload) { }
    }
    internal class Pong : PEvent<object>
    {
        static Pong() { AssertVal = 1; AssumeVal = -1; }
        public Pong() : base() { }
        public Pong(object payload) : base(payload) { }
    }
    internal class Success : PEvent<object>
    {
        static Success() { AssertVal = -1; AssumeVal = -1; }
        public Success() : base() { }
        public Success(object payload) : base(payload) { }
    }
    internal class Main : PMachine
    {
        private PMachineValue pongId = null;
        private PMachineValue pongId1 = null;
        public Main()
        {
            this.sends.Add(nameof(Ping));
            this.sends.Add(nameof(Pong));
            this.sends.Add(nameof(Success));
            this.receives.Add(nameof(Ping));
            this.receives.Add(nameof(Pong));
            this.receives.Add(nameof(Success));
            this.creates.Add(nameof(I_PONG));
        }

        public void Anon()
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0 = null;
            IEventWithPayload<object> TMP_tmp1 = null;
            TMP_tmp0 = currentMachine.CreateInterface<I_PONG>(currentMachine);
            pongId = TMP_tmp0;
            TMP_tmp1 = new Success(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp1);
            throw new PUnreachableCodeException();
        }
        public PrtBool foo()
        {
            Main currentMachine = this;
            return ((PrtBool)true);
        }
        public void Anon_1()
        {
            Main currentMachine = this;
            PrtBool res = ((PrtBool)false);
            PMachineValue TMP_tmp0_1 = null;
            IEventWithPayload<object> TMP_tmp1_1 = null;
            PMachineValue TMP_tmp2 = null;
            PrtBool TMP_tmp3 = ((PrtBool)false);
            IEventWithPayload<object> TMP_tmp4 = null;
            TMP_tmp0_1 = ((PMachineValue)((IPrtValue)pongId).Clone());
            TMP_tmp1_1 = new Ping(null);
            TMP_tmp2 = currentMachine.self;
            currentMachine.SendEvent(currentMachine, TMP_tmp0_1, (Event)TMP_tmp1_1, TMP_tmp2);
            TMP_tmp3 = foo();
            res = TMP_tmp3;
            TMP_tmp4 = new Success(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp4);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Ping_Init))]
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
            this.sends.Add(nameof(Ping));
            this.sends.Add(nameof(Pong));
            this.sends.Add(nameof(Success));
            this.receives.Add(nameof(Ping));
            this.receives.Add(nameof(Pong));
            this.receives.Add(nameof(Success));
        }

        public void Anon_2()
        {
            PONG currentMachine = this;
        }
        public void Anon_3()
        {
            PONG currentMachine = this;
            PMachineValue payload = (currentMachine.ReceivedEvent as PEvent<PMachineValue>).Payload;
            PMachineValue TMP_tmp0_2 = null;
            IEventWithPayload<object> TMP_tmp1_2 = null;
            IEventWithPayload<object> TMP_tmp2_1 = null;
            TMP_tmp0_2 = ((PMachineValue)((IPrtValue)payload).Clone());
            TMP_tmp1_2 = new Pong(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_2, (Event)TMP_tmp1_2);
            TMP_tmp2_1 = new Success(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp2_1);
            throw new PUnreachableCodeException();
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Pong_WaitPing))]
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
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_PONG), nameof(I_PONG));
            PModule.linkMap[nameof(I_PONG)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_PONG), typeof(PONG));
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

    public class I_PONG : PMachineValue
    {
        public I_PONG(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.AddInterface(nameof(I_Main), nameof(Ping), nameof(Pong), nameof(Success));
            PInterfaces.AddInterface(nameof(I_PONG), nameof(Ping), nameof(Pong), nameof(Success));
        }
    }

}

namespace Main
{
    public static class _TestRegression
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
            // to not terminate the process
            Console.WriteLine("Press Enter to terminate...");
            Console.ReadLine();
        }
    }
}