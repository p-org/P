using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.Runtime;
using Plang.PrtSharp.Exceptions;
using Plang.PrtSharp.Values;

namespace Plang.PrtSharp
{
    public class PMachine : Machine
    {
        public List<string> creates = new List<string>();
        protected IPrtValue gotoPayload;
        private string interfaceName;
        public List<string> receives = new List<string>();
        public PMachineValue self;
        public List<string> sends = new List<string>();

        public new void Assert(bool predicate)
        {
            base.Assert(predicate);
        }

        public new void Assert(bool predicate, string s, params object[] args)
        {
            base.Assert(predicate, s, args);
        }

        protected void InitializeParametersFunction()
        {
            if (!(ReceivedEvent is InitializeParametersEvent @event))
                throw new ArgumentException("Event type is incorrect: " + ReceivedEvent.GetType().Name);

            var initParam = @event.Payload as InitializeParameters;
            interfaceName = initParam.InterfaceName;
            self = new PMachineValue(Id, receives.ToList());
            RaiseEvent(GetConstructorEvent(initParam.Payload), initParam.Payload);
        }

        protected virtual Event GetConstructorEvent(IPrtValue value)
        {
            throw new NotImplementedException();
        }

        protected override OnExceptionOutcome OnException(string methodName, Exception ex)
        {
            var v = ex is UnhandledEventException;
            if (!v)
                return ex is PNonStandardReturnException
                    ? OnExceptionOutcome.HandledException
                    : base.OnException(methodName, ex);
            return (ex as UnhandledEventException).UnhandledEvent is PHalt
                ? OnExceptionOutcome.HaltMachine
                : base.OnException(methodName, ex);
        }

        public PMachineValue CreateInterface<T>(PMachine creator, IPrtValue payload = null)
            where T : PMachineValue
        {
            var createdInterface = PModule.linkMap[creator.interfaceName][typeof(T).Name];
            Assert(creates.Contains(createdInterface),
                $"Machine {GetType().Name} cannot create interface {createdInterface}, not in its creates set");
            var createMachine = PModule.interfaceDefinitionMap[createdInterface];
            var machineId = CreateMachine(createMachine,
                new InitializeParametersEvent(new InitializeParameters(createdInterface, payload)));
            return new PMachineValue(machineId, PInterfaces.GetPermissions(createdInterface));
        }

        public void SendEvent(PMachineValue target, Event ev, object payload = null)
        {
            Assert(ev != null, "Machine cannot send a null event");
            Assert(sends.Contains(ev.GetType().Name),
                $"Event {ev.GetType().Name} is not in the sends set of the Machine {this.GetType().Name}");
            Assert(target.Permissions.Contains(ev.GetType().Name),
                $"Event {ev.GetType().Name} is not in the permissions set of the target machine");
            var oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            ev = (Event) oneArgConstructor.Invoke(new[] {payload});


            AnnounceInternal(ev);
            Send(target.Id, ev);
        }

        public void RaiseEvent(Event ev, object payload = null)
        {
            Assert(ev != null, "Machine cannot raise a null event");
            var oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            ev = (Event) oneArgConstructor.Invoke(new[] {payload});


            this.Raise(ev);
            throw new PNonStandardReturnException {ReturnKind = NonStandardReturn.Raise};
        }


        public Task<Event> ReceiveEvent(params Type[] events)
        {
            return Receive(events);
        }

        public void GotoState<T>(IPrtValue payload = null) where T : MachineState
        {
            gotoPayload = payload;
            Goto<T>();
            throw new PNonStandardReturnException {ReturnKind = NonStandardReturn.Goto};
        }

        public void PopState()
        {
            Pop();
            throw new PNonStandardReturnException {ReturnKind = NonStandardReturn.Pop};
        }

        public int RandomInt(int maxValue)
        {
            return RandomInteger(maxValue);
        }

        public bool RandomBool(int maxValue)
        {
            return Random(maxValue);
        }

        public bool RandomBool()
        {
            return Random();
        }

        public void Announce(Event ev, object payload = null)
        {
            Assert(ev != null, "Machine cannot announce a null event");
            if (ev is PHalt) ev = new Halt();
            var oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            var @event = (Event) oneArgConstructor.Invoke(new[] {payload});
            AnnounceInternal(@event);
        }

        private void AnnounceInternal(Event ev)
        {
            Assert(ev != null, "cannot send a null event");
            if (!PModule.monitorMap.ContainsKey(interfaceName)) return;
            foreach (var monitor in PModule.monitorMap[interfaceName])
                if (PModule.monitorObserves[monitor.Name].Contains(ev.GetType().Name))
                    Monitor(monitor, ev);
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