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
    internal class E1 : PEvent<object>
    {
        static E1() { AssertVal = 1; AssumeVal = -1; }
        public E1() : base() { }
        public E1(object payload) : base(payload) { }
    }
    internal class E2 : PEvent<PrtInt>
    {
        static E2() { AssertVal = 1; AssumeVal = -1; }
        public E2() : base() { }
        public E2(PrtInt payload) : base(payload) { }
    }
    internal class E3 : PEvent<object>
    {
        static E3() { AssertVal = -1; AssumeVal = 1; }
        public E3() : base() { }
        public E3(object payload) : base(payload) { }
    }
    internal class E4 : PEvent<object>
    {
        static E4() { AssertVal = -1; AssumeVal = -1; }
        public E4() : base() { }
        public E4(object payload) : base(payload) { }
    }
    internal class unit : PEvent<object>
    {
        static unit() { AssertVal = 1; AssumeVal = -1; }
        public unit() : base() { }
        public unit(object payload) : base(payload) { }
    }
    internal class Main : PMachine
    {
        private PMachineValue ghost_machine = null;
        public Main()
        {
            this.sends.Add(nameof(E1));
            this.sends.Add(nameof(E2));
            this.sends.Add(nameof(E3));
            this.sends.Add(nameof(E4));
            this.sends.Add(nameof(unit));
            this.receives.Add(nameof(E1));
            this.receives.Add(nameof(E2));
            this.receives.Add(nameof(E3));
            this.receives.Add(nameof(E4));
            this.receives.Add(nameof(unit));
            this.creates.Add(nameof(I_Ghost));
        }

        public void Anon()
        {
            Main currentMachine = this;
            PrtInt payload = ((PEvent<PrtInt>)currentMachine.ReceivedEvent).PayloadT;
            PrtInt TMP_tmp0 = ((PrtInt)0);
            TMP_tmp0 = ((PrtInt)((IPrtValue)payload).Clone());
            Action1(TMP_tmp0);
        }
        public void Anon_1()
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0_1 = null;
            PMachineValue TMP_tmp1 = null;
            IEventWithPayload TMP_tmp2 = null;
            TMP_tmp0_1 = currentMachine.self;
            TMP_tmp1 = currentMachine.CreateInterface<I_Ghost>(currentMachine, TMP_tmp0_1);
            ghost_machine = TMP_tmp1;
            TMP_tmp2 = new unit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp2);
            throw new PUnreachableCodeException();
        }
        public void Anon_2()
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0_2 = null;
            IEventWithPayload TMP_tmp1_1 = null;
            PMachineValue TMP_tmp2_1 = null;
            IEventWithPayload TMP_tmp3 = null;
            TMP_tmp0_2 = ((PMachineValue)((IPrtValue)ghost_machine).Clone());
            TMP_tmp1_1 = new E1(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_2, (Event)TMP_tmp1_1);
            TMP_tmp2_1 = ((PMachineValue)((IPrtValue)ghost_machine).Clone());
            TMP_tmp3 = new E1(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp2_1, (Event)TMP_tmp3);
        }
        public void Anon_3()
        {
            Main currentMachine = this;
            IEventWithPayload TMP_tmp0_3 = null;
            TMP_tmp0_3 = new unit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp0_3);
            throw new PUnreachableCodeException();
        }
        public void Anon_4()
        {
            Main currentMachine = this;
        }
        public void Action1(PrtInt payload_1)
        {
            Main currentMachine = this;
            PrtBool TMP_tmp0_4 = ((PrtBool)false);
            PMachineValue TMP_tmp1_2 = null;
            IEventWithPayload TMP_tmp2_2 = null;
            PMachineValue TMP_tmp3_1 = null;
            IEventWithPayload TMP_tmp4 = null;
            TMP_tmp0_4 = (payload_1) == (((PrtInt)100));
            currentMachine.Assert(TMP_tmp0_4, "");
            TMP_tmp1_2 = ((PMachineValue)((IPrtValue)ghost_machine).Clone());
            TMP_tmp2_2 = new E3(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp1_2, (Event)TMP_tmp2_2);
            TMP_tmp3_1 = ((PMachineValue)((IPrtValue)ghost_machine).Clone());
            TMP_tmp4 = new E3(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp3_1, (Event)TMP_tmp4);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Real_Init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_1))]
        [OnEventDoAction(typeof(E2), nameof(Anon))]
        [OnEventPushState(typeof(unit), typeof(Real_S1))]
        [OnEventGotoState(typeof(E4), typeof(Real_S2))]
        class Real_Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_2))]
        class Real_S1 : MachineState
        {
        }
        [OnEntry(nameof(Anon_3))]
        [OnEventGotoState(typeof(unit), typeof(Real_S3))]
        class Real_S2 : MachineState
        {
        }
        [OnEntry(nameof(Anon_4))]
        [OnEventGotoState(typeof(E4), typeof(Real_S3))]
        class Real_S3 : MachineState
        {
        }
    }
    internal class Ghost : PMachine
    {
        private PMachineValue real_machine = null;
        public Ghost()
        {
            this.sends.Add(nameof(E1));
            this.sends.Add(nameof(E2));
            this.sends.Add(nameof(E3));
            this.sends.Add(nameof(E4));
            this.sends.Add(nameof(unit));
            this.receives.Add(nameof(E1));
            this.receives.Add(nameof(E2));
            this.receives.Add(nameof(E3));
            this.receives.Add(nameof(E4));
            this.receives.Add(nameof(unit));
        }

        public void Anon_5()
        {
            Ghost currentMachine = this;
            PMachineValue payload_2 = ((PEvent<PMachineValue>)currentMachine.ReceivedEvent).PayloadT;
            IEventWithPayload TMP_tmp0_5 = null;
            real_machine = ((PMachineValue)((IPrtValue)payload_2).Clone());
            TMP_tmp0_5 = new unit(null);
            currentMachine.RaiseEvent(currentMachine, (Event)TMP_tmp0_5);
            throw new PUnreachableCodeException();
        }
        public void Anon_6()
        {
            Ghost currentMachine = this;
        }
        public void Anon_7()
        {
            Ghost currentMachine = this;
            PMachineValue TMP_tmp0_6 = null;
            IEventWithPayload TMP_tmp1_3 = null;
            PrtInt TMP_tmp2_3 = ((PrtInt)0);
            TMP_tmp0_6 = ((PMachineValue)((IPrtValue)real_machine).Clone());
            TMP_tmp1_3 = new E2(((PrtInt)0));
            TMP_tmp2_3 = ((PrtInt)100);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_6, (Event)TMP_tmp1_3, TMP_tmp2_3);
        }
        public void Anon_8()
        {
            Ghost currentMachine = this;
            PMachineValue TMP_tmp0_7 = null;
            IEventWithPayload TMP_tmp1_4 = null;
            PMachineValue TMP_tmp2_4 = null;
            IEventWithPayload TMP_tmp3_2 = null;
            PMachineValue TMP_tmp4_1 = null;
            IEventWithPayload TMP_tmp5 = null;
            TMP_tmp0_7 = ((PMachineValue)((IPrtValue)real_machine).Clone());
            TMP_tmp1_4 = new E4(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp0_7, (Event)TMP_tmp1_4);
            TMP_tmp2_4 = ((PMachineValue)((IPrtValue)real_machine).Clone());
            TMP_tmp3_2 = new E4(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp2_4, (Event)TMP_tmp3_2);
            TMP_tmp4_1 = ((PMachineValue)((IPrtValue)real_machine).Clone());
            TMP_tmp5 = new E4(null);
            currentMachine.SendEvent(currentMachine, TMP_tmp4_1, (Event)TMP_tmp5);
        }
        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(_Init))]
        class __InitState__ : MachineState { }

        [OnEntry(nameof(Anon_5))]
        [OnEventGotoState(typeof(unit), typeof(Ghost_Init))]
        class _Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_6))]
        [OnEventGotoState(typeof(E1), typeof(Ghost_S1))]
        class Ghost_Init : MachineState
        {
        }
        [OnEntry(nameof(Anon_7))]
        [OnEventGotoState(typeof(E3), typeof(Ghost_S2))]
        [IgnoreEvents(typeof(E1))]
        class Ghost_S1 : MachineState
        {
        }
        [OnEntry(nameof(Anon_8))]
        [OnEventGotoState(typeof(E3), typeof(Ghost_Init))]
        class Ghost_S2 : MachineState
        {
        }
    }
    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>();
            PModule.linkMap[nameof(I_Main)].Add(nameof(I_Ghost), nameof(I_Ghost));
            PModule.linkMap[nameof(I_Ghost)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_Ghost), typeof(Ghost));
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

    public class I_Ghost : PMachineValue
    {
        public I_Ghost(MachineId machine, List<string> permissions) : base(machine, permissions) { }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.AddInterface(nameof(I_Main), nameof(E1), nameof(E2), nameof(E3), nameof(E4), nameof(unit));
            PInterfaces.AddInterface(nameof(I_Ghost), nameof(E1), nameof(E2), nameof(E3), nameof(E4), nameof(unit));
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
