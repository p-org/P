using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.PSharp;

namespace PSharpExtensions
{
    public class PMachine : Machine
    {
        public string interfaceName;
        public List<string> sends;
        public List<string> creates;
        public List<string> receives;
        public PMachineId self;

        protected class InitializeParameters
        {
            public string InterfaceName { get; }
            public object Payload { get; }

            public InitializeParameters(string interfaceName, object payload)
            {
                this.InterfaceName = interfaceName;
                this.Payload = payload;
            }
        }

        protected class IntializeParametersEvent : PEvent<InitializeParameters>
        {
            public IntializeParametersEvent(InitializeParameters payload) : base(payload)
            {
            }
        }

        protected class ContructorEvent : PEvent<object>
        {
            public ContructorEvent(object payload) : base(payload)
            {
            }
        }

        protected void InitializeParametersFunction()
        {
            if (ReceivedEvent is IntializeParametersEvent @event)
            {
                interfaceName = (@event.Payload as InitializeParameters).InterfaceName;
                self = new PMachineId(this.Id, this.receives.ToList());
                this.Raise(new ContructorEvent((@event.Payload as InitializeParameters).Payload));
            }
        }

        public PMachineId CreateInterface(PMachine creator, string createInterface, object payload)
        {
            var createdInterface = PProgram.linkMap[creator.interfaceName][createInterface];
            this.Assert(this.creates.Contains(createdInterface), $"Machine {this.GetType().Name} cannot create interface {createdInterface}, not in its creates set");
            var createMachine = PProgram.interfaceDefinitionMap[createdInterface];
            var machineId = this.CreateMachine(Type.GetType(createMachine), new IntializeParametersEvent(new InitializeParameters(createdInterface, payload)));
            return new PMachineId(machineId, PProgram.interfaces[createdInterface]);
        }

        public void SendEvent(PMachine source, PMachineId target, Event ev, object payload = null)
        {
            this.Assert(ev is Default, "Machine cannot send a null event");
            this.Assert(this.sends.Contains(ev.GetType().Name), $"Event {ev.GetType().Name} is not in the sends set of the Machine {source.GetType().Name}");
            this.Assert(target.Permissions.Contains(ev.GetType().Name), $"Event {ev.GetType().Name} is not in the permissions set of the target machine");
            var @event = (Event)Activator.CreateInstance(ev.GetType(), BindingFlags.CreateInstance, payload);
            this.Send(target.Id, @event);
        }

        public void RaiseEvent(PMachine source, Event ev, object payload = null)
        {
            this.Assert(ev is Default, "Machine cannot raise a null event");
            var @event = (Event)Activator.CreateInstance(ev.GetType(), BindingFlags.CreateInstance, payload);
            this.Raise(@event);
        }
    }
}
