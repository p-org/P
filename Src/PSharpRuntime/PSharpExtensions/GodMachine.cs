using System;
using Microsoft.PSharp;

namespace PrtSharp
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
            this.CreateMachine(mainMachine, new PMachine.IntializeParametersEvent(new PMachine.InitializeParameters("I_" +mainMachine.Name, null)));
        }
    }
}
