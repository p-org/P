using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxosRacey
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the MultiPaxos distributed
    /// concencus algorithm.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Registering events to the runtime.\n");
            Runtime.RegisterNewEvent(typeof(ePrepare));
            Runtime.RegisterNewEvent(typeof(eAccept));
            Runtime.RegisterNewEvent(typeof(eAgree));
            Runtime.RegisterNewEvent(typeof(eReject));
            Runtime.RegisterNewEvent(typeof(eAccepted));
            Runtime.RegisterNewEvent(typeof(eAllNodes));
            Runtime.RegisterNewEvent(typeof(eChosen));
            Runtime.RegisterNewEvent(typeof(eUpdate));
            Runtime.RegisterNewEvent(typeof(ePing));
            Runtime.RegisterNewEvent(typeof(eNewLeader));
            Runtime.RegisterNewEvent(typeof(eMonitorValueChosen));
            Runtime.RegisterNewEvent(typeof(eMonitorValueProposed));
            Runtime.RegisterNewEvent(typeof(eMonitorClientSent));
            Runtime.RegisterNewEvent(typeof(eMonitorProposerSent));
            Runtime.RegisterNewEvent(typeof(eMonitorProposerChosen));
            Runtime.RegisterNewEvent(typeof(eLocal));
            Runtime.RegisterNewEvent(typeof(eSuccess));
            Runtime.RegisterNewEvent(typeof(eGoPropose));
            Runtime.RegisterNewEvent(typeof(eStartTimer));
            Runtime.RegisterNewEvent(typeof(eCancelTimer));
            Runtime.RegisterNewEvent(typeof(eCancelTimerSuccess));
            Runtime.RegisterNewEvent(typeof(eTimeout));

            Console.WriteLine("Registering state machines to the runtime.\n");
            Runtime.RegisterNewMachine(typeof(GodMachine));
            Runtime.RegisterNewMachine(typeof(Client));
            Runtime.RegisterNewMachine(typeof(PaxosNode));
            Runtime.RegisterNewMachine(typeof(LeaderElection));
            Runtime.RegisterNewMachine(typeof(Timer));

            Console.WriteLine("Registering monitors to the runtime.\n");
            Runtime.RegisterNewMonitor(typeof(PaxosInvariantMonitor));
            Runtime.RegisterNewMonitor(typeof(ValidityCheckMonitor));

            Console.WriteLine("Configuring the runtime.\n");
            Runtime.Options.Mode = Runtime.Mode.BugFinding;

            Console.WriteLine("Starting the runtime.\n");
            Runtime.Start();
            Runtime.Wait();

            Console.WriteLine("Performing cleanup.\n");
            Runtime.Dispose();
        }
    }
}
