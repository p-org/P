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
    class Program
    {
        static void Main(string[] args)
        {
            Runtime.Test(
                () =>
                {
                    Console.WriteLine("Registering events to the runtime.\n");
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

                    Console.WriteLine("Registering state machines to the runtime.\n");
                    Runtime.RegisterNewMachine(typeof(GodMachine));
                    Runtime.RegisterNewMachine(typeof(Acceptor));
                    Runtime.RegisterNewMachine(typeof(Proposer));
                    Runtime.RegisterNewMachine(typeof(Timer));

                    Console.WriteLine("Registering monitors to the runtime.\n");
                    Runtime.RegisterNewMonitor(typeof(PaxosInvariantMonitor));

                    Console.WriteLine("Starting the runtime.\n");
                    Runtime.Start();
                    Runtime.Wait();

                    Console.WriteLine("Performing cleanup.\n");
                    Runtime.Dispose();
                },
                10000,
                true,
                Runtime.SchedulingType.Random,
                true);
        }
    }
}
