using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Scheduling;

namespace BasicPaxos
{
    /// <summary>
    /// This is an example of usign P#.
    /// 
    /// This example implements Lamport's Paxos distributed
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
                    "BasicPaxos",
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
    }
}
