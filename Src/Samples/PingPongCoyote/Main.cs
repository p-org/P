using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Plang.PrtSharp;
using Plang.PrtSharp.Exceptions;
using Plang.PrtSharp.Values;
using System.Collections.Generic;

#pragma warning disable 162, 219, 414

namespace pingpong
{
    public static partial class GlobalFunctions { }

    internal partial class Ping : PEvent
    {
        public Ping() : base()
        {
        }

        public Ping(PMachineValue payload) : base(payload)
        {
        }

        public override IPrtValue Clone()
        {
            return new Ping();
        }
    }

    internal partial class Pong : PEvent
    {
        public Pong() : base()
        {
        }

        public Pong(IPrtValue payload) : base(payload)
        {
        }

        public override IPrtValue Clone()
        {
            return new Pong();
        }
    }

    internal partial class Success : PEvent
    {
        public Success() : base()
        {
        }

        public Success(IPrtValue payload) : base(payload)
        {
        }

        public override IPrtValue Clone()
        {
            return new Success();
        }
    }

    internal partial class Main : PMachine
    {
        private PMachineValue pongId = null;

        public class ConstructorEvent : PEvent {
            public ConstructorEvent(IPrtValue val) : base(val)
            {
            }
        }

        protected override Event GetConstructorEvent(IPrtValue value)
        {
            return new ConstructorEvent(value);
        }

        public Main()
        {
            sends.Add(nameof(Ping));
            sends.Add(nameof(Pong));
            sends.Add(nameof(Success));
            sends.Add(nameof(PHalt));
            receives.Add(nameof(Ping));
            receives.Add(nameof(Pong));
            receives.Add(nameof(Success));
            receives.Add(nameof(PHalt));
            creates.Add(nameof(I_PONG));
        }

        public void Anon()
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0 = null;
            PEvent TMP_tmp1 = null;
            TMP_tmp0 = currentMachine.CreateInterface<I_PONG>(currentMachine);
            pongId = TMP_tmp0;
            TMP_tmp1 = new Success(null);
            currentMachine.TryRaiseEvent(TMP_tmp1);
            throw new PUnreachableCodeException();
        }

