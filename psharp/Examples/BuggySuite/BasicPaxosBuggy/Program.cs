using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace BasicPaxosBuggy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements Lamport's Paxos distributed
    /// concencus algorithm.
    /// </summary>
    public class Program
    {
        public static void Go()
        {
            Runtime.RegisterNewEvent(typeof(ePrepare));
            Runtime.RegisterNewEvent(typeof(eAccept));
            Runtime.RegisterNewEvent(typeof(eAgree));
            Runtime.RegisterNewEvent(typeof(eReject));
            Runtime.RegisterNewEvent(typeof(eAccepted));
            Runtime.RegisterNewEvent(typeof(eTimeout));
            Runtime.RegisterNewEvent(typeof(eStartTimer));
            Runtime.RegisterNewEvent(typeof(eCancelTimer));
            Runtime.RegisterNewEvent(typeof(eCancelTimerSuccess));
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eSuccess));
            Runtime.RegisterNewEvent(typeof(eMonitorValueChosen));
            Runtime.RegisterNewEvent(typeof(eMonitorValueProposed));
            Runtime.RegisterNewEvent(typeof(eStop));
            Runtime.RegisterNewMachine(typeof(GodMachine));
            Runtime.RegisterNewMachine(typeof(Acceptor));
            Runtime.RegisterNewMachine(typeof(Proposer));
            Runtime.RegisterNewMachine(typeof(Timer));
            Runtime.RegisterNewMachine(typeof(PaxosInvariantMonitor));
            Runtime.Start();
            Runtime.Wait();
            Runtime.Dispose();
        }

        static void Main(string[] args)
        {
            Runtime.Test(
                () =>
                {
                    Go();
                },
                1000,
                false,
                Runtime.SchedulingType.Random,
                true);
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
