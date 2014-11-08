using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal class Transfer : Transaction
    {
        private Integer From;

        public Transfer(Machine machine, Type callback, Integer from, Integer to,
            Integer amount, string pin)
            : base(machine, callback, to, amount, pin, 3, 0)
        {
            this.From = from;
        }

        public Integer GetFrom()
        {
            return this.From;
        }
    }

    internal class Transaction
    {
        private Machine Machine;

        private Type Callback;

        private Integer AccountNumber;

        private int Amount;

        private String Pin;

        private int Type;

        private double Rate;

        public Transaction(Machine machine, Type callback, Integer accountNumber, Integer amount,
            string pin, int type, double rate)
        {
            this.Machine = machine;
            this.Callback = callback;
            this.AccountNumber = accountNumber;
            this.Amount = amount;
            this.Pin = pin;

            if (type == 3)
            {
                this.Type = 3;
                this.Rate = new Double(0.0);
                return;
            }

            if ((type != 0) && (type != 1))
            {
                throw new Exception("Invalid account type.");
            }

            this.Type = type;
            this.Rate = rate;
        }

        public Machine GetMachine()
        {
            return this.Machine;
        }

        public Type GetCallback()
        {
            return this.Callback;
        }

        public Integer GetAccountNumber()
        {
            return this.AccountNumber;
        }

        public int GetAmount()
        {
            return this.Amount;
        }

        public String GetPin()
        {
            return this.Pin;
        }

        public int GetAccountType()
        {
            return this.Type;
        }

        public double GetRate()
        {
            return this.Rate;
        }
    }
}
