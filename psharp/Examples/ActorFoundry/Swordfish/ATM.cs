using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal class ATM : Machine
    {
        private Machine Bank;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as ATM;

                Console.WriteLine("[ATM] is Initializing ...\n");

                machine.Bank = (Machine)this.Payload;

                this.Raise(new eLocal());
            }
        }

        private class Active : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("[ATM] is Active ...\n");
            }
        }

        private void Withdraw()
        {
            Console.WriteLine("[ATM] is Withdrawing ...\n");

            var transaction = (Transaction)this.Payload;

            this.Send(this.Bank, new eWithdraw(transaction));
        }

        private void Deposit()
        {
            Console.WriteLine("[ATM] is Depositing ...\n");

            var transaction = (Transaction)this.Payload;

            this.Send(this.Bank, new eDeposit(transaction));
        }

        private void Transfer()
        {
            Console.WriteLine("[ATM] is Transfering ...\n");

            var transfer = (Transfer)this.Payload;

            this.Send(this.Bank, new eTransfer(transfer));
        }

        private void BalanceInquiry()
        {
            Console.WriteLine("[ATM] is in BalanceInquiry ...\n");

            var transaction = (Transaction)this.Payload;

            this.Send(this.Bank, new eBalanceInquiry(transaction));
        }

        private void Stop()
        {
            Console.WriteLine("[ATM] is Stopping ...\n");

            this.Delete();
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Active));

            dict.Add(typeof(Init), initDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings activeDict = new ActionBindings();
            activeDict.Add(typeof(eWithdraw), new Action(Withdraw));
            activeDict.Add(typeof(eDeposit), new Action(Deposit));
            activeDict.Add(typeof(eTransfer), new Action(Transfer));
            activeDict.Add(typeof(eBalanceInquiry), new Action(BalanceInquiry));
            activeDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(Active), activeDict);

            return dict;
        }
    }
}
