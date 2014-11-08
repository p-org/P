using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal class SuccessResponse : Response
    {
        public SuccessResponse(String message)
            : base(message)
        {

        }
    }

    internal class BalanceResponse : Response
    {
        private double Cents;

        public BalanceResponse(double cents)
            : base("Here is your balance: " + cents)
        {
            this.Cents = cents;
        }

        public double GetCents()
        {
            return this.Cents;
        }
    }

    internal class OpenedResponse : Response
    {
        private Integer AccountNumber;

        public OpenedResponse(Integer acctNumber)
            : base("Account opened.")
        {
            this.AccountNumber = acctNumber;
        }

        public double GetAccountNumber()
        {
            return this.AccountNumber;
        }
    }

    internal class FailureResponse : Response
    {
        public FailureResponse(String message)
            : base(message)
        {

        }
    }

    internal abstract class Response
    {
        private String Message;

        public Response(String message)
        {
            this.Message = message;
        }

        public String GetMessage()
        {
            return this.Message;
        }
    }
}
