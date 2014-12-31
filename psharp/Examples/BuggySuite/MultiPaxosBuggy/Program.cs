using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace MultiPaxosBuggy
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements the MultiPaxos distributed
    /// concencus algorithm.
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            new CommandLineOptions(args).Parse();

            if (Runtime.Options.Mode == Runtime.Mode.Execution)
            {
                Program.Run();
            }
            else if (Runtime.Options.Mode == Runtime.Mode.BugFinding)
            {
                TestConfiguration test = new TestConfiguration(
                    "MultiPaxosBuggy",
                    Program.Run,
                    new RandomSchedulingStrategy(0),
                    100);

                //test.UntilBugFound = true;
                test.SoftTimeLimit = 600;
                Runtime.Test(test);
                Console.WriteLine(test.Result());
            }
        }

        public static void Run()
        {
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
            Runtime.RegisterNewEvent(typeof(eStop));

            Runtime.RegisterNewMachine(typeof(GodMachine));
            Runtime.RegisterNewMachine(typeof(Client));
            Runtime.RegisterNewMachine(typeof(PaxosNode));
            Runtime.RegisterNewMachine(typeof(LeaderElection));
            Runtime.RegisterNewMachine(typeof(Timer));
            Runtime.RegisterNewMachine(typeof(PaxosInvariantMonitor));
            Runtime.RegisterNewMachine(typeof(ValidityCheckMonitor));

            Runtime.Start();
            Runtime.Wait();
            Runtime.Dispose();
        }
    }
}
