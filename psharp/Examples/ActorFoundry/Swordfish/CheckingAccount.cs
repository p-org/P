using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal class CheckingAccount : Account
    {
        private double Rate = 0.0;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as CheckingAccount;

                Console.WriteLine("[CheckingAccount] is Initializing ...\n");

                machine.Cents = 0;
                machine.IsLocked = false;
                machine.Counter = 0;
                machine.TransferTransaction = null;

                machine.Cents = ((Tuple<Integer, string>)this.Payload).Item1;
                machine.Pin = ((Tuple<Integer, string>)this.Payload).Item2;

                this.Raise(new eLocal());
            }
        }

        protected override void Update()
        {

        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Active));

            dict.Add(typeof(Init), initDict);

            return dict;
        }
    }
}
