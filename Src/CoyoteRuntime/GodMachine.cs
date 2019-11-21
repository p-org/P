using Microsoft.Coyote.Actors;
using Microsoft.Coyote;
using System;

namespace Plang.PrtSharp
{
    public class _GodMachine : StateMachine
    {
        private void InitOnEntry()
        {
            Type mainMachine = (ReceivedEvent as Config).MainMachine;
            CreateActor(mainMachine,
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
        private class Init : State
        {
        }
    }
}