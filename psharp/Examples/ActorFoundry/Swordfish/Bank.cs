using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal class Bank : Machine
    {
        private Machine Driver;

        private Dictionary<Integer, Machine> Accounts;

        private int AccountIds = 0;

        private Transaction TransBeingProcessed;

        [Initial]
        private class Init : State
        {
            protected override void OnEntry()
            {
                var machine = this.Machine as Bank;

                Console.WriteLine("[Bank] is Initializing ...\n");

                machine.Driver = (Machine)this.Payload;
                machine.Accounts = new Dictionary<Integer, Machine>();
                machine.TransBeingProcessed = null;

                this.Raise(new eLocal());
            }

            protected override HashSet<Type> DefineDeferredEvents()
            {
                return new HashSet<Type>
                {
                    typeof(eCreateAccount)
                };
            }
        }

        private class Active : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("[Bank] is Active ...\n");
            }
        }

        private class WaitingAccountToClose : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("[Bank] is WaitingAccountToClose ...\n");
            }
        }

        private void CreateAccount()
        {
            Console.WriteLine("[Bank] is Creating Account ...\n");

            var transaction = (Transaction)this.Payload;

            Machine newAcct = null;

            int accountNumber = this.AccountIds + 1;

            if (transaction.GetAccountType() == 0)
            {
                newAcct = Machine.Factory.CreateMachine<CheckingAccount>(
                    new Tuple<Integer, string>(
                        transaction.GetAmount(), transaction.GetPin()));
            }
            else if (transaction.GetAccountType() == 1)
            {
                newAcct = Machine.Factory.CreateMachine<SavingsAccount>(
                    new Tuple<Integer, string, Double>(
                        transaction.GetAmount(), transaction.GetPin(), transaction.GetRate()));
            }
            else
            {
                this.Send(transaction, new FailureResponse("Illegal account type."));
                return;
            }

            this.Accounts.Add(accountNumber, newAcct);
            this.AccountIds = this.AccountIds + 1;

            this.Send(transaction, new OpenedResponse(accountNumber));
        }

        private void CloseAccount()
        {
            Console.WriteLine("[Bank] is Closing Account ...\n");

            var transaction = (Transaction)this.Payload;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account."));
                return;
            }

            var acct = this.Accounts[transaction.GetAccountNumber()];

            this.Send(acct, new eClose(this));
            this.TransBeingProcessed = transaction;

            this.Raise(new eClose(null));
        }

        private void CloseAccountAck()
        {
            Console.WriteLine("[Bank] is Closing Account Ack ...\n");

            var result = (Boolean)this.Payload;

            if (result.BooleanValue())
            {
                this.Accounts.Remove(this.TransBeingProcessed.GetAccountNumber());
                this.Send(this.TransBeingProcessed, new SuccessResponse("Account closed."));
                this.TransBeingProcessed = null;
            }
            else
            {
                this.Send(this.TransBeingProcessed, new FailureResponse("Account not closed: nonzero balance."));
                this.TransBeingProcessed = null;
            }

            this.Raise(new eLocal());
        }

        private void Withdraw()
        {
            Console.WriteLine("[Bank] is Withdrawing ...\n");

            var transaction = (Transaction)this.Payload;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account."));
                return;
            }

            var acct = this.Accounts[transaction.GetAccountNumber()];

            this.Send(acct, new eWithdraw(transaction));
        }

        private void Deposit()
        {
            Console.WriteLine("[Bank] is Depositing ...\n");

            var transaction = (Transaction)this.Payload;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account."));
                return;
            }

            var acct = this.Accounts[transaction.GetAccountNumber()];

            this.Send(acct, new eDeposit(transaction));
        }

        private void Transfer()
        {
            Console.WriteLine("[Bank] is Transfering ...\n");

            var transfer = (Transfer)this.Payload;

            if (!this.Accounts.ContainsKey(transfer.GetFrom()))
            {
                this.Send(transfer, new FailureResponse("No such account: " + transfer.GetFrom()));
                return;
            }

            var from = this.Accounts[transfer.GetFrom()];

            if (!this.Accounts.ContainsKey(transfer.GetAccountNumber()))
            {
                this.Send(transfer, new FailureResponse("No such account: " + transfer.GetAccountNumber()));
                return;
            }

            var to = this.Accounts[transfer.GetAccountNumber()];

            this.Send(to, new eTransfer(new Tuple<Transfer, Machine>(transfer, from)));
        }

        private void BalanceInquiry()
        {
            Console.WriteLine("[Bank] is in BalanceInquiry ...\n");

            var transaction = (Transaction)this.Payload;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account: " + transaction.GetAccountNumber()));
                return;
            }

            var acct = this.Accounts[transaction.GetAccountNumber()];

            this.Send(acct, new eBalanceInquiry(transaction));
        }

        private void UnlockAccount()
        {
            Console.WriteLine("[Bank] is Unlocking Account ...\n");

            var transaction = (Transaction)this.Payload;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account: " + transaction.GetAccountNumber()));
                return;
            }

            var acct = this.Accounts[transaction.GetAccountNumber()];

            this.Send(acct, new eUnlock(transaction));
        }

        private void LockAccount()
        {
            Console.WriteLine("[Bank] is Locking Account ...\n");

            var transaction = (Transaction)this.Payload;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account: " + transaction.GetAccountNumber()));
                return;
            }

            var acct = this.Accounts[transaction.GetAccountNumber()];

            this.Send(acct, new eLock(transaction));
        }

        private void Stop()
        {
            Console.WriteLine("[Bank] is Stopping ...\n");

            foreach (var account in this.Accounts)
            {
                this.Send(account.Value, new eStop());
            }

            this.Delete();
        }

        private void Send(Transaction trans, Response resp)
        {
            this.Send(trans.GetMachine(), Activator.CreateInstance(trans.GetCallback(), resp) as Event);
        }

        protected override Dictionary<Type, StepStateTransitions> DefineStepStateTransitions()
        {
            Dictionary<Type, StepStateTransitions> dict = new Dictionary<Type, StepStateTransitions>();

            StepStateTransitions initDict = new StepStateTransitions();
            initDict.Add(typeof(eLocal), typeof(Active));

            StepStateTransitions activeDict = new StepStateTransitions();
            activeDict.Add(typeof(eClose), typeof(WaitingAccountToClose));

            StepStateTransitions waitingAccountToCloseDict = new StepStateTransitions();
            waitingAccountToCloseDict.Add(typeof(eLocal), typeof(Active));

            dict.Add(typeof(Init), initDict);
            dict.Add(typeof(Active), activeDict);
            dict.Add(typeof(WaitingAccountToClose), waitingAccountToCloseDict);

            return dict;
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings activeDict = new ActionBindings();
            activeDict.Add(typeof(eCreateAccount), new Action(CreateAccount));
            activeDict.Add(typeof(eCloseAccount), new Action(CloseAccount));
            activeDict.Add(typeof(eWithdraw), new Action(Withdraw));
            activeDict.Add(typeof(eDeposit), new Action(Deposit));
            activeDict.Add(typeof(eTransfer), new Action(Transfer));
            activeDict.Add(typeof(eBalanceInquiry), new Action(BalanceInquiry));
            activeDict.Add(typeof(eUnlock), new Action(UnlockAccount));
            activeDict.Add(typeof(eLock), new Action(LockAccount));
            activeDict.Add(typeof(eStop), new Action(Stop));

            ActionBindings waitingAccountToCloseDict = new ActionBindings();
            waitingAccountToCloseDict.Add(typeof(eCloseAck), new Action(CloseAccountAck));

            dict.Add(typeof(Active), activeDict);
            dict.Add(typeof(WaitingAccountToClose), waitingAccountToCloseDict);

            return dict;
        }
    }
}
