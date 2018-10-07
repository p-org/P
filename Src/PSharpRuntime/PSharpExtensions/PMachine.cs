using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.Runtime;
using PrtSharp.Values;

namespace PrtSharp
{
    public class PMachine : Machine
    {
        private string interfaceName;
        public List<string> sends = new List<string>();
        public List<string> creates = new List<string>();
        public List<string> receives = new List<string>();
        public PMachineValue self;
        protected object gotoPayload = null;

        public new void Assert(bool predicate)
        {
            base.Assert(predicate);
        }

        public new void Assert(bool predicate, string s, params object[] args)
        {
            base.Assert(predicate, s, args);
        }

        public class InitializeParameters
        {
            public string InterfaceName { get; }
            public object Payload { get; }

            public InitializeParameters(string interfaceName, object payload)
            {
                InterfaceName = interfaceName;
                Payload = payload;
            }
        }

        public class IntializeParametersEvent : PEvent<InitializeParameters>
        {
            public IntializeParametersEvent(InitializeParameters payload) : base(payload)
            {
            }
        }

        protected class ConstructorEvent : PEvent<object>
        {
            public ConstructorEvent(object payload) : base(payload)
            {
            }
        }

        protected void InitializeParametersFunction()
        {
            if (!(ReceivedEvent is IntializeParametersEvent @event))
            {
                throw new ArgumentException("Event type is incorrect: " + ReceivedEvent.GetType().Name);
            }

            interfaceName = @event.Payload.InterfaceName;
            self = new PMachineValue(Id, receives.ToList());
            RaiseEvent(this, new ConstructorEvent(@event.Payload.Payload));
        }

        protected override OnExceptionOutcome OnException(string methodName, Exception ex)
        {
            return ex is PNonStandardReturnException ? OnExceptionOutcome.HandledException : base.OnException(methodName, ex);
        }

        public PMachineValue CreateInterface<T>(PMachine creator, object payload = null)
            where T : PMachine
        {
            var createdInterface = PModule.linkMap[creator.interfaceName][typeof(T).Name];
            Assert(creates.Contains(createdInterface), $"Machine {GetType().Name} cannot create interface {createdInterface}, not in its creates set");
            var createMachine = PModule.interfaceDefinitionMap[createdInterface];
            var machineId = CreateMachine(createMachine, new IntializeParametersEvent(new InitializeParameters(createdInterface, payload)));
            return new PMachineValue(machineId, PInterfaces.GetPermissions(createdInterface));
        }

        public void SendEvent(PMachine source, PMachineValue target, Event ev, object payload = null)
        {
            Assert(!(ev is Default), "Machine cannot send a null event");
            Assert(sends.Contains(ev.GetType().Name), $"Event {ev.GetType().Name} is not in the sends set of the Machine {source.GetType().Name}");
            Assert(target.Permissions.Contains(ev.GetType().Name), $"Event {ev.GetType().Name} is not in the permissions set of the target machine");
            var oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length>0);
            var @event = (Event)oneArgConstructor.Invoke(new[]{payload});
            AnnounceInternal(@event);
            Send(target.Id, @event);
        }

        public void RaiseEvent(PMachine source, Event ev, object payload = null)
        {
            Assert(!(ev is Default), "Machine cannot raise a null event");
            var oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            var @event = (Event)oneArgConstructor.Invoke(new[] { payload });
            source.Raise(@event);
            throw new PNonStandardReturnException {ReturnKind = NonStandardReturn.Raise};
        }

        public void RaiseEvent(Event ev, object payload = null)
        {
            RaiseEvent(this, ev, payload);
        }

        public Task<Event> ReceiveEvent(params Type[] events)
        {
            return Receive(events);
        }

        public void GotoState<T>(object payload = null) where T : MachineState
        {
            //todo: goto parameter has to be initialized correctly
            gotoPayload = payload;
            Goto<T>();
            throw new PNonStandardReturnException { ReturnKind = NonStandardReturn.Goto };
        }

        public void PopState()
        {
            Pop();
            throw new PNonStandardReturnException { ReturnKind = NonStandardReturn.Pop };
        }

        public void Announce(Event ev, object payload = null)
        {
            Assert(!(ev is Default), "Machine cannot announce a null event");
            var oneArgConstructor = ev.GetType().GetConstructors().First(x => x.GetParameters().Length > 0);
            var @event = (Event)oneArgConstructor.Invoke(new[] { payload });
            AnnounceInternal(@event);
        }

        private void AnnounceInternal(Event ev)
        {
            Assert(!(ev is Default), "cannot send a null event");
            foreach (var monitor in PModule.monitorMap[interfaceName])
            {
                if (PModule.monitorObserves[monitor.Name].Contains(ev.GetType().Name))
                {
                    Monitor(monitor, ev);
                }
            }
        }
    }
}
