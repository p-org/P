using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PSharp;

namespace PSharpExtensions
{
    public class _GodMachine : Machine
    {
        public class Config : Event
        {
            public Type MainMachine;

            public Config(Type main)
            {
                this.MainMachine = main;
            }
        }
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            var mainMachine = (this.ReceivedEvent as Config).MainMachine;
            this.CreateMachine(mainMachine, new PMachine.IntializeParametersEvent(new PMachine.InitializeParameters(mainMachine.Name, null)));
        }
    }
}
