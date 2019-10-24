using Microsoft.PSharp;
using System;

namespace Plang.PrtSharp
{
    public class _GodMachine : Machine
    {
        private void InitOnEntry()
        {
            Type mainMachine = (ReceivedEvent as Config).MainMachine;
            CreateMachine(mainMachine,
                new PMachine.InitializeParametersEvent(
                    new PMachine.InitializeParameters("I_" + mainMachine.Name, null)));
        }

        public class Config : Event
        {
            public Type MainMachine;

            public Config(Type main)
            {
                MainMachine = main;
            }
        }

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : MachineState
        {
        }
    }
}