using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using System;

namespace Plang.CSharpRuntime
{
    public class _GodMachine : StateMachine
    {
        private void InitOnEntry(Event e)
        {
            Type mainMachine = (e as Config).MainMachine;
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