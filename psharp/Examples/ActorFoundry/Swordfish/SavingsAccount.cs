using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal class SavingsAccount : Account
    {
        private double Rate = 0.0;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as SavingsAccount;

                Console.WriteLine("[SavingsAccount] is Initializing ...\n");

                machine.Cents = 0;
                machine.IsLocked = false;
                machine.Counter = 0;
                machine.TransferTransaction = null;

                machine.Cents = ((Tuple<Integer, string, Double>)this.Payload).Item1;
                machine.Pin = ((Tuple<Integer, string, Double>)this.Payload).Item2;
                machine.Rate = ((Tuple<Integer, string, Double>)this.Payload).Item3;

                this.Raise(new eLocal());
            }
        }

        protected override void Update()
        {
            Console.WriteLine("[SavingsAccount] is Updating ...\n");

            this.Cents = ((int)(this.Cents * (1 + this.Rate)));
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
