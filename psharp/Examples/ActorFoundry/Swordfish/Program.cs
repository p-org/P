using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the swordfish benchmark.
    /// It attempts to be a faithful port from the SOTER
    /// actor version.
    /// </summary>
    public class Program
    {
        public static void Go()
        {
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewEvent(typeof(eCreateAccount));
            Runtime.RegisterNewEvent(typeof(eCloseAccount));
            Runtime.RegisterNewEvent(typeof(eWithdraw));
            Runtime.RegisterNewEvent(typeof(eDeposit));
            Runtime.RegisterNewEvent(typeof(eBalanceInquiry));
            Runtime.RegisterNewEvent(typeof(eUnlock));
            Runtime.RegisterNewEvent(typeof(eLock));
            Runtime.RegisterNewEvent(typeof(eClose));
            Runtime.RegisterNewEvent(typeof(eCloseAck));
            Runtime.RegisterNewEvent(typeof(eTransfer));
            Runtime.RegisterNewEvent(typeof(eTransComplete));
            Runtime.RegisterNewEvent(typeof(eUpdate));
            Runtime.RegisterNewEvent(typeof(eCreateCallback));

            Runtime.RegisterNewMachine(typeof(Driver));
            Runtime.RegisterNewMachine(typeof(Bank));
            Runtime.RegisterNewMachine(typeof(ATM));
            Runtime.RegisterNewMachine(typeof(CheckingAccount));
            Runtime.RegisterNewMachine(typeof(SavingsAccount));

            Runtime.Start();
            Runtime.Wait();
            Runtime.Dispose();
        }

        static void Main(string[] args)
        {
            Go();
        }
    }

    public class ChessTest
    {
        public static bool Run()
        {
            Program.Go();
            return true;
        }
    }
}
