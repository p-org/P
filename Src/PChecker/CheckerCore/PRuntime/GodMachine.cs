using System;
using PChecker.StateMachines;
using PChecker.StateMachines.Events;

namespace PChecker.PRuntime
{
    public class _GodMachine : StateMachine
    {
        private void InitOnEntry(Event e)
        {
            var mainMachine = (e as Config).MainMachine;
            CreateStateMachine(mainMachine, mainMachine.Name);
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