        public void Anon_1()
        {
            Main currentMachine = this;
            PMachineValue TMP_tmp0_1 = null;
            PEvent TMP_tmp1_1 = null;
            PMachineValue TMP_tmp2 = null;
            PEvent TMP_tmp3 = null;
            TMP_tmp0_1 = ((PMachineValue)((IPrtValue)pongId)?.Clone());
            TMP_tmp1_1 = new Ping(null);
            TMP_tmp2 = currentMachine.self;
            currentMachine.TrySendEvent(TMP_tmp0_1, TMP_tmp1_1, TMP_tmp2);
            TMP_tmp3 = new Success(null);
            currentMachine.TryRaiseEvent(TMP_tmp3);
            throw new PUnreachableCodeException();
        }

        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Ping_Init))]
        private class __InitState__ : State { }

        [OnEntry(nameof(Anon))]
        [OnEventGotoState(typeof(Success), typeof(Ping_SendPing))]
        private class Ping_Init : State
        {
        }

        [OnEntry(nameof(Anon_1))]
        [OnEventGotoState(typeof(Success), typeof(Ping_WaitPong))]
        private class Ping_SendPing : State
        {
        }

        [OnEventGotoState(typeof(Pong), typeof(Ping_SendPing))]
        private class Ping_WaitPong : State
        {
        }

        private class Done : State
        {
        }
    }

    internal partial class PONG : PMachine
    {
        public class ConstructorEvent : PEvent {
            public ConstructorEvent(IPrtValue val) : base(val)
            {
            }
        }

        protected override Event GetConstructorEvent(IPrtValue value)
        {
            return new ConstructorEvent(value);
        }

        public PONG()
        {
            sends.Add(nameof(Ping));
            sends.Add(nameof(Pong));
            sends.Add(nameof(Success));
            sends.Add(nameof(PHalt));
            receives.Add(nameof(Ping));
            receives.Add(nameof(Pong));
            receives.Add(nameof(Success));
            receives.Add(nameof(PHalt));
        }

        public void Anon_2()
        {
            PONG currentMachine = this;
        }

        public void Anon_3(Event currentMachine_dequeuedEvent)
        {
            PONG currentMachine = this;
            PMachineValue payload = (PMachineValue)(gotoPayload ?? ((PEvent)currentMachine_dequeuedEvent).Payload);
            gotoPayload = null;
            PMachineValue TMP_tmp0_2 = null;
            PEvent TMP_tmp1_2 = null;
            PEvent TMP_tmp2_1 = null;
            TMP_tmp0_2 = ((PMachineValue)((IPrtValue)payload)?.Clone());
            TMP_tmp1_2 = new Pong(null);
            currentMachine.TrySendEvent(TMP_tmp0_2, TMP_tmp1_2);
            TMP_tmp2_1 = new Success(null);
            currentMachine.TryRaiseEvent(TMP_tmp2_1);
            throw new PUnreachableCodeException();
        }

        [Start]
        [OnEntry(nameof(InitializeParametersFunction))]
        [OnEventGotoState(typeof(ConstructorEvent), typeof(Pong_WaitPing))]
        private class __InitState__ : State { }

        [OnEntry(nameof(Anon_2))]
        [OnEventGotoState(typeof(Ping), typeof(Pong_SendPong))]
        private class Pong_WaitPing : State
        {
        }

        [OnEntry(nameof(Anon_3))]
        [OnEventGotoState(typeof(Success), typeof(Pong_WaitPing))]
        private class Pong_SendPong : State
        {
        }
    }

    public class DefaultImpl
    {
        public static void InitializeLinkMap()
        {
            PModule.linkMap.Clear();
            PModule.linkMap[nameof(I_Main)] = new Dictionary<string, string>
            {
                { nameof(I_PONG), nameof(I_PONG) }
            };
            PModule.linkMap[nameof(I_PONG)] = new Dictionary<string, string>();
        }

        public static void InitializeInterfaceDefMap()
        {
            PModule.interfaceDefinitionMap.Clear();
            PModule.interfaceDefinitionMap.Add(nameof(I_Main), typeof(Main));
            PModule.interfaceDefinitionMap.Add(nameof(I_PONG), typeof(PONG));
        }

        public static void InitializeMonitorObserves()
        {
            PModule.monitorObserves.Clear();
        }

        public static void InitializeMonitorMap(IActorRuntime runtime)
        {
            PModule.monitorMap.Clear();
        }

        [Microsoft.Coyote.TestingServices.Test]
        public static void Execute(IActorRuntime runtime)
        {
            runtime.SetLogFormatter(new PLogFormatter());
            PModule.runtime = runtime;
            PHelper.InitializeInterfaces();
            PHelper.InitializeEnums();
            InitializeLinkMap();
            InitializeInterfaceDefMap();
            InitializeMonitorMap(runtime);
            InitializeMonitorObserves();
            runtime.CreateActor(typeof(_GodMachine), new _GodMachine.Config(typeof(Main)));
        }
    }

    public class I_Main : PMachineValue
    {
        public I_Main(ActorId machine, List<string> permissions) : base(machine, permissions)
        {
        }
    }

    public class I_PONG : PMachineValue
    {
        public I_PONG(ActorId machine, List<string> permissions) : base(machine, permissions)
        {
        }
    }

    public partial class PHelper
    {
        public static void InitializeInterfaces()
        {
            PInterfaces.Clear();
            PInterfaces.AddInterface(nameof(I_Main), nameof(Ping), nameof(Pong), nameof(Success), nameof(PHalt));
            PInterfaces.AddInterface(nameof(I_PONG), nameof(Ping), nameof(Pong), nameof(Success), nameof(PHalt));
        }
    }

    public partial class PHelper
    {
        public static void InitializeEnums()
        {
            PrtEnum.Clear();
        }
    }
}

#pragma warning restore 162, 219, 414