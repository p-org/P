using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Leader
{
    #region Events

    internal class eLocal : Event { }

    internal class eSecondRound : Event
    {
        public eSecondRound(Object payload)
            : base(payload)
        { }
    }

    #endregion

    #region Machines

    [Main]
    internal class Broker : Machine
    {
        private List<Machine> Creatures;

        private int TotalRendezvous;
        private Machine FirstHooker;
        private Colour FirstColour;

        private int TotalCreatures;
        private int TotalStoppedCreatures;
        private int Round;

        private int TR;
        private int TT;

        private List<List<Colour>> Groups;

        private int Counter;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Broker;

                Console.WriteLine("[Broker] is Initializing ...\n");

                machine.TR = (int)this.Payload;
                machine.TotalRendezvous = machine.TR;
                machine.Round = 1;

                machine.Groups = new List<List<Colour>>();
                machine.Groups.Add(new List<Colour> {
                    Colour.blue, Colour.red, Colour.yellow
                });
                machine.Groups.Add(new List<Colour> {
                    Colour.blue, Colour.red, Colour.yellow,
                    Colour.red, Colour.yellow, Colour.blue,
                    Colour.red, Colour.yellow, Colour.red,
                    Colour.blue
                });

                this.Raise(new eLocal());
            }
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Booting));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings bootingDict = new ActionBindings();
            bootingDict.Add(typeof(eHook), new Action(Hook));

            dict.Add(typeof(Booting), bootingDict);

            return dict;
        }
    }

    #endregion
}
