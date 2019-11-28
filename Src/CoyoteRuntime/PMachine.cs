using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Plang.PrtSharp.Exceptions;
using Plang.PrtSharp.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plang.PrtSharp
{
    public class PMachine : StateMachine
    {
        public List<string> creates = new List<string>();
        protected IPrtValue gotoPayload;
        private string interfaceName;
        public List<string> receives = new List<string>();
        public PMachineValue self;
        public List<string> sends = new List<string>();

        public void TryAssert(bool predicate)
        {
            base.Assert(predicate);
        }

        public void TryAssert(bool predicate, string s, params object[] args)
        {
            base.Assert(predicate, s, args);
        }

        protected void InitializeParametersFunction(Event e)
        {
            if (!(e is InitializeParametersEvent @event))
            {
                throw new ArgumentException("Event type is incorrect: " + e.GetType().Name);
            }

            InitializeParameters initParam = @event.Payload as InitializeParameters;
            interfaceName = initParam.InterfaceName;
            self = new PMachineValue(Id, receives.ToList());
            TryRaiseEvent(GetConstructorEvent(initParam.Payload), initParam.Payload);
        }

        protected virtual Event GetConstructorEvent(IPrtValue value)
        {
            throw new NotImplementedException();
        }

        protected override OnExceptionOutcome OnException(Exception ex, string methodName, Event e)
        {
            bool v = ex is UnhandledEventException;
            if (!v)
            {
                return ex is PNonStandardReturnException
                    ? OnExceptionOutcome.HandledException
                    : base.OnException(ex, methodName, e);
            }

            return (ex as UnhandledEventException).UnhandledEvent is PHalt
                ? OnExceptionOutcome.Halt
                : base.OnException(ex, methodName, e);
        }

        public PMachineValue CreateInterface<T>(PMachine creator, IPrtValue payload = null)
            where T : PMachineValue
        {
            string createdInterface = PModule.linkMap[creator.interfaceName][typeof(T).Name];
            Assert(creates.Contains(createdInterface),
                $"Machine {GetType().Name} cannot create interface {createdInterface}, not in its creates set");
            Type createMachine = PModule.interfaceDefinitionMap[createdInterface];
            ActorId machineId = base.CreateActor(createMachine, createdInterface.Substring(2),
                new InitializeParametersEvent(new InitializeParameters(createdInterface, payload)));
            return new PMachineValue(machineId, PInterfaces.GetPermissions(createdInterface));
        }

        public void TrySendEvent(PMachineValue target, Event ev, object payload = null)
        {
            Assert(ev != null, "Machine cannot send a null event");
            Assert(sends.Contains(ev.GetType().Name),
                $"Event {ev.GetType().Name} is not in the sends set of the Machine {GetType().Name}");
            Assert(target.Permissions.Contains(ev.GetType().Name),
                $"Event {ev.GetType().Name} is not in the permissions set of the target machine");
            System.Reflection.ConstructorInfo oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            ev = (Event)oneArgConstructor.Invoke(new[] { payload });

            AnnounceInternal(ev);
            base.SendEvent(target.Id, ev);
            Logger.WriteLine($"<SendPayloadLog> Event {ev.GetType().Name} with payload {((PEvent)ev).Payload}");
        }

        public void TryRaiseEvent(Event ev, object payload = null)
        {
            Assert(ev != null, "Machine cannot raise a null event");
            System.Reflection.ConstructorInfo oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            ev = (Event)oneArgConstructor.Invoke(new[] { payload });

            base.RaiseEvent(ev);
            throw new PNonStandardReturnException { ReturnKind = NonStandardReturn.Raise };
        }

        public Task<Event> TryReceiveEvent(params Type[] events)
        {
            return base.ReceiveEventAsync(events);
        }

        public void TryGotoState<T>(IPrtValue payload = null) where T : State
        {
            gotoPayload = payload;
            base.GotoState<T>();
            throw new PNonStandardReturnException { ReturnKind = NonStandardReturn.Goto };
        }

        public void TryPopState()
        {
            base.PopState();
            throw new PNonStandardReturnException { ReturnKind = NonStandardReturn.Pop };
        }

        public int TryRandomInt(int maxValue)
        {
            return base.RandomInteger(maxValue);
        }

        public int TryRandomInt(int minValue, int maxValue)
        {
            return minValue + base.RandomInteger(maxValue - minValue);
        }

        public bool TryRandomBool(int maxValue)
        {
            return base.Random(maxValue);
        }

        public bool TryRandomBool()
        {
            return base.Random();
        }

        public void LogLine(string message)
        {
            Logger.WriteLine($"<PrintLog> {message}");
        }

        public void Log(string message)
        {
            Logger.Write($"{message}");
        }

        public void Announce(Event ev, object payload = null)
        {
            Assert(ev != null, "Machine cannot announce a null event");
            if (ev is PHalt)
            {
                ev = HaltEvent.Instance;
            }

            System.Reflection.ConstructorInfo oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            Event @event = (Event)oneArgConstructor.Invoke(new[] { payload });
            AnnounceInternal(@event);
        }

        private void AnnounceInternal(Event ev)
        {
            Assert(ev != null, "cannot send a null event");
            if (!PModule.monitorMap.ContainsKey(interfaceName))
            {
                return;
            }

            foreach (Type monitor in PModule.monitorMap[interfaceName])
            {
                if (PModule.monitorObserves[monitor.Name].Contains(ev.GetType().Name))
                {
                    Monitor(monitor, ev);
                }
            }
        }

        public class InitializeParameters : IPrtValue
        {
            public InitializeParameters(string interfaceName, IPrtValue payload)
            {
                InterfaceName = interfaceName;
                Payload = payload;
            }

            public string InterfaceName { get; }
            public IPrtValue Payload { get; }

            public bool Equals(IPrtValue other)
            {
                throw new NotImplementedException();
            }

            public IPrtValue Clone()
            {
                throw new NotImplementedException();
            }
        }

        public class InitializeParametersEvent : PEvent
        {
            public InitializeParametersEvent(InitializeParameters payload) : base(payload)
            {
            }
        }
    }
}