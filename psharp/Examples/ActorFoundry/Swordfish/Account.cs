using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal abstract class Account : Machine
    {
        protected int Cents;

        protected String Pin;

        protected bool IsLocked;

        protected int Counter;

        protected Transfer TransferTransaction;

        protected class Active : State
        {
            protected override void OnEntry()
            {
                Console.WriteLine("[Account] is Active ...\n");
            }
        }

        private void Withdraw()
        {
            Console.WriteLine("[Account] is Withdrawing ...\n");

            var trans = ((Tuple<Transaction, Boolean>)this.Payload).Item1;
            var multiples = ((Tuple<Transaction, Boolean>)this.Payload).Item2;

            if (this.IsLocked)
            {
                this.Send(trans, new FailureResponse("Account is locked."));
                return;
            }

            if (!this.CheckPin(trans.GetPin()))
            {
                this.Send(trans, new FailureResponse("Invalid PIN."));
                return;
            }

            if (!multiples.BooleanValue() && (trans.GetAmount() % 2000 != 0))
            {
                this.Send(trans, new FailureResponse("Withdrawals must be in multiples of 20."));
                return;
            }

            if ((trans.GetAmount() < 0) || (trans.GetAmount() > this.Cents))
            {
                this.Send(trans, new FailureResponse("Withdraw failed."));
            }
            else
            {
                this.Cents = this.Cents - trans.GetAmount();
                this.Send(trans, new SuccessResponse("Withdraw succeeded."));
            }
        }

        private void Deposit()
        {
            Console.WriteLine("[Account] is Depositing ...\n");

            var trans = (Transaction)this.Payload;

            if (this.IsLocked)
            {
                this.Send(trans, new FailureResponse("Account is locked."));
                return;
            }

            if (!this.CheckPin(trans.GetPin()))
            {
                this.Send(trans, new FailureResponse("Invalid PIN."));
                return;
            }

            double amount = trans.GetAmount();

            if (amount < 0)
            {
                this.Send(trans, new FailureResponse("Deposit failed."));
            }
            else
            {
                this.Cents = this.Cents + (int)amount;
                this.Send(trans, new SuccessResponse("Deposit succeeded."));
            }
        }

        private void BalanceInquiry()
        {
            Console.WriteLine("[Account] is in BalanceInquiry ...\n");

            var trans = (Transaction)this.Payload;

            if (this.IsLocked)
            {
                this.Send(trans, new FailureResponse("Account is locked."));
                return;
            }

            if (this.TransferTransaction != null)
            {
                this.Send(trans, new FailureResponse("Transaction cannot be completed at this time."));
                return;
            }

            if (!this.CheckPin(trans.GetPin()))
            {
                this.Send(trans, new FailureResponse("Invalid PIN."));
                return;
            }

            this.Send(trans, new BalanceResponse(this.Cents));
        }

        private void Unlock()
        {
            Console.WriteLine("[Account] is Unlocking ...\n");

            var trans = (Transaction)this.Payload;

            this.Counter = 0;
            this.IsLocked = false;

            this.Send(trans, new SuccessResponse("Account successfully unlocked!"));
        }

        private void Lock()
        {
            Console.WriteLine("[Account] is Locking ...\n");

            var trans = (Transaction)this.Payload;

            this.IsLocked = true;

            this.Send(trans, new SuccessResponse("Account successfully locked!"));
        }

        private void Close()
        {
            Console.WriteLine("[Account] is Closing ...\n");

            var sender = (Machine)this.Payload;

            this.Send(sender, new eCloseAck(this.Cents == 0 && this.TransferTransaction == null));

            this.Delete();
        }

        private void Transfer()
        {
            Console.WriteLine("[Account] is Transferring ...\n");

            var trans = ((Tuple<Transfer, Machine>)this.Payload).Item1;
            var from = ((Tuple<Transfer, Machine>)this.Payload).Item2;

            if (this.IsLocked)
            {
                this.Send(trans, new FailureResponse("Destination account is locked."));
                return;
            }

            if (this.TransferTransaction != null)
            {
                this.Send(trans, new FailureResponse("Transaction cannot be completed at this time."));
                return;
            }

            this.TransferTransaction = trans;
            var wTrans = new Transaction(this, typeof(eTransComplete), new Integer(0),
                trans.GetAmount(), trans.GetPin(), 0, 0.0);

            this.Send(from, new eWithdraw(new Tuple<Transaction, bool>(wTrans, new Boolean(false))));
        }

        private void TransferComplete()
        {
            Console.WriteLine("[Account] Transfer completed ...\n");

            var resp = (Response)this.Payload;

            if (resp is SuccessResponse)
            {
                this.Cents = this.Cents + this.TransferTransaction.GetAmount();
                this.Send(this.TransferTransaction, new SuccessResponse("Transfer succeeded!"));
            }
            else
            {
                this.Send(this.TransferTransaction, new FailureResponse("Withdraw during transfer failed."));
            }

            this.TransferTransaction = null;
        }

        protected abstract void Update();

        private bool CheckPin(String p)
        {
            if (!this.IsLocked && p.Trim().Equals(this.Pin))
            {
                this.Counter = 0;
                return true;
            }
            else
            {
                this.Counter = this.Counter + 1;

                if (this.Counter >= 3)
                {
                    this.IsLocked = true;
                }

                return false;
            }
        }

        private void Stop()
        {
            Console.WriteLine("[Account] is Stopping ...\n");

            this.Delete();
        }

        private void Send(Transaction trans, Response resp)
        {
            this.Send(trans.GetMachine(), Activator.CreateInstance(trans.GetCallback(), resp) as Event);
        }

        protected override Dictionary<Type, ActionBindings> DefineActionBindings()
        {
            Dictionary<Type, ActionBindings> dict = new Dictionary<Type, ActionBindings>();

            ActionBindings activeDict = new ActionBindings();
            activeDict.Add(typeof(eWithdraw), new Action(Withdraw));
            activeDict.Add(typeof(eDeposit), new Action(Deposit));
            activeDict.Add(typeof(eBalanceInquiry), new Action(BalanceInquiry));
            activeDict.Add(typeof(eUnlock), new Action(Unlock));
            activeDict.Add(typeof(eLock), new Action(Lock));
            activeDict.Add(typeof(eClose), new Action(Close));
            activeDict.Add(typeof(eTransfer), new Action(Transfer));
            activeDict.Add(typeof(eTransComplete), new Action(TransferComplete));
            activeDict.Add(typeof(eUpdate), new Action(Update));
            activeDict.Add(typeof(eStop), new Action(Stop));

            dict.Add(typeof(Active), activeDict);

            return dict;
        }
    }
}
