using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.PSharp;

namespace PSharpExtensions
{
    public class PMachine : Machine
    {
        public string interfaceName;
        public List<string> sends;
        public List<string> creates;
        public PMachineId self;

        internal class InitializeParameters
        {
            public string InterfaceName { get; }
            public object Payload { get; }

            public InitializeParameters(string interfaceName, object payload)
            {
                this.InterfaceName = interfaceName;
                this.Payload = payload;
            }
        }

        internal class IntializeParametersEvent : PEvent<InitializeParameters>
        {
            public IntializeParametersEvent(InitializeParameters payload) : base(payload)
            {
            }

        }

        internal class ContructorEvent : PEvent<object>
        {
            public ContructorEvent(object payload) : base(payload)
            {
            }
        }

        //implement the start state and raise the InitializeEvent
        

        public PMachineId CreateInterface(PMachine creator, string createInterface, object payload)
        {
            var createdInterface = PProgram.linkMap[creator.interfaceName][createInterface];
            this.Assert(this.creates.Contains(createdInterface), $"Machine {this.GetType().Name} cannot create interface {createdInterface}, not in its creates set");
            var createMachine = PProgram.interfaceDefinitionMap[createdInterface];
            var machineId = this.CreateMachine(Type.GetType(createMachine), new IntializeParametersEvent(new InitializeParameters(createdInterface, payload)));
            return new PMachineId(machineId, PProgram.interfaces[createdInterface]);
        }

        public void SendEvent(PMachine source, PMachineId target, Event ev)
        {

            this.Send(target.Id, ev);
        }
    }
}
